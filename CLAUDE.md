- Follow @docs/AGENTS.md instructions without exceptions
- Ignore the "archived" directory
- During development don't EVER write to my personal ~/.km dir
- For any task with multiple steps, create a todo list FIRST, then execute.
- Avoid destructive operations like "get reset", "git clean", etc.

# Definition of done

- [ ] instructions in `docs/AGENTS.md` have been followed without exceptions
- [ ] magic values and constants are centralized in `Constants.cs`
- [ ] `build.sh` runs successfully without warnings or errors
- [ ] `format.sh` runs successfully without warnings or errors
- [ ] `coverage.sh` runs successfully without warnings or errors
- [ ] there are zero skipped tests

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
