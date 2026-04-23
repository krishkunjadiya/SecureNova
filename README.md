# SecureNova

SecureNova is a Windows-focused security monitoring stack that combines a PowerShell detection engine with .NET desktop and service components.

This repository is organized for local operations and GitHub-based collaboration, with CI and issue templates included.

## What It Includes

- Keylogger and suspicious hook detection heuristics
- Suspicious file change monitoring in sensitive directories
- Process execution analysis for risky paths and unsigned binaries
- JSON findings export plus timestamped log output
- WPF GUI project and Windows service project for extensibility

## Repository Structure

- `SecureNova.sln` - Solution root
- `src/SecureNova.ps1` - Core scanning engine script
- `src/config.json` - Runtime scanner configuration
- `src/SecureNova.GUI` - WPF desktop application (`net8.0-windows`)
- `src/SecureNova.Service` - Service host (`net8.0-windows`)

## Prerequisites

- Windows 10 or Windows 11
- .NET SDK 8.0 (for GUI project)
- PowerShell 5.1 or later
- Administrator privileges (required for full scan visibility)

## Quick Start (Script)

1. Open PowerShell as Administrator.
2. Move to the repository root.
3. Run:

```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
cd src
.\SecureNova.ps1
```

## Build

```powershell
dotnet restore SecureNova.sln
dotnet build SecureNova.sln --configuration Release
```

## Configuration

Update `src/config.json` to tune scan behavior:

- `scanInterval`
- `sensitiveDirectories`
- `suspiciousExtensions`
- `excludeProcesses`
- `logRetentionDays`
- `alertThresholds`

## Output

- Text logs: `src/logs/`
- JSON findings: `src/findings/`

These paths are ignored by Git through `.gitignore`.

## CI and Maintenance

- GitHub Actions CI builds the solution on pull requests and pushes.
- PowerShell script syntax validation runs in CI.
- Dependabot is configured for NuGet and GitHub Actions updates.

## Security Reporting

Please report vulnerabilities privately through your repository security contact channel.

## Contributing

Please open an issue first to discuss major changes before submitting a pull request.

## License

Licensed under the MIT License. See `LICENSE` for details.

## Disclaimer

SecureNova is a defensive monitoring project. Validate detections and response playbooks in a controlled environment before production use.