# Stable extension contract

The normative public API is `api/PublicAPI.txt`; descriptor fields and mappings are documented in the [README](../../README.md). The library must not reference interpreter assemblies or expose AST, closure environments, inference witnesses, runtime state, or task workers.

Providers use SDK-owned registrations, values, schemes, attributes, and contexts. Host implementations validate registrations before exposing functions. Schemes are authoritative at invocation boundaries. Opaque values preserve metadata, visible snapshots, and payloads; constructible records carry visible data only. Both record construction and updates remain forbidden for opaque types.

Reflection helpers reject unsupported/recursive projections, hidden constructible fields, and absent/mismatched referenced descriptors before invoking handlers. Argument conversion completes before handler execution. Handler and conversion failures use ExtensionException and retain inner causes. Manual implementations own their conversions.

Callable/task handles are host-created and session-bound. Invocation contexts expire after the handler returns. Hosts reject invalid handle implementations, cross-session use, and use after evaluation expires. Hosts enforce runtime limits and preserve callback source errors independently of SDK exceptions.

Compatible 1.x releases retain unsigned AssemblyVersion 1.0.0.0 and public record/union shapes. API snapshot validation is required for every change; package validation compares future compatible releases against 1.0.0. FSharp.Core remains an explicit package dependency.
