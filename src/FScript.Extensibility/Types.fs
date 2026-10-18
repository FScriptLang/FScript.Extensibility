namespace FScript.Extensibility

open System

[<StructuralEquality; StructuralComparison>]
type ScriptType =
    | TUnit
    | TInt
    | TFloat
    | TBool
    | TString
    | TList of ScriptType
    | TTask of ScriptType
    | TTuple of ScriptType list
    | TRecord of Map<string, ScriptType>
    | TMap of ScriptType * ScriptType
    | TOption of ScriptType
    | TFun of ScriptType * ScriptType
    | TNamed of string
    | TUnion of string * Map<string, ScriptType option>
    | TTypeToken
    | TVar of int
    | TNumeric of int
    | THostManaged of name: string * fields: Map<string, ScriptType>

type HostTypeDescriptor =
    { Name: string
      Fields: Map<string, ScriptType>
      HostManaged: bool }

/// Script-constructible union supplied by a host; payloads use ordinary script types.
type HostUnionDescriptor =
    { Name: string
      Cases: Map<string, ScriptType option> }

[<StructuralEquality; StructuralComparison>]
type FunctionScheme = Forall of int list * ScriptType

module Types =
    let rec ftvType t =
        match t with
        | TUnit | TInt | TFloat | TBool | TString -> Set.empty
        | TList t1 -> ftvType t1
        | TTask t1 -> ftvType t1
        | TTuple ts -> ts |> List.map ftvType |> List.fold Set.union Set.empty
        | THostManaged _ -> Set.empty
        | TRecord fields -> fields |> Map.values |> Seq.map ftvType |> Seq.fold Set.union Set.empty
        | TMap (tk, tv) -> Set.union (ftvType tk) (ftvType tv)
        | TOption t1 -> ftvType t1
        | TFun (a, b) -> Set.union (ftvType a) (ftvType b)
        | TNamed _ | TUnion _ | TTypeToken -> Set.empty
        | TVar v | TNumeric v -> Set.singleton v


    let rec typeToString t =
        let rec go t =
            match t with
            | TUnit -> "unit"
            | TInt -> "int"
            | TFloat -> "float"
            | TBool -> "bool"
            | TString -> "string"
            | TList t1 -> sprintf "%s list" (postfixArg t1)
            | TTask t1 -> sprintf "%s task" (postfixArg t1)
            | TTuple ts -> ts |> List.map go |> String.concat " * " |> sprintf "(%s)"
            | THostManaged (name, _) -> name
            | TRecord fields ->
                fields
                |> Map.toList
                |> List.map (fun (name, t) -> sprintf "%s: %s" name (go t))
                |> String.concat "; "
                |> sprintf "{ %s }"
            | TMap (tk, tv) ->
                match tk with
                | TString -> sprintf "%s map" (postfixArg tv)
                | _ -> sprintf "%s map[%s]" (postfixArg tv) (go tk)
            | TOption t1 -> sprintf "%s option" (postfixArg t1)
            | TFun (a, b) -> sprintf "(%s -> %s)" (go a) (go b)
            | TNamed n -> n
            | TUnion (name, _) -> name
            | TTypeToken -> "type"
            | TNumeric _ -> "numeric"
            | TVar v ->
                let letter = char (int 'a' + (v % 26))
                let suffix = v / 26
                if suffix = 0 then sprintf "'%c" letter else sprintf "'%c%d" letter suffix
        and postfixArg t =
            match t with
            | TFun _ | TTuple _ | TRecord _ -> sprintf "(%s)" (go t)
            | _ -> go t
        go t
