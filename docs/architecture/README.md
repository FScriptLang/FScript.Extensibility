# Architecture

This repository owns only the independent extension SDK. It builds one netstandard2.1 assembly with an explicit FSharp.Core dependency. No umbrella solution or build/release properties are shared with the FScript interpreter repository.

Types.fs defines declarative type contracts; Values.fs defines boundary values, descriptors, contexts, errors, and opaque handle interfaces. HostInterop validates descriptors and snapshots. HostTypes implements attributed CLR projection; HostFunctions derives closed schemes and invocation adapters from typed synchronous handlers. Helpers produce the same public records an extension can author manually.

The interpreter repository owns the Runtime adapter. It recursively translates data and descriptors, associates handles with native evaluation sessions, adapts standard host functions, and restores callback error locations. The Language assembly never references the SDK. This ownership separates stable provider binaries from product assembly versions.

PR/main CI builds, tests, checks the public API and changelog, and packs. Tag CI verifies versioned changelog notes and creates the draft with NuGet/symbol artifacts. Publishing that draft triggers the NuGet publish job. The job exchanges a GitHub Actions OIDC token for a short-lived key before pushing the release assets. The first release compares the bootstrap commit with tag 1.0.0; later releases compare successive tags. No native packaging, signing, npm, VSIX, or website workflow belongs here.

SDK 3 retains generic opaque metadata and exposes cancellable async I/O factories through ExposeAsync. Scripts own task spawning and awaiting. Runtime-private services own their internal workers; the SDK has no scheduling or waiting methods.

## Toolchain and repository ownership

The repository is hosted at [FScriptLang/FScript.Extensibility](https://github.com/FScriptLang/FScript.Extensibility), under the Magnus Opera umbrella. Package identity remains `MagnusOpera.FScript.Extensibility`. New repository metadata and comparison links use FScriptLang; archived release links remain unchanged.

Development and CI use the .NET 11 SDK pinned in `global.json`. All workflows read that file, and tests target `net11.0`. The published library retains its `netstandard2.1` target, explicit FSharp.Core dependency, and unsigned SDK 3.x assembly identity. API verification and package validation continue to protect consumer compatibility.

PR validation uses a read-only GitHub token, no stored credentials, no publishing environment, and checkouts with credential persistence disabled. NuGet publication uses the `release` environment, whose GitHub deployment rules allow only numeric release tags. The NuGet Trusted Publishing policy must require that environment as well as `on-release-published.yml` and the repository identity. Do not grant organization secrets to this repository; a modified PR workflow could read them without entering an environment.

Async factories are invoked inline, without worker scheduling or blocking waits.
The host tracks their results, cancellation, and lifetime using opaque script-task
handles. The three-argument context constructor replaces the SDK 2 constructors.
