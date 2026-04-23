# Contributing to SecureNova

Thank you for your interest in improving SecureNova.

## Development Setup

1. Install .NET SDK 8.0 and .NET SDK 6.0.
2. Clone the repository.
3. Open `SecureNova.sln` in Visual Studio 2022 or VS Code.
4. Restore dependencies:
   - `dotnet restore SecureNova.sln`

## Project Structure

- `src/SecureNova.GUI`: WPF desktop client (.NET 8)
- `src/SecureNova.Service`: Windows service host (.NET 6)
- `src/SecureNova.ps1`: Core PowerShell scanning script

## Branches and Commits

- Create feature branches from `main`.
- Use clear commit messages in imperative form.
- Keep pull requests focused and small when possible.

## Pull Request Checklist

- Code builds successfully in Release mode.
- No generated files from `bin/`, `obj/`, logs, or findings are included.
- README/docs updated for behavior changes.
- Security-sensitive changes include rationale and test notes.

## Reporting Bugs

Please use GitHub Issues and include:

- Steps to reproduce
- Expected behavior
- Actual behavior
- OS and .NET SDK versions
- Relevant logs (redacted)
