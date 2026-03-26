# Contributing

## Scope

Keep changes focused. Large features, broad refactors, or deployment changes should start with an issue or design discussion so the implementation path is clear before code lands.

## Local Setup

1. Install the .NET 9 SDK and Node.js 18 or newer.
2. Copy [`.env.example`](.env.example) and [`frontend/.env.example`](frontend/.env.example) into local-only env files as needed.
3. Start dependencies with Docker or use the Windows bootstrap script:

```powershell
.\START-ALL.bat
```

## Development Workflow

1. Branch from the current default branch.
2. Make the smallest change set that solves the problem.
3. Update documentation when behavior, setup, or operations change.
4. Keep local secrets, machine-specific config, and generated artifacts out of the commit.

## Validation

Run the checks that match your change before opening a pull request:

```powershell
dotnet test
```

```powershell
cd frontend
npm install
npm run build
```

If your change affects infrastructure, deployment, or monitoring, validate the corresponding manifests or scripts as well.

## Pull Request Expectations

- Explain the problem being solved and the approach taken
- Call out any schema, API, or operational changes
- Include screenshots for UI changes when they improve review clarity
- Note follow-up work explicitly instead of leaving hidden TODOs

## Review Checklist

- No secrets or local environment files are included
- Tests or verification steps were run and documented
- User-facing or operator-facing documentation stays consistent with the code
