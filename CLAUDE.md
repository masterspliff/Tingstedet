# CLAUDE.md - Project Guidelines

## Commands
- Build: `dotnet build`
- Run webapp: `dotnet run --project webapp`
- Run server: `dotnet run --project server`
- Watch webapp: `dotnet watch --project webapp`
- Watch server: `dotnet watch --project server`
- Test: `dotnet test`
- Test specific: `dotnet test --filter "FullyQualifiedName~Tingstedet.Tests.TestClass.TestMethod"`
- Lint/Format: `dotnet format`
- Clean: `dotnet clean`
- Restore: `dotnet restore`
- Publish: `dotnet publish -c Release`

## Code Style
- **Naming**: PascalCase for classes/interfaces/methods, camelCase for variables/parameters, _camelCase for private fields
- **Component naming**: Directories follow PascalCase, component parameters use camelCase
- **Null handling**: Nullable is enabled, use null checks or null-conditional operators
- **Types**: Always use explicit types unless obvious from initialization
- **Imports**: Group imports by type (System, Microsoft, local), sort alphabetically
- **Formatting**: 4-space indentation, braces on new lines, line length < 120 chars
- **Error handling**: Prefer exceptions for failures, use try/catch for expected errors
- **Event handling**: UI callbacks follow On{Action} pattern, handlers follow Handle{Action} pattern
- **Documentation**: XML comments for public APIs

## Project Structure
- **Solution**: Multi-project solution with core, webapp, and server projects
- **Core**: Contains shared models and domain logic
- **Webapp**: Blazor WebAssembly frontend targeting .NET 7.0
- **Server**: Backend API
- **Components**: Organized by feature in Components directory (e.g., Post, Sidebar)
- **Pages**: Contains routable components
- **Services**: Follow interface-based pattern for dependency injection
- **UI**: Uses BlazorBootstrap library with placeholder components for loading states