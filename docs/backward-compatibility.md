# Backward Compatibility

This is a public library with downstream consumers. A breaking change breaks other people's builds. **Never delete or change an existing public API.** Deprecate it instead.

## Obsolete Pattern

To rename or replace a member, mark the old one `[Obsolete]` and keep it functional. Provide a JetBrains `CodeTemplate` so ReSharper/Rider can auto-convert call sites.

```csharp
[Obsolete("Use Application instead")]
[CodeTemplate(
    searchTemplate: "UiApplication",
    Message = "UiApplication is obsolete, use Application instead",
    ReplaceTemplate = "Application",
    ReplaceMessage = "Replace with Application")]
public UIApplication UiApplication => Application;
```

For a class-level deprecation, mark the whole type `[Obsolete]` (e.g. `ActionEventHandler` is obsolete in favor of `ExternalEvent` and the `[ExternalEvent]` source generator).

## Obsolete Guidelines

* **Message:** a clear explanation that names the replacement member.
* **CodeTemplate:** provide the ReSharper auto-conversion pattern (`searchTemplate` → `ReplaceTemplate`) so call sites can be migrated automatically.
* **Implementation:** the obsolete member must keep calling the **original implementation** (or forward to the property/field it became), not throw and not silently change behavior. It stays working independently.
* **Class replacement:** create the new class, mark the old one `[Obsolete]`, and keep the old one functional.

## Breaking Changes

* **Method signatures:** never change an existing signature. Add a new overload instead.
* **Return types:** never change a return type.
* **Parameters:** add new parameters only as optional, and only at the end of the list.
* **Renaming:** use the Obsolete pattern above; keep the old member functional indefinitely.
* **Analyzer/generator output:** treat the generated API shape and diagnostic ids (`RVTTK####`) as public surface — renaming a generated property or reusing a retired diagnostic id is a breaking change. See [Analyzers & Source Generators](./analyzers-and-generators.md).

Document every change — additions, deprecations, and behavior changes — in [the Changelog](./documentation.md).
