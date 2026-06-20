# Documentation

These rules govern every piece of prose the package ships: XML doc comments, `README.md`, and `CHANGELOG.md`. Each format adds its own rules on top of the shared set.

A public-surface change updates the README, the CHANGELOG, and the affected XML docs in the same commit. Documentation that lags the code is a defect.

## Shared Prose Rules

* **State what, not how.** Describe observable behavior and contract, never the implementation. A summary survives an implementation rewrite unchanged.
* **Plain technical English.** No corporate jargon, no marketing tone.
* **No filler.** Omit obvious statements. State only what a reader cannot infer from the signature.
* **Third-person present indicative.** Write "Suppresses the dialog", not "Suppressing the dialog". No `-ing` verb form for what a member does.
* **One sentence per line.** Break at sentence boundaries, never at a fixed character width.
* **No dashes or semicolons.** Use separate sentences or commas.

## XML Doc Comments

* Document every public member with a `<summary>` that states what it does.
* **`<summary>` describes the member, not its parameters.** Parameters belong in `<param>`, the return value in `<returns>`, and thrown exceptions in `<exception>`. Do not restate the signature in prose.
* For a wrapper over the Revit API, mirror the corresponding Revit API summary and document the Revit `<exception>`s the member can throw.
* Add `<remarks>` for a non-trivial constraint or a thread-safety note. Add `<example>` for a non-trivial API, as the dialog-suppression scopes and the `[ExternalEvent]` attribute show.
* Reference another type or member with `<see cref="..."/>` so renames stay tracked.

## README

Every new feature has a usage example under its matching section (`External Commands`, `External Applications`, `External events`, `Context`, `Options`, `Decorators`, `Helpers`). A new overload or related member belongs with its primary feature, not a new top-level section. Keep examples copy-pasteable with C# syntax highlighting.

## CHANGELOG

Update the current preview or release version section. Categorize every change, not only the major ones:

* **New Features:** new classes, methods, overloads, generated APIs, analyzers.
* **Breaking Changes:** renamed members, changed behavior.
* **Improvements:** performance, refactoring.
* **Bug Fixes:** corrections to existing functionality.

Provide a migration example at the end of the section for any breaking change or deprecation. See [Backward Compatibility](./backward-compatibility.md) for the deprecation pattern.
