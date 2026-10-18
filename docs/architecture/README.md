# Architecture

This repository owns only the independent extension SDK. It builds one netstandard2.1 assembly with an explicit FSharp.Core dependency. No umbrella solution or build/release properties are shared with the FScript interpreter repository.

Types.fs defines declarative type contracts; Values.fs defines boundary values, descriptors, contexts, errors, and opaque handle interfaces. HostInterop validates descriptors and snapshots. HostTypes implements attributed CLR projection; HostFunctions derives closed schemes and invocation adapters from typed synchronous handlers. Helpers produce the same public records an extension can author manually.

The interpreter repository owns the Runtime adapter. It recursively translates data and descriptors, associates handles with native evaluation sessions, adapts standard host functions, and restores callback error locations. The Language assembly never references the SDK. This ownership separates stable provider binaries from product assembly versions.

PR/main CI builds, tests, checks the public API and changelog, and packs. Tag CI verifies versioned changelog notes and creates the draft with NuGet/symbol artifacts. Publishing that draft triggers NuGet publication. The first release compares the bootstrap commit with tag 1.0.0; later releases compare successive tags. No native packaging, signing, npm, VSIX, or website workflow belongs here.
