# Extension contracts (SDK 3)

The normative public API is `api/PublicAPI.txt`; descriptor fields and mappings
are documented in the [README](../../README.md). The SDK must not reference
Language or Runtime, or expose inference witnesses, closure environments, runtime
state, schedulers, threads, or task workers. FSharp.Core is an explicit dependency.

Public record and union shapes remain frozen within major version 3. Generic
opaque descriptors carry type parameters and named applications; validation checks
registered descriptor identity and arity. Callable and task values are opaque,
owned by the host, and authenticated against the active evaluation session.

## Asynchronous library operations

ExternalCallContext has one constructor taking `apply`, `exposeAsync`, and
`checkRuntime`. Its only capabilities are Apply, ExposeAsync, and CheckRuntime.

`ExposeAsync : (CancellationToken -> Task<ScriptValue>) -> ScriptValue` exposes a
library-owned asynchronous operation as an opaque script Task. The host invokes
the operation factory directly during the library call; this neither schedules a
worker nor waits for completion. The factory must return promptly. Async I/O can
then complete through ordinary platform continuations. No Task.Run/StartNew or
blocking wait is supplied or implied by this contract.

The cancellation token belongs to the host. The host authenticates the resulting
task handle, transports completion values/errors, and includes pending I/O in its
scope cleanup/join policy. Libraries must propagate cancellation into their I/O.
The invocation context expires when the library function returns; asynchronous
continuations must use the cancellation token rather than retain that context.
Scripts choose task spawning, composition, and explicit awaiting.

Runtime requires Native to load/call SDK providers. Exposing asynchronous I/O
does not additionally require Threading. Script Task operations retain their own
Threading checks. Providers cannot choose or grant permissions. Automatically
supplied default functions exclude operations requiring Threading, including Task,
Mailbox, and child-script execution.

## Migration from SDK 2

SDK 3 removes SpawnTask, SpawnWorker, AwaitTask, and the previous context
constructors. Libraries
return async operations through ExposeAsync; scripts perform Task.await. Worker
scheduling needed by Runtime services such as Mailbox stays private to Runtime.
There is no compatibility overload supplying hidden worker/await callbacks.

AssemblyVersion is 3.0.0.0 for compatible 3.x releases. Providers/hosts must rebuild
against SDK 3; SDK 2 binaries are rejected by the host ABI check. API snapshot
validation is required for every change. Compatible releases after 3.0.0 use the
published 3.0.0 package as their validation baseline.
