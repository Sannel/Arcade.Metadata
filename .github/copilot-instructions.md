# GitHub Copilot Instructions

## Code Style and Formatting

When generating or modifying code in this repository, **always follow the `.editorconfig` settings** located in the root of the repository. Key formatting requirements include:

### General Formatting
- Use **tabs** for indentation (not spaces) for all files except YAML files
- For YAML files (`.yml`, `.yaml`): use 2 spaces for indentation
- Do not insert final newline in C# files

### C# Specific Guidelines
- Use **file-scoped namespaces** (`namespace Foo;` instead of `namespace Foo { }`)
- Use **top-level statements** when appropriate
- Prefer **primary constructors** for classes
- Use **expression-bodied members** for simple properties and accessors
- Place opening braces on new lines (Allman style)
- Use **explicit types** instead of `var` unless type is apparent
- Use **PascalCase** for public members, types, and namespaces
- Use **camelCase** with underscore prefix (`_fieldName`) for private fields
- Use **camelCase** with `s_` prefix for private static fields
- Interface names should start with `I` (e.g., `IService`)
- Type parameters should start with `T` (e.g., `TEntity`)

### Blazor/Razor Specific Guidelines
- Follow standard Blazor component conventions
- Use proper `@page` directives
- Use `@rendermode` attributes appropriately for WebAssembly components
- Maintain consistent HTML structure and Bootstrap classes where used

### Code Quality
- Enable nullable reference types (`<Nullable>enable</Nullable>`)
- Use implicit usings where available
- Organize using statements with System directives first
- Prefer collection expressions when types loosely match
- Use pattern matching where appropriate
- Prefer readonly fields and properties when possible

## Project Context
This is a .NET 9 Blazor WebAssembly application with the following projects:
- `Sannel.Arcade.Metadata` (Server/Host project)
- `Sannel.Arcade.Metadata.Client` (WebAssembly client project)

When suggesting code changes, ensure compatibility with .NET 9 and Blazor WebAssembly patterns.

## Important Notes
- **Always respect the existing .editorconfig file** - do not suggest changes that violate these settings
- When in doubt about formatting, refer to the `.editorconfig` file in the repository root
- Maintain consistency with existing code patterns in the repository
- Use tabs for indentation consistently across all C# files