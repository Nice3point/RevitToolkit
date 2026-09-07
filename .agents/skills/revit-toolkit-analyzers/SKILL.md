---
name: revit-toolkit-analyzers
description: >
  Author and extend the Roslyn tooling bundled in the Nice3point.Revit.Toolkit package: the incremental source generator that emits external-event boilerplate, the analyzers and code fixers that enforce its prerequisites, the RVTTK#### diagnostic catalog, and the dual-Roslyn packaging.
  USE FOR: adding or changing a generator, analyzer, code fixer, or diagnostic in the Analyzers, CodeFixers, or SourceGenerators projects; assigning a diagnostic id; wiring the .Roslyn### twin projects and Directory.Roslyn.props; packaging the tooling into the analyzers/dotnet/roslyn paths; recording a rule in AnalyzerReleases.
  DO NOT USE FOR: designing the runtime library types the generator emits against, such as the external-event classes and static contexts.
license: MIT
---

# Revit Toolkit Analyzers

Nice3point.Revit.Toolkit ships Roslyn tooling inside the NuGet package: an incremental source generator that turns an `[ExternalEvent]`-annotated method into event properties, analyzers that enforce the annotation's prerequisites, and code fixers that resolve the diagnostics.
The generated API shape and the `RVTTK####` diagnostic ids are public surface; treat every id and generated name as a contract.
This skill covers authoring and extending that tooling; the generator and analyzers require `Microsoft.CodeAnalysis.CSharp`.

## When to use

- Adding or changing the `ExternalEventGenerator` entry point or its `ExternalEvents/Analysis` and `ExternalEvents/Emission` implementation.
- Adding a diagnostic to the `ExternalEventDiagnostics` catalog or a new analyzer or code fixer.
- Wiring a `.Roslyn###` twin project or editing `Directory.Roslyn.props`.
- Changing how the tooling assemblies pack into the `analyzers/dotnet/roslyn{4.14,5.0}` paths.

## When not to use

- Designing the runtime types the generator emits against (the external-event family, the static contexts). That is the library design contract, not the compiler tooling.

## Architecture layout

Public generator, analyzer, and code-fix entry points live at their project roots.
Their public namespaces remain stable.
Capability-specific implementation lives under the same subject name across projects, such as `ExternalEvents`.
The capability root holds its shared definitions; `Analysis` extracts and validates symbols, and `Emission` renders source.
The external-event diagnostic catalog lives in `Analyzers/ExternalEvents/ExternalEventDiagnostics.cs` and is linked into the other tooling projects.
Descriptor declarations follow diagnostic-ID order.

`CSharp` holds language-specific symbol and source operations.
`Diagnostics` holds generator diagnostic reporting, and `IncrementalGeneration` holds value equality used by incremental pipelines.
These subjects have no dependency on `ExternalEvents`.
The public `EquatableArray<T>` retains its existing `SourceGenerators.Models` namespace for compatibility; its file belongs to `IncrementalGeneration`.
Folders describe subjects rather than class forms; `Helpers`, `Extensions`, and `Models` are not storage categories.
New capabilities receive sibling subject folders instead of expanding the external-event implementation or a project-wide helper catalog.

## Workflow

### Step 1: Emit from the incremental generator

`ExternalEventGenerator` is `[Generator(LanguageNames.CSharp)]` and implements `IIncrementalGenerator`.
It pipes matched methods with `ForAttributeWithMetadataName`, reports diagnostics, then registers source output.
Keep the pipeline incremental: extract into an equatable model in `ExternalEventExtractor`, and render text in `ExternalEventWriter`.

```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
    var methodAnalyses = context.SyntaxProvider
        .ForAttributeWithMetadataName(
            ExternalEventTypeNames.ExternalEventAttribute.WithoutGlobalPrefix,
            predicate: static (node, cancellationToken) => node is MethodDeclarationSyntax,
            transform: static (syntaxContext, cancellationToken) => ExternalEventExtractor.AnalyzeMethod(syntaxContext, cancellationToken));

    context.ReportDiagnostics(methodAnalyses.Select(static (methodAnalysis, cancellationToken) => methodAnalysis.Diagnostics));

    var eventDefinitionsWithOptions = methodAnalyses
        .Where(static methodAnalysis => methodAnalysis.Definition is not null)
        .Select(static (methodAnalysis, cancellationToken) => methodAnalysis.Definition!)
        .Combine(context.ParseOptionsProvider);

    context.RegisterSourceOutput(eventDefinitionsWithOptions, static (sourceProductionContext, generationInput) =>
    {
        var (eventDefinition, _) = generationInput;
        var generatedSource = ExternalEventWriter.GenerateSource(eventDefinition, useFieldKeyword: false, useLockType: false);
        sourceProductionContext.AddSource(eventDefinition.HintName, SourceText.From(generatedSource, Encoding.UTF8));
    });
}
```

Emit into the method's own namespace and a stable hint name derived from the type hierarchy, and treat every generated member name as public surface.
Never carry a `SyntaxNode`, `ISymbol`, or `Compilation` into the model; carry only equatable data. The pipeline then caches correctly.
Project consumer capabilities into equatable values before combining them with event definitions.
The `field` keyword depends on the consumer language version; `System.Threading.Lock` requires C# 13 or later and an accessible type in the consumer compilation.
Generated multi-parameter handlers use block-bodied lambdas, with a `return` statement for value-returning handlers.

### Step 2: Register the diagnostic in the RVTTK catalog

Diagnostics live in the internal `ExternalEventDiagnostics` catalog under the `ExternalEventGenerator` category, keyed by a `RVTTK####` id.
The shipped ids are `RVTTK0001` (returns `Task`), `RVTTK0002` (`async void`), `RVTTK0003` (generic), `RVTTK0004` (duplicate overloads), and `RVTTK0005` (containing type not partial).
Assign the next unused id, never reuse a retired one, and give it a title, a parameterized `messageFormat`, a category, and a severity.

```csharp
public static readonly DiagnosticDescriptor ContainingTypeNotPartial = new(
    id: "RVTTK0005",
    title: "Containing type is not partial",
    messageFormat: "The type '{0}' containing method '{1}' marked with [ExternalEvent] must be declared as partial",
    category: "ExternalEventGenerator",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true);
```

### Step 3: Report from the generator or a dedicated analyzer

Choose the reporter by whether the check needs the full symbol model at edit time.
The generator's `ExternalEventExtractor.ValidateMethod` reports `RVTTK0001`, `RVTTK0003`, and `RVTTK0004` while building the model, returning `false` to skip emission.
`RVTTK0002` and `RVTTK0005` also have standalone analyzers (`AsyncVoidMethodAnalyzer`, `ContainingTypeNotPartialAnalyzer`); the IDE flags them live and a code fixer can attach.

An analyzer resolves the attribute symbol once per compilation, then registers a symbol action.

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncVoidMethodAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [ExternalEventDiagnostics.AsyncVoidMethod];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            var attributeSymbol = context.Compilation.GetTypeByMetadataName("Nice3point.Revit.Toolkit.External.ExternalEventAttribute");
            if (attributeSymbol is null)
            {
                return;
            }

            context.RegisterSymbolAction(context =>
            {
                if (context.Symbol is not IMethodSymbol { IsAsync: true, ReturnsVoid: true } methodSymbol)
                {
                    return;
                }

                if (!methodSymbol.HasAttribute(attributeSymbol))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: ExternalEventDiagnostics.AsyncVoidMethod,
                    location: methodSymbol.Locations[0],
                    messageArgs: methodSymbol.Name));
            }, SymbolKind.Method);
        });
    }
}
```

Always call `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)` and `EnableConcurrentExecution()`, and bail out early when the attribute type is absent from the compilation.

### Step 4: Add a code fixer when the fix is mechanical

Add a fixer only when the violation resolves without a human decision.
A fixer is `[Shared]` and `[ExportCodeFixProvider(LanguageNames.CSharp)]`, lists the fixable id from the catalog, and returns `WellKnownFixAllProviders.BatchFixer`.

```csharp
[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class MakeTypePartialCodeFixer : CodeFixProvider
{
    private const string Title = "Make type partial";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [ExternalEventDiagnostics.ContainingTypeNotPartial.Id];

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var typeDeclaration = root!.FindNode(context.Span).FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (typeDeclaration is null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: cancellationToken => AddPartialModifier(context.Document, root, typeDeclaration, cancellationToken),
                equivalenceKey: Title),
            diagnostic);
    }
}
```

Reference the fixable id through the catalog (`ExternalEventDiagnostics.X.Id`), never a string literal; an id can never drift between analyzer and fixer.

### Step 5: Compile the same source against both Roslyn versions

Every tooling project has a `.Roslyn###` twin that compiles the same source against an older Roslyn; the package works on older IDEs and SDKs.
`Directory.Roslyn.props` parses the suffix, links the base project's `.cs` files into the twin, and sets `ROSLYN*_OR_GREATER` constants.
Author the code once in the base project; the twin's csproj is empty except for the props import and a version-pinned `Microsoft.CodeAnalysis.CSharp`.

```xml
<!-- Twin project: Nice3point.Revit.Toolkit.Analyzers.Roslyn414.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
    <Import Project="..\..\Directory.Roslyn.props"/>
    <ItemGroup>
        <PackageReference Include="Microsoft.CodeAnalysis.CSharp" VersionOverride="$(RoslynVersion).*" PrivateAssets="all"/>
    </ItemGroup>
</Project>
```

Guard any API that differs across Roslyn versions with the `ROSLYN5_0_0_OR_GREATER` / `ROSLYN4_14_0_OR_GREATER` constants, as `ExternalEventGenerator` does when it decides whether the `field` keyword is available.
Never copy source into a twin; `Directory.Roslyn.props` links it automatically via `<Compile Include="..\$(_BaseProjectName)\**\*.cs" .../>`.

### Step 6: Package into the version-specific analyzer paths

The runtime library project is the only package-producing project.
It packs each tooling assembly into the matching Roslyn path; the right build loads per IDE and SDK. It references the tooling projects with `ReferenceOutputAssembly="false"` purely for build order.

```xml
<None Include="..\Nice3point.Revit.Toolkit.SourceGenerators\bin\$(Configuration)\netstandard2.0\Nice3point.Revit.Toolkit.SourceGenerators.dll" PackagePath="analyzers\dotnet\roslyn5.0\cs" Pack="true" Visible="false"/>
<None Include="..\Nice3point.Revit.Toolkit.SourceGenerators.Roslyn414\bin\$(Configuration)\netstandard2.0\Nice3point.Revit.Toolkit.SourceGenerators.dll" PackagePath="analyzers\dotnet\roslyn4.14\cs" Pack="true" Visible="false"/>
```

All tooling projects target `netstandard2.0` with `IsRoslynComponent=true` and `EnforceExtendedAnalyzerRules=true`, set in `Directory.Roslyn.props`.

### Step 7: Record the rule, then test

Add every new or changed rule to `AnalyzerReleases.Unshipped.md` in the analyzer project; the release-tracking analyzer fails the build otherwise.
The shipped ids are already in `AnalyzerReleases.Shipped.md`.

```text
### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
RVTTK0006 | ExternalEventGenerator | Warning | <the new rule>
```

Then cover the change in the tooling test projects and build.
Analyzer and fixer tests assert both the reported `RVTTK####` and the fixed source; generator tests drive the generator and verify the emitted source and diagnostics.

```shell
dotnet run -c Release
```

## Validation

- [ ] The generator stays incremental: only equatable data crosses into the model, and output goes to a stable hint name and namespace.
- [ ] A new diagnostic uses the next unused `RVTTK####` id in `ExternalEventDiagnostics`, with a title, parameterized message, category, and severity.
- [ ] Every analyzer configures generated-code analysis and concurrent execution and bails when the attribute type is absent.
- [ ] A code fixer is added only for a mechanical fix, is `[Shared]`/`[ExportCodeFixProvider]`, and references the fixable id through the catalog.
- [ ] Roslyn-version-specific APIs are guarded with `ROSLYN*_OR_GREATER`; no source is duplicated into a `.Roslyn###` twin.
- [ ] Each tooling assembly packs into both `analyzers/dotnet/roslyn5.0/cs` and `analyzers/dotnet/roslyn4.14/cs`.
- [ ] Every new or changed rule has an `AnalyzerReleases.Unshipped.md` entry and a test in the matching tooling test project.

## Common Pitfalls

| Pitfall                                                    | Correct approach                                                         |
|------------------------------------------------------------|--------------------------------------------------------------------------|
| Reusing or renumbering a retired `RVTTK####` id            | Assign the next unused id; shipped ids are public surface.               |
| Renaming a generated property or namespace                 | Generated names are public surface; deprecate, do not rename.            |
| Capturing a symbol or `Compilation` in the generator model | Carry only equatable data; the incremental pipeline then caches.         |
| Duplicating source into a `.Roslyn###` twin                | Author once in the base project; the props file links the files.         |
| A hard-coded diagnostic string in a code fixer             | Reference `ExternalEventDiagnostics.X.Id`; analyzer and fixer stay in sync. |
| Skipping the `AnalyzerReleases.Unshipped.md` entry         | Add every rule; release tracking fails the build without it.             |
| Adding a code fixer for a fix that needs a human decision  | Report the diagnostic only; fix mechanically resolvable cases.           |
