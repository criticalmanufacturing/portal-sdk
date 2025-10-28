# Copilot instructions for portal-sdk

This is a .NET 8 multi-project solution that ships a Customer Portal SDK as:
- Core library: `src/Common` (handlers, services, utilities)
- Console CLI: `src/Console` (System.CommandLine commands)
- PowerShell module: `src/Powershell` (cmdlets wrapping handlers)
- Unit tests: `src/Common.UnitTests` (xUnit + Moq)

## Architecture and conventions
- Handlers derive from `AbstractHandler` and encapsulate operations; they use `ISession` for login, logging, and configuration.
  - Examples: `NewEnvironmentHandler`, `UndeployEnvironmentHandler`
- CLI and PowerShell are thin shells that resolve handlers via `ServiceLocator` and forward parameters.
  - CLI example: `src/Console/UndeployCommand.cs`
  - PS example: `src/Powershell/UndeployEnvironment.cs`
- Services abstract portal interactions:
  - `ICustomerEnvironmentServices` (create/update environments, terminate other versions)
  - `ICustomerPortalClient` (fetch objects, termination logs)
  - `INewEnvironmentUtilities` (deployment target mapping, connection checks)
- Help/UX strings live in `src/Common/Resources.cs`; keep CLI/PS help in sync via these constants.
- Filesystem abstractions: use `System.IO.Abstractions` (`IFileSystem`) in handlers to stay testable.

## Critical behaviors
- Termination API: `CustomerEnvironmentServices.TerminateOtherVersions(CustomerEnvironment env, bool remove, bool removeVolumes, bool undeploy)`
  - Undeploy flow (handler): pass `undeploy: true`. See `UndeployEnvironmentHandler.Run`.
  - Normal deploy/update flows: pass `undeploy: false`. See `NewEnvironmentHandler.Run`.
  - On termination failures (returned failed IDs), fetch logs via `ICustomerPortalClient.GetCustomerEnvironmentTerminationLogs(id)` and log them to the user via `ISession.LogError`.
- Feature previews: the `undeploy` feature is [Preview]. Mark descriptions accordingly and log a preview notice at runtime.

## Developer workflows
- Build solution:
  ```powershell
  dotnet build "c:\Users\v-moreira\product\portal-sdk\Cmf.CustomerPortal.Sdk.sln" --nologo
  ```
- Run unit tests:
  ```powershell
  dotnet test "c:\Users\v-moreira\product\portal-sdk\src\Common.UnitTests\Common.UnitTests.csproj" --nologo
  ```
- VS Code tasks (Console project): build, publish, watch are pre-configured for `src/Console/Console.csproj`.

## Testing patterns
- Tests live in `src/Common.UnitTests`; use xUnit + Moq.
- Prefer small, focused tests that verify interactions (Moq Verify) over end-to-end behaviors.
  - Example: `UndeployEnvironmentHandlerTests` checks argument validation, environment existence, connection check, and correct forwarding of the `removeVolumes` flag and `undeploy: true`.
  - For failure paths (e.g., terminate failures), assert that termination logs are retrieved and logged.

## Navigation quick links
- Handlers: `src/Common/Handlers/*Handler.cs`
- Services/interfaces: `src/Common/Services/*.cs` (e.g., `ICustomerEnvironmentServices`)
- CLI commands: `src/Console/*Command.cs`
- PowerShell cmdlets: `src/Powershell/*.cs`
- Shared resources: `src/Common/Resources.cs`
- Unit tests: `src/Common.UnitTests/Handlers/*Tests.cs`

If you change a service signature, update all handler call sites and related tests in the same PR. If anything’s unclear or you spot repo-specific patterns not captured here, please update this file with concrete examples.