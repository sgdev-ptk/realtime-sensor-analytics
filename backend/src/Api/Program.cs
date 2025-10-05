using Api;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use dev certificate from environment if provided
var certPath = builder.Configuration["ASPNETCORE_Kestrel:Certificates:Default:Path"]
              ?? builder.Configuration["ASPNETCORE_Kestrel__Certificates__Default__Path"];
var certPassword = builder.Configuration["ASPNETCORE_Kestrel:Certificates:Default:Password"]
                 ?? builder.Configuration["ASPNETCORE_Kestrel__Certificates__Default__Password"];
if (!string.IsNullOrWhiteSpace(certPath) && File.Exists(certPath))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureHttpsDefaults(https =>
        {
            https.ServerCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath, certPassword);
        });
    });
}

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// Capture API key from configuration (can be null/empty in dev)
var apiKey = builder.Configuration["API_KEY"];

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Basic security headers
app.Use((ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["X-XSS-Protection"] = "0";
    return next();
});

// Allow dev CORS - tighten later
app.UseCors(policy => policy
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .SetIsOriginAllowed(_ => true));

// API key middleware for protected endpoints
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value ?? string.Empty;
    var requiresKey = path.StartsWith("/api/ack", StringComparison.OrdinalIgnoreCase)
                      || path.StartsWith("/api/stream", StringComparison.OrdinalIgnoreCase);
    if (!requiresKey)
    {
        await next();
        return;
    }

    var provided = ctx.Request.Headers["x-api-key"].ToString();
    var acceptAnyWhenNotConfigured = string.IsNullOrWhiteSpace(apiKey);
    if ((acceptAnyWhenNotConfigured && !string.IsNullOrWhiteSpace(provided)) || provided == apiKey)
    {
        await next();
        return;
    }

    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
});

// Metrics endpoint (Prometheus format can be added later); return 200 OK for contract
app.MapGet("/api/metrics", () => Results.Text("ok", "text/plain"));

// Ack alert endpoint (requires API key via middleware)
app.MapPost("/api/ack/{alertId}", (string alertId) => Results.NoContent());

// SignalR stream hub mapping (requires API key via middleware)
app.MapHub<StreamHub>("/api/stream");

app.Run();
