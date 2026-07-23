---
name: revit-toolkit-backward-compat
description: >
  Evolve the public surface of Nice3point.Revit.Toolkit without breaking downstream consumers: deprecate a renamed or replaced member with [Obsolete] with a JetBrains [CodeTemplate] auto-conversion, and treat both the generated API shape and the RVTTK#### diagnostic ids as stable public surface.
  USE FOR: renaming, replacing, or removing any public type or member; adding an overload or parameter to a shipped API; changing a generated member name or a diagnostic id; recording the deprecation in the changelog.
  DO NOT USE FOR: private implemetations and internal types.
license: MIT
---

# Revit Toolkit Backward Compatibility

Nice3point.Revit.Toolkit is a public NuGet library with downstream consumers; a breaking change breaks other people's builds.
Never delete or change an existing public API; deprecate it and keep it working.
Public surface is broader than the hand-written types: the shape the source generator emits and the `RVTTK####` diagnostic ids the analyzers report are contracts too.

## When to use

- Renaming, replacing, or retiring a public type or member.
- Adding an overload or a parameter to an already shipped API.
- Changing a generated member name or introducing or renumbering a diagnostic id.
- Recording a deprecation or breaking change in the changelog.

## Workflow

### Step 1: Add, deprecate, but never change or delete

The only safe evolutions are additive or a deprecation.

- **Method signatures:** never change an existing signature; add a new overload.
- **Return types:** never change a return type.
- **Parameters:** add a new parameter only as optional, and only at the end of the list.
- **Renaming or removal:** use the deprecation pattern below and keep the old member functional indefinitely.

### Step 2: Deprecate a member with [Obsolete] and [CodeTemplate]

To rename or replace a member, mark the old one `[Obsolete]` with a message that names the replacement, and add a JetBrains `[CodeTemplate]`; ReSharper and Rider auto-convert call sites.
The obsolete member forwards to its replacement and stays functional.

```csharp
[Obsolete("Use Application instead")]
[CodeTemplate(
    searchTemplate: "UiApplication",
    Message = "UiApplication is obsolete, use Application instead",
    ReplaceTemplate = "Application",
    ReplaceMessage = "Replace with Application")]
public UIApplication UiApplication => Application;
```

`searchTemplate` is the pattern ReSharper matches at a call site, and `ReplaceTemplate` is what it substitutes; `Message` shows on the obsolete member and `ReplaceMessage` on the quick-fix.

### Step 3: Match the CodeTemplate placeholder to the member shape

Use a `[CodeTemplate]` placeholder; the auto-conversion carries the call site's arguments and type arguments across.

- Method arguments use `$args$` in both templates:

```csharp
[Obsolete("Use RevitApiContext.BeginFailureSuppressionScope instead")]
[CodeTemplate(
    searchTemplate: "Context.SuppressFailures($args$)",
    Message = "Context.SuppressFailures is obsolete, use RevitApiContext.BeginFailureSuppressionScope with 'using' statement instead",
    ReplaceTemplate = "RevitApiContext.BeginFailureSuppressionScope($args$)",
    ReplaceMessage = "Replace with RevitApiContext.BeginFailureSuppressionScope")]
public static void SuppressFailures(bool resolveErrors = true)
{
    // original implementation kept intact
}
```

- Generic type arguments use a named placeholder such as `$T$`:

```csharp
[Obsolete("Use BeginAssemblyResolveScope<T> instead for automatic resource management")]
[CodeTemplate(
    searchTemplate: "BeginAssemblyResolve<$T$>()",
    Message = "BeginAssemblyResolve is obsolete. Use BeginAssemblyResolveScope with 'using' statement instead",
    ReplaceTemplate = "BeginAssemblyResolveScope<$T$>()",
    ReplaceMessage = "Replace with BeginAssemblyResolveScope")]
public static void BeginAssemblyResolve<T>()
{
    BeginAssemblyResolveScope(typeof(T));
}
```

Omit `[CodeTemplate]` only when there is no mechanical call-site rewrite, such as retiring a stateful method that a scope now replaces (`RestoreDialogs`), and still keep the member functional.

### Step 4: Keep the obsolete member functional

An obsolete member keeps calling the original implementation, or forwards to the property, field, or method it became.
It must not throw and must not silently change behavior; a consumer who has not migrated keeps building and running.
A simple rename forwards directly (`UiApplication => Application`); a replaced method forwards to the new API (`BeginAssemblyResolve<T>` calls `BeginAssemblyResolveScope`).

### Step 5: Deprecate a whole class the same way

To replace a class, create the new type, mark the old one `[Obsolete]`, and keep it functional.
Keep `[PublicAPI]` on the deprecated class; its contract stays documented.

```csharp
[PublicAPI]
[Obsolete("Use Nice3point.Revit.Toolkit.External.ExternalEvent class or ExternalEvent source-generator instead")]
public class ActionEventHandler : ExternalEventHandler
{
    // full implementation stays working
}
```

The static `Context` class shows the same pattern at scale: the type is `[Obsolete]` and every member carries its own `[Obsolete]` and `[CodeTemplate]` forwarding to `RevitContext`/`RevitApiContext`.

### Step 6: Treat generated API and RVTTK ids as public surface

The `[ExternalEvent]` generator's output and the analyzers' diagnostic ids are as much a contract as a hand-written method.

- Never rename a generated property or record; renaming one is a breaking change for every consumer that references the generated name.
- Never renumber or reuse a shipped `RVTTK####` id; assign the next unused id for a new rule.
- Record every new or changed rule in `AnalyzerReleases.Unshipped.md`; the shipped ids stay in `AnalyzerReleases.Shipped.md`.

### Step 7: Document, then verify

Record every addition, deprecation, and behavior change in the current section of `CHANGELOG.md`, and add a migration example at the end of the section for any breaking change or deprecation.
Update the README and XML docs in the same change.
Then confirm the obsolete member still compiles and its callers still build.

```shell
dotnet run -c Release
```

## Validation

- [ ] No existing public signature, return type, or member name changed; new parameters are optional and appended.
- [ ] A renamed or replaced member carries `[Obsolete]` naming the replacement and a `[CodeTemplate]` whose placeholder matches the member shape (`$args$`, `$T$`).
- [ ] The obsolete member forwards to its replacement, stays functional, and neither throws nor changes behavior.
- [ ] A replaced class is `[Obsolete]` and still functional; the new class exists alongside it.
- [ ] No generated member name was renamed and no `RVTTK####` id was renumbered or reused; a new rule is recorded in `AnalyzerReleases.Unshipped.md`.
- [ ] The changelog, README, and XML docs record the change, with a migration example for any deprecation.

## Common Pitfalls

| Pitfall                                                  | Correct approach                                                       |
|----------------------------------------------------------|------------------------------------------------------------------------|
| Changing a method signature or return type in place      | Add a new overload; leave the shipped one untouched.                   |
| Deleting or renaming a public member outright            | Deprecate with `[Obsolete]` + `[CodeTemplate]` and keep it functional. |
| An obsolete member that throws or changes behavior       | Forward to the replacement; unmigrated callers keep working.           |
| Adding a required parameter to a shipped method          | Add it as optional and only at the end, or add an overload.            |
| Renaming a generated property or reusing a diagnostic id | Treat generated names and `RVTTK####` ids as public surface.           |
| Deprecating without a changelog migration note           | Record the change and add a migration example for the deprecation.     |
