#Requires -Version 7.0

<#!
run-local.ps1

Purpose: One-command local dev runner for the realtime-sensor-analytics project.
- Verifies prerequisites (dotnet, node, npm)
- Ensures Redis is available (starts a Docker container if needed and Docker is available)
- Trusts the .NET dev HTTPS certificate (optional skip)
- Starts the backend API (ASP.NET Core) and the frontend (Angular) in parallel
- Waits until both are ready and opens the frontend in the browser (optional)
- Cleans up on exit (stops child processes; optionally stops Redis container)

Usage examples:
  pwsh -ExecutionPolicy Bypass -File ./scripts/run-local.ps1
  pwsh -ExecutionPolicy Bypass -File ./scripts/run-local.ps1 -ApiKey dev-key
  pwsh -ExecutionPolicy Bypass -File ./scripts/run-local.ps1 -CheckOnly

Parameters:
  -ApiKey            API key for protected endpoints. If omitted, backend in dev accepts any non-empty key.
  -RedisConnection   Redis connection string (default: localhost:6379)
  -UseDockerRedis    Force starting Redis via Docker if not already listening on the port (default: auto if Docker is available)
  -NoDockerRedis     Do not use Docker even if available (you must start Redis yourself)
  -SkipCert          Skip trusting the dev HTTPS certificate
  -OpenBrowser       Open http://localhost:4200 when frontend is ready (default: true)
  -CheckOnly         Perform dependency and environment checks, then exit
!#>

[CmdletBinding()]
param(
  [string]$ApiKey = "",
  [string]$RedisConnection = "localhost:6379",
  [switch]$UseDockerRedis,
  [switch]$NoDockerRedis,
  [switch]$SkipCert,
  [switch]$OpenBrowser = $true,
  [switch]$CheckOnly,
  [switch]$ExitAfterReady
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Info($msg) { Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Warn($msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow }
function Write-Err($msg)  { Write-Host "[ERROR] $msg" -ForegroundColor Red }

function Assert-Command($name, $hint) {
  if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
    throw "Required command '$name' was not found. $hint"
  }
}

function Test-Port([string]$hostname, [int]$port) {
  try {
    $res = Test-NetConnection -ComputerName $hostname -Port $port -WarningAction SilentlyContinue -ErrorAction SilentlyContinue
    return $res.TcpTestSucceeded
  } catch { return $false }
}

function Wait-HttpOk([string]$url, [int]$timeoutSec = 60) {
  $deadline = (Get-Date).AddSeconds($timeoutSec)
  while ((Get-Date) -lt $deadline) {
    try {
      $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5
      if ($r.StatusCode -ge 200 -and $r.StatusCode -lt 500) { return $true }
    } catch {
      Start-Sleep -Milliseconds 500
    }
  }
  return $false
}

function Start-RedisIfNeeded {
  $redisHost, $redisPort = $RedisConnection -split ':'
  if (-not $redisHost) { $redisHost = 'localhost' }
  if (-not $redisPort) { $redisPort = 6379 }

  if (Test-Port -hostname $redisHost -port ([int]$redisPort)) {
    Write-Info ("Redis already listening at {0}:{1}" -f $redisHost,$redisPort)
    return @{ Started = $false; Container = $null }
  }

  if ($NoDockerRedis) {
    throw ("Redis not reachable at {0}:{1} and -NoDockerRedis was specified. Please start Redis manually." -f $redisHost,$redisPort)
  }

  $dockerCli = (Get-Command docker -ErrorAction SilentlyContinue)
  $dockerAvailable = $dockerCli -ne $null
  if (-not $dockerAvailable) {
    if (-not $UseDockerRedis) {
      Write-Warn ("Redis not reachable at {0}:{1} and Docker is not installed. Continuing without Redis; streaming will still work, persistence disabled." -f $redisHost,$redisPort)
      return @{ Started = $false; Container = $null }
    } else {
      throw "Docker CLI not found; cannot start Redis via Docker."
    }
  }

  # Verify Docker engine is running
  $engineOk = $false
  try {
    $null = docker info -f '{{.ServerVersion}}' 2>$null
    if ($LASTEXITCODE -eq 0) { $engineOk = $true }
  } catch { $engineOk = $false }

  if (-not $engineOk) {
    if ($UseDockerRedis) {
      throw "Docker is installed but the Engine is not running. Start Docker Desktop and retry."
    }
    Write-Warn ("Docker Engine not running; continuing without Redis. Streaming will work, persistence disabled. Start Redis manually or run Docker if you need it.")
    return @{ Started = $false; Container = $null }
  }

  Write-Info "Starting Redis via Docker..."
  try {
    # Check if a container named rsa-redis is already present
    $existing = (docker ps -a --format '{{.Names}}' 2>$null | Where-Object { $_ -eq 'rsa-redis' })
    if ($existing) {
      # Try to start if not running
      $running = (docker ps --format '{{.Names}}' 2>$null | Where-Object { $_ -eq 'rsa-redis' })
      if (-not $running) {
        docker start rsa-redis | Out-Null
      }
    } else {
      docker run -d --name rsa-redis -p 6379:6379 --rm redis:7-alpine `
        redis-server --save "" --appendonly no | Out-Null
    }
  } catch {
    if ($UseDockerRedis) { throw "Failed to start Redis via Docker: $($_.Exception.Message)" }
    Write-Warn "Failed to start Redis via Docker; continuing without Redis (persistence disabled)."
    return @{ Started = $false; Container = $null }
  }

  # Wait for port to be ready
  $ok = $false
  for ($i=0; $i -lt 30; $i++) {
  if (Test-Port -hostname $redisHost -port ([int]$redisPort)) { $ok = $true; break }
    Start-Sleep -Milliseconds 500
  }
  if (-not $ok) { throw ("Redis container did not become ready on {0}:{1}" -f $redisHost,$redisPort) }

  Write-Info ("Redis is ready at {0}:{1}" -f $redisHost,$redisPort)
  return @{ Started = $true; Container = 'rsa-redis' }
}

function Ensure-DevCert {
  if ($SkipCert) { Write-Info "Skipping dev cert trust per -SkipCert"; return }
  Assert-Command dotnet "Install .NET SDK 8 from https://dotnet.microsoft.com/download"
  Write-Info "Ensuring dev HTTPS certificate is trusted..."
  dotnet dev-certs https --trust | Out-Null
}

function Start-Backend {
  $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
  $apiPath = Join-Path $repoRoot 'backend/src/Api'
  if (-not (Test-Path $apiPath)) { throw "Backend path not found: $apiPath" }

  $env:REDIS__CONNECTION = $RedisConnection
  if ($ApiKey) { $env:API_KEY = $ApiKey }
  $env:FRONTEND_ORIGIN = 'http://localhost:4200'
  # Bind deterministic ports for easier verification
  $env:ASPNETCORE_URLS = 'https://localhost:7215;http://localhost:5028'

  Write-Info "Starting backend API..."
  $proc = Start-Process -FilePath 'dotnet' -ArgumentList 'run','--launch-profile','https' -WorkingDirectory $apiPath -PassThru -NoNewWindow
  return $proc
}

function Start-Frontend {
  $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
  $fePath = Join-Path $repoRoot 'frontend'
  if (-not (Test-Path $fePath)) { throw "Frontend path not found: $fePath" }

  Assert-Command node "Install Node.js 20 from https://nodejs.org"
  Assert-Command npm  "Install Node.js 20 from https://nodejs.org"

  Write-Info "Installing frontend dependencies (npm ci)..."
  $nodeModules = Join-Path $fePath 'node_modules'
  if (Test-Path $nodeModules) {
    Write-Info "node_modules present; skipping npm ci"
  } else {
    Push-Location $fePath
    try {
      npm ci | Write-Host
    } finally {
      Pop-Location
    }
  }

  Write-Info "Starting Angular dev server..."
  $proc = Start-Process -FilePath 'cmd.exe' -ArgumentList '/c','npm','start' -WorkingDirectory $fePath -PassThru -NoNewWindow
  return $proc
}

# 1) Checks
Assert-Command dotnet "Install .NET SDK 8 from https://dotnet.microsoft.com/download"
Assert-Command pwsh  "Install PowerShell 7 from https://aka.ms/pwsh"

Write-Info "dotnet version:  $(dotnet --version)"
if (Get-Command node -ErrorAction SilentlyContinue) {
  Write-Info "node version:    $(node -v)"
} else {
  Write-Warn "node not found on PATH"
}
if (Get-Command npm -ErrorAction SilentlyContinue) {
  Write-Info "npm version:     $(npm -v)"
} else {
  Write-Warn "npm not found on PATH"
}

if ($CheckOnly) {
  Write-Info "Check-only run complete."
  exit 0
}

# 2) Ensure Redis
$redis = Start-RedisIfNeeded

# 3) Dev cert
Ensure-DevCert

# 4) Start backend and verify health
$backend = Start-Backend
Write-Info "Waiting for backend health at http://localhost:5028/healthz ..."
$apiHealthy = Wait-HttpOk -url 'http://localhost:5028/healthz' -timeoutSec 90
if (-not $apiHealthy) {
  Write-Err "Backend health check failed. See backend output above."
  try { if ($backend -and -not $backend.HasExited) { $backend.Kill() } } catch {}
  if ($redis.Started) { Write-Warn "Redis container 'rsa-redis' remains running." }
  exit 1
}
Write-Info "Backend is healthy. Swagger: https://localhost:7215/swagger"

# 5) Start frontend and verify it responds
$frontend = Start-Frontend
Write-Info "Waiting for frontend at http://localhost:4200 ..."
$feReady = Wait-HttpOk -url 'http://localhost:4200' -timeoutSec 120
if (-not $feReady) {
  Write-Err "Frontend did not become ready. See frontend output above."
  try { if ($backend -and -not $backend.HasExited) { $backend.Kill() } } catch {}
  try { if ($frontend -and -not $frontend.HasExited) { $frontend.Kill() } } catch {}
  if ($redis.Started) { Write-Warn "Redis container 'rsa-redis' remains running." }
  exit 1
}
Write-Info "Frontend is ready at http://localhost:4200"

if ($OpenBrowser) {
  try { Start-Process 'http://localhost:4200' | Out-Null } catch {}
}

if ($ExitAfterReady) {
  Write-Info "ExitAfterReady is set. Stopping child processes and exiting."
  try { if ($frontend -and -not $frontend.HasExited) { $frontend.Kill() } } catch {}
  try { if ($backend -and -not $backend.HasExited) { $backend.Kill() } } catch {}
  exit 0
}

Write-Host ""; Write-Info "All services are running."
Write-Host "- Frontend: http://localhost:4200"
Write-Host "- API (HTTP): http://localhost:5028"
Write-Host "- API (HTTPS): https://localhost:7215"
Write-Host "- Health: http://localhost:5028/healthz"
Write-Host ""
if ($ApiKey) {
  Write-Host "Use this API key in the UI: $ApiKey"
} else {
  Write-Host "Dev tip: In Development, the API accepts any non-empty API key if API_KEY isn't set."
}
Write-Host "Press Ctrl+C to stop..."

# 6) Wait and handle Ctrl+C
try {
  while ($true) {
    if ($backend.HasExited -or $frontend.HasExited) { break }
    Start-Sleep -Seconds 1
  }
} finally {
  Write-Info "Shutting down processes..."
  try { if ($frontend -and -not $frontend.HasExited) { $frontend.Kill() } } catch {}
  try { if ($backend -and -not $backend.HasExited) { $backend.Kill() } } catch {}
}

Write-Info "Done."
