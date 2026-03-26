# Security Policy

## Supported Versions

Security fixes are applied to the latest state of the default branch only. Older commits, abandoned branches, and local environment snapshots are not supported.

## Reporting a Vulnerability

Do not open a public issue for suspected vulnerabilities.

Please report security issues through one of these private channels:

- GitHub private vulnerability reporting, if it is enabled for the repository
- A direct private channel already established with the repository maintainer or team

Include the following information in your report:

- Affected component or file path
- Reproduction steps or proof of concept
- Expected impact
- Any suggested mitigation or known workaround

## Response Expectations

- Initial acknowledgment target: within 3 business days
- Triage and severity assessment: as soon as the issue is reproducible
- Fix and disclosure timing: coordinated with the reporter after a patch or mitigation is ready

## Configuration Notes

The repository includes sample configuration for local and lab environments. Treat compose defaults, example env files, and local startup scripts as development scaffolding, not production-ready secrets management.
