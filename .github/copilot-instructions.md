# Portal SDK - Copilot Coding Agent Instructions

## Repository Overview

**Purpose**: Customer Portal SDK that allows the creation of automation scripts for integration with Critical Manufacturing DevOps Center. The SDK provides both a console executable (`cmf-portal`) and PowerShell cmdlets for managing customer infrastructure, environments, deployments, and packages.

**Repository Size**: ~76MB total (73MB in src/, primarily due to binary DLL dependencies in `src/Common/libs/`)

**Key Technologies**:
- Language: C# (.NET 8.0)
- Target Framework: net8.0
- Runtime: Requires .NET 8.0.x SDK
- Test Framework: xUnit with Moq for mocking
- Code Coverage: XPlat Code Coverage with ReportGenerator
- Package Distribution: npm package `@criticalmanufacturing/portal`
- PowerShell: Compatible with PowerShell Core 7.1.3+

## Project Structure

### Main Projects (Solution: `Cmf.CustomerPortal.Sdk.sln`)

1. **src/Common/** - Core shared library (`Cmf.CustomerPortal.Sdk.Common.dll`)
   - Services: CustomerEnvironmentServices, LicenseServices, QueryProxyService, etc.
   - Handlers: AddManifestsHandler, LoginHandler, NewEnvironmentHandler, UndeployEnvironmentHandler, InstallAppHandler, PublishPackageHandler
   - Dependencies: Uses pre-compiled DLLs from `src/Common/libs/` (Cmf.LightBusinessObjects, Cmf.MessageBus.Client, WebSocket4Net, etc.)
   - Contains 80+ C# source files

2. **src/Console/** - Console application (`cmf-portal`)
   - Commands: deploy, deployagent, publish, publish-package, login, undeploy, checkagentconnection, createinfrastructure, download-artifacts, install-app
   - Entry Point: `Program.cs`
   - Uses System.CommandLine for CLI parsing

3. **src/Powershell/** - PowerShell module (`Cmf.CustomerPortal.Sdk.Powershell.dll`)
   - Cmdlets: New-Environment, New-InfrastructureAgent, Add-Manifests, Add-Package, Set-Login, Get-AgentConnection, Undeploy-Environment
   - Base classes: AsyncCmdlet, BaseCmdlet for common functionality

4. **src/Common.UnitTests/** - Unit tests (xUnit)
   - 23 tests covering handlers and services
   - Uses Autofac.Extras.Moq, Moq, and TestableIO.System.IO.Abstractions.TestingHelpers
   - Tests run in ~200ms

### Configuration Files

- **src/Version.props**: Central version management (currently 1.17.1)
- **src/*/appsettings.json**: Configuration files for Console and Powershell projects
- **.gitignore**: Standard .NET gitignore (excludes bin/, obj/, TestResults/, etc.)

### Auxiliary Directories

- **npm/**: NPM package configuration and install script
  - `package.json`: Package metadata with version token `#{Version}#`
  - `install.js`: Post-install script for binary download
  
- **features/**: Devcontainer features for installing the SDK
  - `src/install/`: Devcontainer feature definition and install script
  - `test/install/`: Test scenarios for the feature

## Build & Test Process

### Prerequisites
- .NET 8.0 SDK (check with `dotnet --version`)
- PowerShell Core 7.1.3+ (for PowerShell module usage/testing)

### Standard Build Commands

**ALWAYS run commands in this order:**

1. **Restore**: `dotnet restore Cmf.CustomerPortal.Sdk.sln` (~20 seconds)
2. **Build**: `dotnet build Cmf.CustomerPortal.Sdk.sln --no-restore --configuration Release` (~2 seconds)
3. **Test**: `dotnet test Cmf.CustomerPortal.Sdk.sln --no-build --no-restore --configuration Release` (~2 seconds)

### Expected Build Warnings

The following warning is EXPECTED and non-blocking:
```
warning NU1902: Package 'System.IdentityModel.Tokens.Jwt' 7.0.3 has a known moderate severity vulnerability
```
Also expected:
```
warning CS9107: Parameter 'ISession session' is captured into the state of the enclosing type and its value is also passed to the base constructor.
```

### Clean Build

To perform a clean build:
```bash
dotnet clean
dotnet restore Cmf.CustomerPortal.Sdk.sln
dotnet build Cmf.CustomerPortal.Sdk.sln --no-restore --configuration Release
dotnet test Cmf.CustomerPortal.Sdk.sln --no-build --no-restore --configuration Release
```

### Publishing for Distribution

For self-contained platform-specific builds:
```bash
# Windows x64
dotnet publish --configuration Release --self-contained --runtime win-x64 Cmf.CustomerPortal.Sdk.sln

# Linux x64
dotnet publish --configuration Release --self-contained --runtime linux-x64 Cmf.CustomerPortal.Sdk.sln
```

Output locations:
- Console (Win): `src/Console/bin/Release/net8.0/win-x64/publish/`
- Console (Linux): `src/Console/bin/Release/net8.0/linux-x64/publish/`
- PowerShell: `src/Powershell/bin/Release/net8.0/linux-x64/publish/`

### PowerShell Module Import (for local testing)

**Option 1 - Build and import** (from repository root):
```powershell
.\run.ps1  # Opens new PowerShell window with module imported
```

**Option 2 - Import existing build**:
```powershell
.\import.ps1 -BuildAndPublish -Configuration Release
```

**Option 3 - Import without rebuilding**:
```powershell
Import-Module .\src\Powershell\bin\Release\net8.0\publish\Cmf.CustomerPortal.Sdk.Powershell.dll
```

## GitHub Workflows & CI/CD

### Main Build Workflow (`.github/workflows/build.yml`)

**Trigger**: Push/PR to `main` (excluding `features/**`)

**Steps**:
1. Setup .NET 8.0.x
2. `dotnet restore`
3. `dotnet build --no-restore --configuration Release`
4. `dotnet test` with XPlat Code Coverage
5. Merge coverage reports with ReportGenerator
6. Generate code coverage summary (threshold: 17% minimum, 70% high)
7. Validate line coverage meets 17% threshold
8. (On main only) Publish win-x64 and linux-x64 binaries
9. (On main only) Archive artifacts

**Coverage Requirements**: 
- Minimum line coverage: 17%
- Build fails if below threshold

### NPM Publish Workflow (`.github/workflows/npm-publish.yml`)

**Trigger**: GitHub Release published

**Steps**:
1. Build and test as above
2. Publish self-contained binaries
3. Create zip archives
4. Add zips to GitHub Release assets
5. Upload Linux build to Nexus Repository
6. Replace `#{Version}#` tokens in npm/package.json and npm/install.js
7. Publish to npm (tag: `next` for pre-release, `latest` for release)

### Devcontainer Features Test (`.github/workflows/decontainer-pr-tests.yml`)

**Trigger**: PR changes to `features/**`

**Steps**: Test devcontainer features with `@devcontainers/cli`

## Common Development Scenarios

### Adding New Commands/Cmdlets

1. Console commands: Add new command class in `src/Console/` inheriting from `BaseCommand`
2. PowerShell cmdlets: Add new cmdlet in `src/Powershell/` inheriting from `AsyncCmdlet` or `BaseCmdlet`
3. Shared logic: Add to `src/Common/Handlers/` and/or `src/Common/Services/`
4. Update `src/Console/Program.cs` to register new console commands

### Adding/Updating Dependencies

**NuGet packages**: Update `.csproj` files
**Binary DLLs**: Place in `src/Common/libs/` and reference in `Common.csproj`

**Important**: The project uses some pre-compiled DLLs that are checked into the repository:
- Cmf.LightBusinessObjects.dll (~12MB)
- Cmf.MessageBus.Client.dll
- WebSocket4Net.dll
- Others in `src/Common/libs/`

### Writing Tests

- Location: `src/Common.UnitTests/`
- Framework: xUnit with Moq for mocking
- Naming: `*Tests.cs` (e.g., `NewEnvironmentHandlerTests.cs`)
- Follow existing patterns using Autofac.Extras.Moq for dependency injection mocking

### Version Updates

**ALWAYS** update version in **ONE** place: `src/Version.props`

This file is imported by all projects and used by CI/CD workflows.

## Key Files Reference

### Root Directory
- `Cmf.CustomerPortal.Sdk.sln` - Main solution file
- `README.md` - Extensive user documentation with command examples
- `run.ps1` - Quick script to build and import PowerShell module
- `import.ps1` - Script to import PowerShell module (optionally builds first)
- `.gitignore` - Standard .NET ignore patterns

### Build Artifacts (gitignored)
- `src/*/bin/` - Build outputs
- `src/*/obj/` - Intermediate build files
- `TestResults/` - Test and coverage reports

## Best Practices for Code Changes

1. **Always build and test**: Run the full build-test cycle after changes
2. **Check coverage**: Ensure tests maintain minimum 17% line coverage
3. **Version consistency**: Only modify `src/Version.props` for version changes
4. **Binary dependencies**: Don't modify files in `src/Common/libs/` unless explicitly required
5. **PowerShell compatibility**: Test cmdlets work in PowerShell Core 7.1.3+
6. **Cross-platform**: Console app must work on both Windows and Linux (x64)
7. **Token replacement**: For release automation, use `#{Version}#` tokens in npm files

## Troubleshooting

### Build Issues

- **Missing restore**: Always run `dotnet restore` first if you get reference errors
- **Stale artifacts**: Run `dotnet clean` before `dotnet restore` and `dotnet build`
- **PowerShell import fails**: Ensure the DLL is built to the expected path and you're using PowerShell Core

### Test Failures

- Check if new code broke existing tests in `src/Common.UnitTests/`
- Run tests with `--logger "console;verbosity=detailed"` for more information
- Ensure mock objects and test data are properly configured

## Trust These Instructions

These instructions have been validated by:
- Building the solution from a clean state
- Running all 23 unit tests successfully
- Testing the publish workflow for both platforms
- Verifying the PowerShell import process
- Examining all GitHub workflows

**Only search for additional information if these instructions are incomplete or you encounter an error not documented here.**
