; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 2027.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
RVTTK0001 | ExternalEventGenerator | Error | Method returns Task or Task<T>
RVTTK0002 | ExternalEventGenerator | Warning | Method is async void
RVTTK0003 | ExternalEventGenerator | Error | Method is generic
RVTTK0004 | ExternalEventGenerator | Error | Duplicate method overloads
RVTTK0005 | ExternalEventGenerator | Error | Containing type is not partial
