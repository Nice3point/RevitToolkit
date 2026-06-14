# Documentation Requirements

Documentation ships with the package. Every change to the public surface updates the README, the Changelog, and the XML docs in the same commit.

## README.md

* **Usage examples:** every new feature must have a usage example under the matching section (`External Commands`, `External Applications`, `External events`, `Context`, `Options`, `Decorators`, `Helpers`).
* **Add to the existing section.** New overloads or related members belong with their primary feature — do not create a new top-level section for a variant.
* **Code blocks:** use proper C# syntax highlighting and keep examples copy-pasteable.

## CHANGELOG.md

* Update the current preview/release version section.
* Categorize every change:
    * **New Features:** new classes, methods, overloads, generated APIs, analyzers.
    * **Breaking Changes:** renamed members, changed behavior.
    * **Improvements:** performance, refactoring.
    * **Bug Fixes:** corrections to existing functionality.
* Provide migration examples at the end of the section for any breaking change or deprecation.
* **Document all changes,** not only major ones.

## XML Documentation

* **Summary:** describe what the member does. For Revit API wrappers, mirror the summary from the Revit API documentation.
* **Parameters / Returns:** document each `<param>` with context and the `<returns>` value's meaning.
* **Remarks:** add implementation details, constraints, and thread-safety notes.
* **Example:** provide a usage `<example>` for complex APIs (the dialog-suppression scopes and the `[ExternalEvent]` attribute are good models).
* **Exceptions:** document every Revit API `<exception>` a member can throw.

See [Code Style](./code-style.md) for the in-code XML doc conventions and [Backward Compatibility](./backward-compatibility.md) for deprecation messaging.
