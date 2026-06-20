# Project Structure

Nice3point.Revit.Toolkit removes the boilerplate of building Revit add-ins. It ships a runtime library plus the Roslyn tooling that automates and guards the add-in patterns. The solution separates the runtime library, the analyzer and generator projects, their Roslyn-version twins, and the tests. Keep each piece of code in the project that owns its responsibility.

## Solution Groups

* **`/source`**: the shipped NuGet package and its bundled tooling.
    * The runtime library is everything a consumer references at runtime: base classes for add-in entry points, the static Revit contexts, the external-event types, option callbacks, decorators, and helpers. It is the only package-producing project.
    * The analyzer project holds the diagnostic analyzers that enforce the tooling's prerequisites and guard against API misuse.
    * The code-fixer project holds the fixers that resolve those diagnostics.
    * The source-generator project holds the incremental generators that emit add-in boilerplate.
    * The `*.Roslyn###` twins compile the same linked source against an older Roslyn so the package works on older IDEs and SDKs. See [Analyzers & Source Generators](./analyzers-and-generators.md).
* **`/tests`**: the verification projects. Revit behavior runs inside a Revit process, the Roslyn tooling runs against the compiler.
* **`/build`**: the ModularPipelines build that compiles, tests, packages, and publishes.
* **Root**: build and package configuration, the README and CHANGELOG, the agent guidelines, and the CI workflows.

## Change Placement

* A base class for an add-in entry point goes with the other entry-point base classes under `External`.
* An external-event type goes with the event family under `External/Events`, its interface alongside the other event interfaces.
* A Revit API option or callback implementation goes under `Options`.
* Reflection-based or otherwise unsafe internals go under `Internal` and stay non-public.
* An analyzer, fixer, or generator goes in its matching tooling project. The `.Roslyn###` twins pick the files up automatically through linked compilation.
* Revit-thread coverage goes in the Revit test project, Roslyn tooling coverage in the matching tooling test project.
