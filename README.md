# VitaCernita

> Cross-platform Core C# (.NET 9) Gmail filter management engine configured via Lua DSL.

VitaCernita pairs modern .NET performance with the flexibility of a declarative and functional Lua DSL for managing Gmail search filters.

---

## Gmail Filter Configuration

VitaCernita supports boolean logic (`AND`, `OR`), field matches (`from`, `subject`), and arbitrary levels of composite nesting.

### Supported Operators & Syntax

#### 1. Simple `AND` Condition
Combines field criteria with an `AND` relation (emitted as canonical space-separated Gmail query syntax):
```lua
return rule {
    match = And(
        From("alerts@monitoring.com"),
        Subject("High CPU")
    )
}
-- Result: from:alerts@monitoring.com subject:"High CPU"
```

#### 2. Simple `OR` Condition
Matches any of multiple criteria using the uppercase `OR` operator:
```lua
return rule {
    match = Or(
        From("alice@example.com"),
        Subject("Urgent")
    )
}
-- Result: from:alice@example.com OR subject:Urgent
```

#### 3. `OR` Containing Matches of `AND`
Combines multiple distinct combinations of `from` and `subject`:
```lua
return rule {
    name = "Tri-Team Incident Dispatcher",
    match = Or(
        And(From("secops@company.com"), Subject("Security Breach")),
        And(From("devops@company.com"), Subject("Cluster Outage")),
        And(From("netops@company.com"), Subject("BGP Route Leak"))
    )
}
-- Result: (from:devops@company.com subject:"Cluster Outage") OR (from:netops@company.com subject:"BGP Route Leak") OR (from:secops@company.com subject:"Security Breach")
```

#### 4. `AND` Containing `OR`
Combines a sender match with an alternative set of subjects:
```lua
return rule {
    name = "Executive Escalations",
    match = And(
        From("ceo@company.com"),
        Or(
            Subject("Immediate Action Required"),
            Subject("Board Resolution"),
            Subject("Urgent")
        )
    )
}
-- Result: from:ceo@company.com (subject:"Board Resolution" OR subject:"Immediate Action Required" OR subject:Urgent)
```

#### 5. Declarative Table Syntax
Equivalent logic can also be declared using pure tables:
```lua
return {
    rules = {
        {
            ["or"] = {
                { from = "devops@company.com", subject = "Outage" },
                { from = "secops@company.com", subject = "Breach" }
            }
        }
    }
}
```

---

## Getting Started

### Build & Test

```bash
# Build solution
dotnet build

# Run unit tests (including Boundary Value Analysis suite)
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
