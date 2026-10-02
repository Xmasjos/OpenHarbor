---
applyTo: "**"
---

# Generic Implementation Rules

- Apply SOLID pragmatically; add abstractions only when they clarify responsibility, protect contracts, or isolate change.
- Put shared behavior on an appropriate parent (abstract if useful); inherit only where types share a meaningful contract.
- Read environment variables, files, and external configuration only at the application configuration boundary. Bind with the options pattern and inject typed options where needed.
- Do not write unit tests during implementation; add them at the end of the implementation stage.
- Unit tests should usually target interfaces and consumer-visible behavior, not implementation details or helper methods. Test helpers indirectly through their consumption points.

## SOLID in this codebase

- **Single Responsibility:** One cohesive responsibility and reason to change. Separate catalog queries, catalog mutations, path/route normalization, plugin activation, and endpoint/public-asset mapping when they have distinct policies or callers. Do not put `Get*`, `Create*`, `Update*`, `Delete*`, and route/endpoint mapping in one catch-all component. Related mutations may share a writer; do not create one class per CRUD verb without a distinct reason.
- **Open/Closed:** Extend expected variation behind a contract instead of adding conditionals to stable callers; add an `IPluginActivator` implementation for a new activation strategy.
- **Liskov Substitution:** Implementations honor their contract without caller type checks. If an activator cannot handle a plugin kind, state that capability explicitly or narrow the contract.
- **Interface Segregation:** Every application behavior component consumed by another component must expose a consumer-focused interface, and consumers must depend on that interface rather than its concrete implementation. Split interfaces by capability (for example, `IPluginCatalogReader`, `IPluginCatalogWriter`, and `IPluginRouteMapper`); do not use a catch-all interface or omit the interface because there is currently only one implementation. Do not add interfaces to data types or private implementation details solely to mirror their class names.
- **Dependency Inversion:** Workflows depend on purpose-specific interfaces; choose infrastructure at composition. Inject an assembly-loading abstraction into `PluginActivator` rather than loading files directly there.

Before implementing an application behavior component, identify its callers, cohesive responsibility, interface, and concrete type name. Before considering the work complete, check that query, mutation, and mapping responsibilities have not been combined; callers use the intended interfaces; and no class name ends in `Service`.

## Examples

Bind configuration at the boundary and inject typed options:

```csharp
builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection(StorageOptions.SectionName));

public class CatalogReader(IOptions<StorageOptions> options)
{
    private readonly StorageOptions _options = options.Value;
}
```

Test observable behavior through the interface:

```csharp
ICatalogReader reader = catalogReader;
var result = await reader.ReadAsync(key, cancellationToken);
Assert.Equal(expected, result);

// Do not test private parsing/helper methods directly.
```