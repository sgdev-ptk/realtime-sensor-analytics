# Slash commands in this repo

This repository includes a set of Copilot Chat slash commands wired to the prompt files under `.github/prompts`. Use them in Copilot Chat or compatible agents to drive a structured spec → plan → tasks → implement workflow.

## Available commands

- `/specify <feature description>`
  - Creates a new feature spec using `.specify/templates/spec-template.md`.
  - Internally calls `.specify/scripts/powershell/create-new-feature.ps1` once to scaffold a feature branch and spec file.

- `/clarify [context or notes]`
  - Asks up to 5 targeted questions and writes accepted answers back into the spec under `## Clarifications`.
  - Runs after `/specify` and before `/plan`.

- `/plan [additional technical context]`
  - Generates design artifacts (plan.md, data-model.md, contracts/, quickstart.md, research.md) using the plan template.
  - Requires the spec to exist and recommends clarifications first.

- `/tasks [notes]`
  - Produces a dependency-ordered `tasks.md` from the available design artifacts.

- `/analyze [notes]`
  - Read-only cross-check of `spec.md`, `plan.md`, and `tasks.md` for gaps or inconsistencies.

- `/implement [scope or notes]`
  - Executes the work defined in `tasks.md` phase by phase, marking tasks complete.

- `/constitution [principle updates]`
  - Updates `.specify/memory/constitution.md` and syncs related templates.

## Prerequisites

- Windows is supported. The prompts call PowerShell scripts located in `.specify/scripts/powershell/`.
- Git is recommended (the `/specify` flow creates/uses a feature branch).
- Use Copilot Chat inside VS Code for best results.

## Typical workflow

1) Draft a feature: `/specify Build a streaming dashboard to visualize live sensor metrics (latency, throughput, error rate) with alerting.`
2) Resolve ambiguities: `/clarify` (answer up to 5 short questions)
3) Generate a plan: `/plan` (optionally pass stack hints)
4) Create actionable tasks: `/tasks`
5) Sanity check: `/analyze`
6) Build it: `/implement`

## Notes

- Commands assume the repository root as working directory.
- If a prerequisite document is missing, the command will indicate which step to run first.
- All paths used by prompts are absolute to avoid ambiguity on Windows.