namespace ProcessCore.Spreadsheet

open ProcessCore
open DynamicObj
open System.Text.RegularExpressions
open ProcessCore.Helper.Regex.ActivePatterns

module Comment = 

    
    let commentValueKey = "commentValue"

    let commentPattern = $@"Comment\s*\[<(?<{commentValueKey}>.+)>\]"

    let commentPatternNoAngleBrackets = $@"Comment\s*\[(?<{commentValueKey}>.+)\]"

    let create (k : string option) (v : string option) =
        let d = DynamicObj()
        if k.IsSome then d.SetProperty("Name", k.Value)
        if v.IsSome then d.SetProperty("Value", v.Value)
        d

    let (|Comment|_|) (key : string) =
        
        match key with
        | Regex commentPattern r ->
            Some r.Groups.[commentValueKey].Value
        | Regex commentPatternNoAngleBrackets r -> 
            let v = r.Groups.[commentValueKey].Value
            if v = "<>" then None else Some v
        | _ -> None
        
   
    let wrapCommentKey k = 
        sprintf "Comment[%s]" k

    let fromString k v =
       let d = DynamicObj()
       d.SetProperty("Name", k)
       d.SetProperty("Value", v)
       d

    let toString (d : #DynamicObj) =
        d.TryGetPropertyValue("Name") |> Option.bind (fun n -> match n with | :? string as s -> Some s | _ -> None) ,        
        d.TryGetPropertyValue("Value") |> Option.bind (fun v -> match v with | :? string as s -> Some s | _ -> None)

    let getCommentsFromDynamicObj (d : #DynamicObj) =
        d.GetProperties(false)
        |> Seq.choose (fun p -> 
            match p.Key with
            | Comment k -> 
                match p.Value with
                | :? string as v -> Some (k, v)
                | _ -> None
            | _ -> None
        )

module Remark = 

    let remarkValueKey = "remarkValue"

    let remarkPattern = $@"#(?<{remarkValueKey}>.+)"


    let (|Remark|_|) (key : Option<string>) =
        key
        |> Option.bind (fun k ->
            match k with
            | Regex remarkPattern r ->
                Some r.Groups.[remarkValueKey].Value
            | _ -> None
        )


    let wrapRemark r = 
        sprintf "#%s" r