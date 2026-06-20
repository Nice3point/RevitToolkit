# Contributing to Nice3point.Revit.Toolkit

Thanks for taking the time to contribute. This guide covers issues and pull requests. For the architecture and conventions, see the project guidelines in [AGENTS.md](AGENTS.md) and the [docs](docs/) folder.

## Issues

* Search the existing issues and discussions before you open a new one.
* For a bug, describe what you expected, what happened, and the smallest steps that reproduce it. Include the Revit version and the package version. For an analyzer or generator report, include the diagnostic id.
* For a feature, describe the problem it solves, not only the solution you have in mind.
* For a large or breaking change, open an issue first so the approach is agreed before you write code.

## Pull Requests

* Keep each pull request focused on one concern. Split unrelated changes into separate pull requests.
* Fork the repository, branch from the default branch, and open a draft pull request early.
* Match the style and patterns of the surrounding code.
* Add or update tests for the behavior you change.
* Verify the change on every Revit version it affects when the Revit API differs between versions.
* Never break an existing public API. Deprecate it instead. The generated API shape and the diagnostic ids are public surface too.
* Update the README, the CHANGELOG, and the XML docs in the same pull request as any public-facing change.
* Write a clear title and description, and link the issue the pull request resolves.
* Make sure the build and the tests pass before you mark the pull request ready for review.

## Development

Run `dotnet run` from the `build` directory to compile, and `dotnet test` to run the tests. The SDK version is pinned in `global.json`.

Please keep issues and pull requests respectful and on topic.
