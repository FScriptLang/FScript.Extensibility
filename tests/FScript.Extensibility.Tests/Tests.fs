namespace FScript.Extensibility.Tests

open System
open NUnit.Framework
open FScript.Extensibility

[<FScriptHostType("Node")>]
type Node = { [<FScriptVisible>] Value: int64; Secret: Guid }
[<FScriptHostType("Mode", ScriptConstructible=true)>]
type Mode = Idle | Pair of int64 * bool | Names of Set<string>
[<FScriptHostType("Data", ScriptConstructible=true)>]
type Data = { [<FScriptVisible>] Mode: Mode; [<FScriptVisible>] Items: Map<string, Node option list> }
[<FScriptHostType("Invalid", ScriptConstructible=true)>]
type Invalid = { Hidden: int }
[<FScriptHostType("Recursive", ScriptConstructible=true)>]
type Recursive = Next of Recursive

[<TestFixture>]
type Contracts() =
    let registration = { HostInterop.empty with Types = [HostTypes.describe<Node>(); HostTypes.describe<Data>()]; Unions = [HostTypes.describeUnion<Mode>()] }
    let context = ExternalCallContext((fun _ _ -> failwith "unused"), (fun _ -> failwith "unused"), (fun _ -> failwith "unused"), ignore)
    [<Test>]
    member _.``SDK has stable unsigned identity and no implementation dependency``() =
        let assembly = typeof<ScriptValue>.Assembly
        Assert.That(assembly.GetName().Name, Is.EqualTo "FScript.Extensibility")
        Assert.That(assembly.GetName().Version, Is.EqualTo(Version(1,0,0,0)))
        Assert.That(assembly.GetName().GetPublicKeyToken(), Is.Empty)
        for reference in assembly.GetReferencedAssemblies() do
            Assert.That(reference.Name, Does.Not.StartWith("FScript.Language").And.Not.StartWith("FScript.Runtime"))
    [<Test>]
    member _.``Opaque state and visible snapshots survive nested union container conversion``() =
        let node = { Value = 42L; Secret = Guid.NewGuid() }
        let source = { Mode = Names(set ["z";"a"]); Items = Map.ofList ["key", [Some node; None]] }
        let wrapped = HostTypes.wrap registration source
        let restored = HostTypes.unwrap<Data> wrapped
        Assert.That(restored, Is.EqualTo source)
        match wrapped with
        | VRecord fields ->
            match fields["Mode"] with
            | VUnionCase("Mode", "Names", Some(VList [VString "a"; VString "z"])) -> ()
            | other -> Assert.Fail($"Unexpected union: {other}")
        | other -> Assert.Fail($"Expected constructible record: {other}")
    [<Test>]
    member _.``Typed functions infer arity and validate before side effects``() =
        let mutable count = 0
        let fn = HostFunctions.create HostInterop.empty "Host.add" (fun (a: int) (b: int64) -> count <- count + 1; int64 a + b)
        Assert.That(fn.Arity, Is.EqualTo 2)
        match fn.Impl context [VInt 40L; VInt 2L] with VInt 42L -> () | _ -> Assert.Fail("Incorrect result")
        Assert.Throws<ExtensionException>(Action(fun () -> fn.Impl context [VInt(Int64.MaxValue);VInt 2L] |> ignore)) |> ignore
        Assert.That(count, Is.EqualTo 1)
        Assert.Throws<ExtensionException>(Action(fun () -> fn.Impl context [VInt 1L] |> ignore)) |> ignore
    [<Test>]
    member _.``Set input applies CLR ordering and deduplication``() =
        let fn = HostFunctions.create HostInterop.empty "Host.set" (fun (items: Set<int64>) -> Set.toList items)
        match fn.Impl context [VList [VInt 3L; VInt 1L; VInt 3L]] with
        | VList [VInt 1L; VInt 3L] -> ()
        | other -> Assert.Fail($"Unexpected set conversion: {other}")
    [<Test>]
    member _.``Tuple union payloads round trip``() =
        let value = VUnionCase("Mode", "Pair", Some(VTuple[VInt 42L; VBool true]))
        Assert.That(HostTypes.unwrap<Mode> value, Is.EqualTo(Pair(42L,true)))
    [<Test>]
    member _.``Invalid declarations and incomplete registrations fail before invocation``() =
        Assert.Throws<ExtensionException>(Action(fun () -> HostTypes.describe<Invalid>() |> ignore)) |> ignore
        Assert.Throws<ExtensionException>(Action(fun () -> HostTypes.describeUnion<Recursive>() |> ignore)) |> ignore
        Assert.Throws<ExtensionException>(Action(fun () -> HostFunctions.create HostInterop.empty "Host.node" (fun (_: unit) -> (None: Node option)) |> ignore)) |> ignore
        Assert.Throws<ExtensionException>(Action(fun () -> HostInterop.typeDefinitions { registration with Types = registration.Types @ registration.Types } [] |> ignore)) |> ignore
    [<Test>]
    member _.``Host context exposes only supplied defaults``() =
        let fn = HostFunctions.create HostInterop.empty "Host.value" (fun () -> 42L)
        let host = HostContext("/workspace", [".git"], [fn])
        Assert.That(host.RootDirectory, Is.EqualTo "/workspace")
        Assert.That(host.DefaultFunctions.Head.Name, Is.EqualTo "Host.value")

    [<Test>]
    member _.``Handler failures retain their original cause``() =
        let cause = InvalidOperationException "resource unavailable"
        let fn = HostFunctions.create HostInterop.empty "Host.fail" (fun () -> raise cause : int64)
        let error = Assert.Throws<ExtensionException>(Action(fun () -> fn.Impl context [VUnit] |> ignore)) |> nonNull
        Assert.That(error.InnerException, Is.SameAs(cause))
        Assert.That(error.Message, Does.Contain("Host.fail"))
