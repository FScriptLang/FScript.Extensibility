# Stable extension contract

The normative public API is `api/PublicAPI.txt`; descriptor fields and mappings are documented in the [README](../../README.md). The library must not reference interpreter assemblies or expose AST, closure environments, inference witnesses, runtime state, or task workers.

Providers use SDK-owned registrations, values, schemes, attributes, and contexts. Host implementations validate registrations before exposing functions. Schemes are authoritative at invocation boundaries. Opaque values preserve metadata, visible snapshots, and payloads; constructible records carry visible data only. Both record construction and updates remain forbidden for opaque types.

Reflection helpers reject unsupported/recursive projections, hidden constructible fields, and absent/mismatched referenced descriptors before invoking handlers. Argument conversion completes before handler execution. Handler and conversion failures use ExtensionException and retain inner causes. Manual implementations own their conversions.

Callable/task handles are host-created and session-bound. Invocation contexts expire after the handler returns. Hosts reject invalid handle implementations, cross-session use, and use after evaluation expires. Hosts enforce runtime limits and preserve callback source errors independently of SDK exceptions.

SDK 2 adds named type applications and type parameter lists on host descriptors. A generic descriptor must be opaque and expose no visible fields. Runtime validation checks the registered descriptor and type arity. The script type checker enforces the type arguments across uses.

`ExternalCallContext.SpawnWorker` starts provider work in the interpreter’s tracked task scope and supplies a fresh context to the worker. A worker can invoke callable handles captured in the same evaluation scope. The host still owns callback permissions, failure reporting, and task lifetime.

Compatible 2.x releases retain unsigned AssemblyVersion 2.0.0.0 and public record/union shapes. API snapshot validation is required for every change. Later compatible releases use 2.0.0 as the package validation baseline. FSharp.Core remains an explicit package dependency.
