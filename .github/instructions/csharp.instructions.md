---
applyTo: "**/*.cs"
---

# C# Implementation Rules

- Apply object-oriented principles and use types to make ownership and behavior explicit.
- Follow the unit-of-work principle: each type owns one lifetime and has an explicit interface. Lifetimes are application, operation, or step. Bind inputs that belong to an application or operation once; pass only step-varying input to each step.
- Give classes names that describe their specific purpose. Never use `Service` as a class-name suffix, even with a domain prefix; name the responsibility instead (for example, `PluginCatalogReader`). Avoid vague suffixes such as `Base` as well.
- Avoid static helper classes and methods unless there is a clear necessity; treat them as a design smell and prefer behavior owned by an appropriate type.
- Prefix interface names with `I`.
- Use primary constructors and file-scoped namespaces.
- Do *not* apply 'sealed', unless extending the type is a design concern. Use 'sealed' only when the type is not intended to be extended.

## Unit-of-work example

```csharp
// Wrong: operation inputs are re-fed on every step.
foreach (var step in steps)
    work.Apply(config, template, step);

// Right: bind the operation once; only the step varies.
var work = new Work(config, template);
foreach (var step in steps)
    work.Apply(step);
```

## Naming example

```csharp
// Prefer a purpose-specific name.
public sealed class PluginCatalogReader
{
}

// Avoid vague names.
public sealed class PluginService
{
}

public abstract class PluginBase
{
}
```