---
name: dotnet-best-practices
description: 'How to write .NET/C# in the RemoteInstallationEngine (ring) solution. Use before writing, reviewing, or changing any C# code.'
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

## Design Patterns & Architecture

- Use primary constructor syntax for dependency injection (e.g., `public class MyClass(IDependency dependency)`)
- Use interface segregation with clear naming conventions (prefix interfaces with 'I')
- Follow the Factory pattern for complex object creation
- DO NOT create interfaces for non-integration service classes.

## Dependency Injection & Services

- Use constructor dependency injection with null checks via ArgumentNullException
- Register services with appropriate lifetimes (Singleton, Scoped, Transient)
- Use Microsoft.Extensions.DependencyInjection patterns
- Implement service interfaces for testability

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
- Test both success and failure scenarios
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
- Prefer the conventions already established in the touched project over generic framework defaults
- Reuse existing services, abstractions, and patterns before introducing new ones
- If behavior crosses multiple projects, update only the affected path through the solution instead
  of broad cleanup, suggesting improvements, unless the user explicitly requests it
- Suggest cleanup when relevant, but do not perform broad cleanup work unless the user explicitly requests it
