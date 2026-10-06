# Agent instructions for portal-sdk

This is a .NET 10 multi-project solution (`Cmf.CustomerPortal.Sdk.sln`) that ships a Customer Portal SDK as:
- Core library: `src/Common` (handlers, services, utilities)
- Console CLI: `src/Console` (`cmf-portal`, System.CommandLine commands)
- PowerShell module: `src/Powershell` (cmdlets wrapping handlers)
- Unit tests:
  - `src/Common.UnitTests` for handlers and services
  - `src/Console.UnitTests` parses the real command tree via `CommandLineFixture`/`RootCommandFactory`, no mocks

C# coding standards live in the `dotnet-best-practices` skill; repo skills are in `.claude/skills` (mirrored in `.agents/skills`).

## Architecture and conventions
- Handlers (`src/Common/Handlers`) derive from `AbstractHandler` and encapsulate one operation.
  They call `EnsureLogin()` before talking to the portal and log through `ISession` (`LogInformation`, `LogError`, `LogDebug`).
  - Examples: `NewEnvironmentHandler`, `UndeployEnvironmentHandler`
- Every handler and service is registered in `CommonModule.RegisterCommon` (`src/Common/CommonModule.cs`):
  handlers as concrete transients, services as interface → implementation.
- `ServiceLocator` (`src/Common/ServiceLocator.cs`) builds the container from `CommonModule` and adds `IFileSystem`,
  `IConfiguration` (`appsettings.json` + environment variables) and the `ISession`.
- The Console and PowerShell projects are thin shells: they create a session, resolve the handler from `ServiceLocator`
  and forward parameters. No business logic belongs there.
- Services (`src/Common/Services`) abstract portal interactions:
  - `ICustomerEnvironmentServices` (create/update environments, terminate other versions)
  - `ICustomerPortalClient` (fetch objects, termination logs)
  - `IEnvironmentUtilities` (deployment target mapping, connection checks)
- Help/UX strings are `const string` members of the partial `Resources` class, split by area in
  `src/Common/Resources/Resources.*.cs` (namespace `Cmf.CustomerPortal.Sdk.Common`). CLI and PowerShell help both use them.
- File access goes through `System.IO.Abstractions` (`IFileSystem`) so handlers stay testable.

### Console entry point
- `src/Console/Program.cs` → `RootCommandFactory.Create()` (`src/Console/RootCommandFactory.cs`) builds the
  `verb noun [name] [options]` command tree. New verbs/nouns must be wired there.
- Commands derive from `BaseCommand` (`src/Console/Base/BaseCommand.cs`), which adds `--verbose` and exposes
  `CreateSession(parseResult)` to create the session and `ServiceLocator`.
- A new noun is a single command class under `src/Console/Commands/<Verb>/` that derives from `BaseCommand`,
  declares its arguments and options, and runs its handler in its action.
  - The existing `*CommandBase` classes only share options and actions with the deprecated legacy commands; don't add new ones.
- Shared option sets are `IOptionExtension`s in `src/Console/Extensions`, added with `BaseCommand.Use(...)`.
- Legacy flat commands live in `src/Console/Commands/Legacy`. They are deprecated (`DeprecationMessage`, printed to stderr)
  but keep working; legacy commands that share a verb name (`deploy`, `undeploy`, `publish`) become the verb via `AsVerb`.
  Their options only parse without a noun: before a noun they are rejected, as on any other verb.

### PowerShell entry point
- Cmdlets derive from `BaseCmdlet<THandler>` (`src/Powershell/Base/BaseCmdlet.cs`, built on `AsyncCmdlet`), override
  `ProcessRecordAsync` and resolve the handler from `ServiceLocator`.
  - Example: `src/Powershell/UndeployEnvironment.cs`
- Shared dynamic parameters are `IParameterExtension`s in `src/Powershell/Extensions`.

## Critical behaviors
- Termination API: `ICustomerEnvironmentServices.TerminateOtherVersions(CustomerEnvironment env, bool remove, bool removeVolumes, bool undeploy)`
  - Undeploy flow: pass `undeploy: true`. See `UndeployEnvironmentHandler.Run`.
  - Normal deploy/update flows: pass `undeploy: false`. See `NewEnvironmentHandler.Run`.
  - On termination failures, `CustomerEnvironmentServices.TerminateOtherVersions` fetches each failed environment's logs via
    `ICustomerPortalClient.GetCustomerEnvironmentTerminationLogs(id)`, logs them with `ISession.LogError` and throws.

## Developer workflows
- Build the solution:
  ```powershell
  dotnet build Cmf.CustomerPortal.Sdk.sln --nologo
  ```
- Run all unit tests (both test projects, as CI does):
  ```powershell
  dotnet test Cmf.CustomerPortal.Sdk.sln --nologo
  ```
- PowerShell module: `.\run.ps1` opens a new `pwsh` that builds, publishes and imports the module
  (`.\import.ps1 -BuildAndPublish` does it in the current session).
- VS Code tasks (Console project): build, publish, watch are pre-configured for `src/Console/Console.csproj`.

# How to work here

If you change a service signature, update all handler call sites and related tests in the same PR.
If something's unclear, state your assumptions or ask to clean doubts.
