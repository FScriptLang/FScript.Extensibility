namespace FScript.Extensibility

open System

[<Sealed>]
type ExtensionException(message: string, inner: Exception | null) =
    inherit Exception(message, inner)
    new(message: string) = ExtensionException(message, null)

[<AttributeUsage(AttributeTargets.Method, AllowMultiple = false)>]
type FScriptExternProviderAttribute() = inherit Attribute()

type MapKey = MKString of string | MKInt of int64 | MKBool of bool | MKUnionCase of string * string

/// Opaque host-owned handle. Use only through ExternalCallContext.
type ICallableHandle = interface end
type ITaskHandle = interface end

type ScriptValue =
    | VUnit | VInt of int64 | VFloat of float | VBool of bool | VString of string
    | VList of ScriptValue list | VTuple of ScriptValue list
    | VRecord of Map<string, ScriptValue> | VMap of Map<MapKey, ScriptValue>
    | VOption of ScriptValue option
    | VUnionCase of string * string * ScriptValue option
    | VTypeToken of ScriptType
    | VCallable of ICallableHandle | VTask of ITaskHandle
    | VHostManaged of HostManagedValue
and HostManagedValue =
    { Descriptor: HostTypeDescriptor; Fields: Map<string, ScriptValue>; Payload: obj }
and [<Sealed>] ExternalCallContext(apply: ScriptValue -> ScriptValue -> ScriptValue, spawnTask: ScriptValue -> ScriptValue, awaitTask: ScriptValue -> ScriptValue, checkRuntime: unit -> unit) =
    member _.Apply = apply
    member _.SpawnTask = spawnTask
    member _.AwaitTask = awaitTask
    member _.CheckRuntime = checkRuntime
and ExternalFunction =
    { Name: string; Scheme: FunctionScheme; Impl: ExternalCallContext -> ScriptValue list -> ScriptValue; Arity: int }

type ExtensionRegistration =
    { Types: HostTypeDescriptor list; Unions: HostUnionDescriptor list; Functions: ExternalFunction list }

[<Sealed>]
type HostContext(rootDirectory: string, deniedPathGlobs: string list, defaultFunctions: ExternalFunction list) =
    member _.RootDirectory = rootDirectory
    member _.DeniedPathGlobs = deniedPathGlobs
    member _.DefaultFunctions = defaultFunctions
