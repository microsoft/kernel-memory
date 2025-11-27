- Follow @docs/AGENTS.md instructions without exceptions
- Ignore the "archived" directory

# Definition of done

- `format.sh` is passing without errors or warnings
- `build.sh` is passing without errors or warnings
- `coverage.sh` is passing without errors or warnings, coverage > 80%

# C# Code Style

- Use .NET 10 and C# 14
- Always use `this.` prefix
- Async methods have mandatory `Async` name suffix (optional only for tests, not required for `Main` method)
- Keep magic values and constants in a centralized `Constants.cs` file
- One class per file, matching the class name with the file name
- Sort class methods by visibility: public first, private at the end
- Sort class fields and const by visibility: private, const, props
- Keep all fields and consts at the top of classes
- Ensure dirs and paths logic is cross-platform compatible
- Avoid generic/meaningless names like "Utils" "Common" "Lib"
- Use plural for Enum names, e.g. "EmbeddingsTypes"
- Always use explicit visibility
- Don't use primary constructors
- Use DateTimeOffset, don't use DateTime
