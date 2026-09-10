# VitaCernita

> Cross-platform Core C# (.NET 9) Gmail filter management engine configured via Lua DSL.

VitaCernita pairs modern .NET performance with the flexibility of a declarative and functional Lua DSL for managing Gmail search filters.

---

## Gmail Filter Configuration

VitaCernita allows defining Gmail filters using multiple expressive Lua styles. All of the following forms define a rule matching both `from` and `subject` with an `and` operator, and all compile into the precise canonical Gmail search filter text:

```
from:alerts@monitoring.com subject:"High CPU"
```

### Supported Lua Syntaxes

#### 1. Functional DSL (Recommended)
```lua
return rule {
    name = "Production Incident Alert Filter",
    match = And(
        From("alerts@monitoring.com"),
        Subject("High CPU")
    )
}
```
*Note: Argument order does not matter; canonical ordering guarantees identical output regardless of whether `Subject` or `From` is specified first.*

#### 2. Declarative Table with Explicit `and` Map
```lua
return {
    ["and"] = {
        from = "alerts@monitoring.com",
        subject = "High CPU"
    }
}
```

#### 3. Declarative Table with Explicit `and` Array
```lua
return {
    ["and"] = {
        { from = "alerts@monitoring.com" },
        { subject = "High CPU" }
    }
}
```

#### 4. Method-Chaining / Fluent Builder
```lua
return filter():from("alerts@monitoring.com"):subject("High CPU")
```

#### 5. Declarative Table with Direct Properties (Implicit AND)
```lua
return {
    from = "alerts@monitoring.com",
    subject = "High CPU"
}
```

---

## Getting Started

### Build & Run

```bash
# Build solution
dotnet build

# Run unit tests
dotnet test

# Run and print the precise Gmail filter text
dotnet run --project src/VitaCernita

# Load a custom filter configuration
dotnet run --project src/VitaCernita -- -c path/to/filter.lua

# Render with explicit 'AND' keyword
dotnet run --project src/VitaCernita -- --explicit-and
```

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
