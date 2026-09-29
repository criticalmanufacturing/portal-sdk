---
name: dotnet-best-practices
description: 'How to write .NET/C# in the Customer Portal SDK (portal-sdk) solution. Use before writing, reviewing, or changing any C# code.'
user-invocable: false
---

<!--
  FORK of github/awesome-copilot :: dotnet-best-practices
  Merge base: 9ef94db7f06c167d544369817e5a4415eeb7886dcff0d47026837a1aada260e3
-->

# .NET/C# Best Practices

## Documentation & Structure

- XML documentation comments on all public classes, interfaces, methods, and properties, including
  parameter and return descriptions
- Namespaces follow the folder depth
- Use file-scoped namespaces (`namespace X.Y;`)

## Design Patterns & Architecture

- Use primary constructor syntax for dependency injection (e.g., `public class MyClass(IDependency dependency)`)
- Use interface segregation with clear naming conventions (prefix interfaces with 'I')
- Follow the Factory pattern for complex object creation
- DO NOT create an interface unless it is needed: the class integrates with an external system, there are
  (or will be) multiple implementations, or its logic is complex enough to promote it to a first-class,
  independently mockable service. Otherwise depend on the concrete class (e.g. handlers are registered
  and resolved as concrete types)

## Dependency Injection & Services

- Use constructor dependency injection with null checks via ArgumentNullException
- Register services with appropriate lifetimes (Singleton, Scoped, Transient)
- Use Microsoft.Extensions.DependencyInjection patterns
- When a service has an interface, depend on the interface so it can be mocked

## Dependencies & Upgrades

- Before proposing a framework or package version change, check the official release notes, breaking
  changes, and migration guide for that version — never bump on version number alone
- Call out any breaking change you find explicitly, with the migration step it implies

## Resource Management & Localization

- Use ResourceManager for localized messages and error strings
- Separate LogMessages and ErrorMessages resource files
- Access resources via `_resourceManager.GetString("MessageKey")`

## Async/Await Patterns

- Use async/await for all I/O operations and long-running tasks
- Return Task or Task<T> from async methods
- Return ValueTask or ValueTask<T> where appropriate for performance-sensitive scenarios
- Use ConfigureAwait(false) where appropriate, e.g. in library code that must not capture a synchronization context
- Handle async exceptions properly

## Testing Standards

- Use MSTest or xUnit with **plain `Assert`**
- Follow AAA pattern (Arrange, Act, Assert)
- Use Moq for mocking dependencies, filesystem mocking with `MockFileSystem` (`System.IO.Abstractions.TestingHelpers`) and HTTP mocking with `RichardSzalay.MockHttp`
- Write use-case tests: the happy path, each failure path, and specific edge cases
- Assert on what the use case produces: the result, the exception, what was logged or written, and the calls
  made across integration boundaries. Verifying calls to an external system is expected, whether it is an HTTP
  request (MockHttp) or a call to an interface wrapping a library or external system, because that call is the
  operation's effect
- Don't make a test out of verifying calls to internal collaborators that are only implementation steps. A test that
  only checks forwarding can pass while the use case is broken
- Include null parameter validation tests

## Configuration & Settings

- Use strongly-typed configuration classes with data annotations
- Implement validation attributes (Required, NotEmptyOrWhitespace)
- Use IConfiguration binding for settings
- Support appsettings.json configuration files

## Error Handling & Logging

- Use structured logging with Microsoft.Extensions.Logging
- Include scoped logging with meaningful context
- Throw specific exceptions with descriptive messages
- Use try-catch blocks for expected failure scenarios

## Performance & Security

- Use C# 14+ features and .NET 10 optimizations where applicable
- Implement proper input validation and sanitization
- Use parameterized queries for database operations
- Read credentials and tokens from configuration or environment, NEVER hardcode them

## Resiliency

- Wrap calls to external systems in retry policies using **Polly** wherever possible for transient network errors, using jitter
- Resiliently handle timeouts and service unavailability, and log failures

## Code Quality

- Ensure SOLID principles compliance
- Avoid code duplication through base classes and utilities
- Use meaningful names that reflect domain concepts
- Keep methods focused and cohesive
- Implement proper disposal patterns for resources
- Access the file system through `System.IO.Abstractions` (`IFileSystem`), never `System.IO.File`/`Directory`
  directly, so the code stays testable with `MockFileSystem`
- Prefer the conventions already established in the touched project over generic framework defaults
- Reuse existing services, abstractions, and patterns before introducing new ones
- If behavior crosses multiple projects, update only the affected path through the solution instead
  of broad cleanup, suggesting improvements, unless the user explicitly requests it
- Suggest cleanup when relevant, but do not perform broad cleanup work unless the user explicitly requests it
