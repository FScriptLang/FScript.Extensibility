open System
open System.IO
open System.Reflection
open Microsoft.FSharp.Reflection

let args = fsi.CommandLineArgs |> Array.skip 1
if args.Length <> 2 then failwith "Usage: api.fsx assembly baseline"
let assembly = Assembly.LoadFrom(Path.GetFullPath args[0])
let flags = BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.Static ||| BindingFlags.DeclaredOnly
let lines =
    assembly.GetExportedTypes()
    |> Array.collect (fun t ->
        let shape =
            if FSharpType.IsRecord t then
                FSharpType.GetRecordFields t
                |> Array.mapi (fun index property -> $"RECORD {t.FullName}[{index}] {property.Name}: {property.PropertyType}")
            elif FSharpType.IsUnion t then
                FSharpType.GetUnionCases t
                |> Array.map (fun case ->
                    let fields = case.GetFields() |> Array.map (fun property -> $"{property.Name}: {property.PropertyType}") |> String.concat "; "
                    $"UNION {t.FullName}[{case.Tag}] {case.Name}({fields})")
            else [||]
        let interfaces = t.GetInterfaces() |> Array.map string |> Array.sort |> String.concat ","
        Array.concat [
            [|$"TYPE {t.FullName} : {t.BaseType}; {t.Attributes}; interfaces={interfaces}"|]
            shape
            t.GetMembers(flags) |> Array.map (fun m ->
                let detail =
                    match m with
                    | :? FieldInfo as field when field.IsLiteral -> $" = {field.GetRawConstantValue()}"
                    | :? MethodBase as method ->
                        let parameters = method.GetParameters() |> Array.map (fun parameter -> parameter.Name) |> String.concat ","
                        $"; parameters={parameters}"
                    | _ -> ""
                $"MEMBER {t.FullName}: {m.MemberType} {m}{detail}") ])
    |> Array.sort
let actual = String.Join("\n", lines) + "\n"
if Environment.GetEnvironmentVariable("UPDATE_API") = "true" then File.WriteAllText(args[1], actual)
elif not (File.Exists args[1]) || File.ReadAllText(args[1]).Replace("\r\n", "\n") <> actual then
    failwith "Public API differs from the approved baseline. Review ABI/record/union compatibility before explicitly running make update-api."
printfn "Public API matches %s" args[1]
