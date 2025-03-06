# CLAUDE.md - Project Guidelines

## Commands
- Build: `dotnet build`
- Run: `dotnet run --project webapp`
- Watch: `dotnet watch --project webapp`
- Test: `dotnet test`
- Test specific: `dotnet test --filter "FullyQualifiedName~Tingstedet.Tests.TestClass.TestMethod"`
- Lint/Format: `dotnet format`
- Clean: `dotnet clean`
- Restore: `dotnet restore`
- Publish: `dotnet publish -c Release`

## Code Style
- **Naming**: PascalCase for classes/interfaces/methods, camelCase for variables/parameters, _camelCase for private fields
- **Null handling**: Nullable is enabled, use null checks or null-conditional operators
- **Types**: Always use explicit types unless obvious from initialization
- **Imports**: Group imports by type (System, Microsoft, local), sort alphabetically
- **Formatting**: 4-space indentation, braces on new lines, line length < 120 chars
- **Error handling**: Prefer exceptions for failures, use try/catch for expected errors
- **Components**: Razor components in Pages for routable components
- **Documentation**: XML comments for public APIs

## Project Structure
- Blazor WebAssembly project targeting .NET 7.0
- Pages directory contains routable components
- All component files use .razor extension