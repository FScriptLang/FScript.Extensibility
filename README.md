# FScript.Extensibility

Development uses .NET SDK 11.0.100, pinned in `global.json`; tests run on .NET 11.
The published library continues targeting `netstandard2.1`.

Stable contracts and authoring helpers for FScript extensions. Install `MagnusOpera.FScript.Extensibility` 3.0.0 and open `FScript.Extensibility`. Extensions need no reference to FScript.Language or FScript.Runtime.

```fsharp
open FScript.Extensibility

[<FScriptHostType("Point", ScriptConstructible = true)>]
type Point = { [<FScriptVisible>] X: int64; [<FScriptVisible>] Y: int64 }

module Functions =
    let types = { HostInterop.empty with Types = [HostTypes.describe<Point>()] }
    let move = HostFunctions.create types "Geometry.move" (fun (point: Point) -> { point with X = point.X + 1L })

type Provider =
    [<FScriptExternProvider>]
    static member Provide(context: HostContext) : ExtensionRegistration =
        { Functions.types with Functions = [Functions.move] }
```

Compile the provider, list its assembly in `fscript.toml` under `[runtime] extern-assemblies`, then run the script with FScript or select the manifest in VS Code. Provider loading executes trusted host code. This library neither loads assemblies nor executes scripts.

## Descriptors and registrations

Helpers construct ordinary SDK records. You may author these records directly for custom schemes, snapshots, callbacks, or tasks.

### HostTypeDescriptor

| Field | Type | Purpose |
| --- | --- | --- |
| `Name` | `string` | Nominal script type name. |
| `Fields` | `Map<string, ScriptType>` | Visible snapshot fields; hidden state is absent. |
| `HostManaged` | `bool` | `true` requires host-created opaque values; `false` permits ordinary script records and updates. |

### HostUnionDescriptor

| Field | Type | Purpose |
| --- | --- | --- |
| `Name` | `string` | Nominal union name. |
| `Cases` | `Map<string, ScriptType option>` | Case names and optional payload types; multiple CLR case fields form a tuple. |

### ExternalFunction

| Field | Type | Purpose |
| --- | --- | --- |
| `Name` | `string` | Script-visible function or value name. |
| `Scheme` | `FunctionScheme` | Authoritative type, expressed as `Forall(quantifiedVariables, ScriptType)`. |
| `Arity` | `int` | Number of arguments before invocation; zero represents an eagerly supplied external value. |
| `Impl` | `ExternalCallContext -> ScriptValue list -> ScriptValue` | Handler with explicit boundary values and interpreter operations. |

### ExtensionRegistration

| Field | Type | Purpose |
| --- | --- | --- |
| `Types` | `HostTypeDescriptor list` | Provided record/opaque types. |
| `Unions` | `HostUnionDescriptor list` | Provided constructible unions. |
| `Functions` | `ExternalFunction list` | Function/value descriptors. |

`HostInterop.empty` supplies empty lists. Registration is explicit, local to the host environment, and rejects duplicate names. Hosts additionally reject conflicts with their built-in and script types.

### HostManagedValue

| Field | Type | Purpose |
| --- | --- | --- |
| `Descriptor` | `HostTypeDescriptor` | Complete nominal metadata. |
| `Fields` | `Map<string, ScriptValue>` | Immutable visible snapshot. |
| `Payload` | `obj` | Opaque host state; never available to scripts. |

## Types, values, and CLR helpers

`ScriptType` describes primitives, lists, options, tuples, maps, functions, tasks, nominal unions, records, type tokens, opaque types, and quantified/numeric variables. It carries no inference witnesses. `ScriptValue` represents their data, with `VCallable` and `VTask` holding opaque interfaces instead of closures or workers.

`HostTypes.describe<'T>()` inspects `[<FScriptHostType>]` types. Only `[<FScriptVisible>]` properties become fields. Opaque mode is the default. `ScriptConstructible = true` requires an F# record whose constructor fields are all visible, or a supported nonrecursive F# union. Hidden fields on constructible records are declaration errors.

`HostTypes.describeUnion<'T>()`, `wrap registration value`, and `unwrap<'T> value` share conversions with `HostFunctions.create registration name handler`. Supported CLR types are unit, bool, string, int/int64, float, lists, one-dimensional arrays, sets, options, tuples/struct tuples, string-keyed F# maps, and registered attributed records/unions. Sets project to lists and reconstruct with set ordering/deduplication. Int32 input conversion checks bounds. Nulls and unsupported mappings fail explicitly. Typed handlers are synchronous and monomorphic; callbacks/tasks and polymorphic schemes use manual descriptors.

Visible getters are snapshotted when wrapping; opaque payloads are retained without reconstruction. `HostInterop.create` supports manually supplied snapshots and arbitrary payloads. Hidden payloads do not participate in script equality, formatting, or serialization.

## Host and invocation contexts

| Context member | Purpose |
| --- | --- |
| `HostContext.RootDirectory` | Host-resolved root for providers. |
| `HostContext.DeniedPathGlobs` | Host-supplied denied-path patterns. |
| `HostContext.DefaultFunctions` | SDK registrations supplied by the host; providers can filter them without Runtime access. |
| `ExternalCallContext.Apply` | Invoke an opaque callable with one argument. |
| `ExternalCallContext.ExposeAsync` | Expose cancellable asynchronous I/O as an opaque script task without spawning work or waiting. |
| `ExternalCallContext.CheckRuntime` | Enforce host runtime/cancellation limits. |

Contexts are sealed classes with read-only members. Invocation contexts expire when their handler returns. Handles belong to the originating active evaluation; passing them through another extension call in that evaluation is supported. Invented, foreign, or expired handles fail. Do not retain contexts for later asynchronous use. `ExtensionException` carries a message and optional inner exception, without interpreter spans or engine exceptions in the contract.

Async functions keep their normal script signature, for example `string -> string task`:

```fsharp
Impl = fun context arguments ->
    context.ExposeAsync(fun cancellation -> task {
        let! text = client.ReadTextAsync(path, cancellation)
        return VString text
    })
```

The script decides when to `Task.await` the returned value. The factory is invoked
inline and must return promptly; the host tracks completion without creating a
worker. Native provider trust is still required, while async I/O registration does
not require Threading. Automatic default-function catalogs omit Task/Mailbox.
Scheduling and awaiting are absent from SDK 3's context. Use only the host's
cancellation token after the original invocation returns.

A provider method is public/static, marked `[<FScriptExternProvider>]`, accepts zero arguments or one `HostContext`, and returns `ExternalFunction list` or `ExtensionRegistration`. Return only provider-owned functions when defaults are also enabled by the loader, avoiding duplicate registrations.

## Embedding and compatibility

An embedding application additionally references Runtime and converts SDK registrations with `FScript.Runtime.ExtensionBridge.toLanguageRegistration`. Native embedding values stay in Language; CLR conversion belongs in SDK handlers. Callback failures preserve script locations at the Runtime boundary. Language has no SDK dependency and retains its Fable boundary.

The SDK targets `netstandard2.1`, explicitly depends on FSharp.Core, and has no Language, Runtime, TOML, or serialization dependency. Package/file/informational versions follow releases. The unsigned assembly identity stays `FScript.Extensibility, Version=3.0.0.0` throughout compatible 3.x releases. Public record and discriminated-union shapes are frozen in 3.x. SDK 2 hosts and providers must rebuild: SDK 3 removes spawning/awaiting members and the old context constructors.

## Development and release

Run `make build test verify-api pack verify-changelog`. `api/PublicAPI.txt` is the reviewed public API baseline; `make update-api` is an explicit review action, never an automatic CI fix. Compatible releases after 3.0.0 enable NuGet package validation against published 3.0.0. SDK 3 intentionally breaks the former context ABI.

`make release-prepare version=X.Y.Z dryrun=true` previews preparation from a clean checkout. Preparation validates checks, moves Unreleased notes, creates the release commit and annotated tag. The first compare link uses the bootstrap commit; later releases compare version tags. Push with `git push origin main --follow-tags`, wait for the tag workflow's draft, verify changelog-derived notes/artifacts, then publish that draft. The published-release workflow uses [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing). The policy for `FScriptLang/FScript.Extensibility` references `on-release-published.yml` and environment `release`. The workflow uses the policy creator's NuGet username to obtain a short-lived key. No signing or product/website release is performed here.

See [contract specification](docs/specs/contracts.md) and [architecture](docs/architecture/README.md). Contributor requirements are in [AGENTS.md](AGENTS.md).
