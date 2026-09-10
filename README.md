# VitaCernita

> Cross-platform Core C# (.NET 9) triage and decision engine paired with dynamic Lua configuration.

VitaCernita pairs modern .NET performance with the flexibility of Lua scripting. Configuration is written in pure Lua, allowing dynamic settings, runtime hooks, scoring algorithms, and screening rules without recompilation.

---

## Architecture & Technologies

- **Main Program**: C# (.NET 9 Core), cross-platform (Linux, macOS, Windows).
- **Lua Interpreter**: [Lua-CSharp](https://github.com/nuskey8/Lua-CSharp) — A high-performance, pure C# Lua 5.3 interpreter with async/await support, zero native binary dependencies, and Native AOT compatibility.
- **CLI & Formatting**: [Spectre.Console](https://spectreconsole.net/) for terminal UI.
- **Testing**: xUnit test suite.
- **Version Control**: Managed with [Jujutsu (`jj`)](https://github.com/jj-vcs/jj) with a colocated Git repository.

---

## Project Structure

```
VitaCernita/
├── .gitignore
├── .mise.toml
├── README.md
├── VitaCernita.sln
├── config/
│   ├── config.lua               # Active configuration
│   └── config.example.lua       # Annotated example configuration
├── src/
│   ├── VitaCernita/             # CLI application entry point
│   └── VitaCernita.Core/        # Configuration loader, Lua interpreter, domain logic
└── tests/
    └── VitaCernita.Tests/       # Unit tests
```

---

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/) (managed via `mise` or system package manager)

### Build & Test

```bash
# Build the solution
dotnet build

# Run unit tests
dotnet test
```

### Running the Application

```bash
# Run with default config (config/config.lua)
dotnet run --project src/VitaCernita

# Validate configuration file
dotnet run --project src/VitaCernita -- validate -c config/config.lua

# Evaluate an arbitrary Lua expression
dotnet run --project src/VitaCernita -- eval "return 40 + 2"
```

---

## Lua Configuration

Configurations are standard Lua scripts that return a table (or declare a global `config = { ... }`).

```lua
return {
    project = {
        name = "VitaCernita",
        version = "0.1.0",
        description = "Core C# with Lua Configuration",
        author = "Phillip Dressen",
    },

    settings = {
        log_level = env("VITACERNITA_LOG_LEVEL", "Information"),
        max_concurrency = 8,
        output_dir = "./output",
        dry_run = false,
        timeout_seconds = 30,
    },

    rules = {
        {
            id = "rule-critical",
            priority = 500,
            enabled = true,
            category = "Critical",
        },
    },

    custom = {
        host_os = platform(), -- returns "Linux", "macOS", or "Windows"
    },

    -- Dynamic hook: executed in C# via the embedded Lua interpreter
    calculate_score = function(urgency, effort)
        return (urgency * 2.5) - (effort * 0.8)
    end,
}
```

### Built-in Lua Helpers

- `env(name, default)`: Retrieves environment variables with fallback.
- `platform()`: Returns host OS name (`Linux`, `macOS`, `Windows`).

---

## Managing the Repo with Jujutsu (`jj`)

This repository is colocated (`.jj/` and `.git/`). All version control operations can be performed using `jj`:

```bash
# Check working copy status
jj st

# View revision history
jj log

# Create a new change
jj new

# Describe / commit message
jj describe -m "feat: your change description"

# Update main bookmark to current revision
jj bookmark set main -r @

# Push bookmark to GitHub
jj git push --bookmark main
```
