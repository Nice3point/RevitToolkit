## Fork, Clone, Branch and Create your PR

1. Fork the repo if you haven't already
2. Clone your fork locally
3. Create & push a feature branch
4. Create a [Draft Pull Request (PR)](https://github.blog/2019-02-14-introducing-draft-pull-requests/)
5. Work on your changes

## Rules

- Follow the pattern of what you already see in the code.
- When adding new classes/methods/changing existing code: check the functionality of new extensions on all versions of Revit if the API has changed.

## Naming of features and functionality

The naming should be descriptive and direct, giving a clear idea of the functionality and usefulness in the future.

## Prerequisites for Compiling RevitExtensions

- .Net 7 SDK or newer
- Visual Studio 2022 / JetBrains Rider 2023.3 or newer

## Life cycle

Revit version support - 5 years.

Package version format:

RevitVersion.MajorVersion.BuildNumber

- The first field is the Revit version the library was compiled for.
- The second field is promoted after a new version of Revit is released.
- The third field is promoted when new extensions are released before publishing to NuGet.