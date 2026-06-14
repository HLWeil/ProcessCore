namespace ProcessCore

module Helper =

    module Regex = 
        open System

        module Pattern =

            let handleGroupPatterns (pattern : string) =
                let pyify (pattern : string) =
                    pattern.Replace(@"(?<", @"(?P<")
                #if FABLE_COMPILER_PYTHON
                pyify pattern
                #else
                pattern
                #endif

            module MatchGroups =
                
                [<Literal>]
                let numberFormat = "numberFormat"

                [<Literal>]
                let localID = "localid"

                [<Literal>]
                let idspace = "idspace"

            /// Hits term accession, without id: ENVO:01001831
            let TermAnnotationShortPattern = $@"(?<{MatchGroups.idspace}>\w+?):(?<{MatchGroups.localID}>\w+)" //prev: @"[\w]+?:[\d]+"

            // https://obofoundry.org/id-policy.html#mapping-of-owl-ids-to-obo-format-ids
            /// <summary>Regex pattern is designed to hit only Foundry-compliant URIs.</summary>
            let TermAnnotationURIPattern = $@"http://purl.obolibrary.org/obo/(?<{MatchGroups.idspace}>\w+?)_(?<{MatchGroups.localID}>\w+)"

            /// Watch this closely, this could hit some edge cases we do not want to cover.
            let TermAnnotationURIPattern_lessRestrictive = $@".*\/(?<{MatchGroups.idspace}>\w+?)[:_](?<{MatchGroups.localID}>\w+)"

            /// Watch this closely, this could hit some edge cases we do not want to cover.
            let TermAnnotationURIPattern_MS_RO_PO = $@".*252F(?<{MatchGroups.idspace}>\w+?)_(?<{MatchGroups.localID}>\w+)"

        module ActivePatterns =
            
            open System.Text.RegularExpressions
            
            /// Matches, if the input string matches the given regex pattern.
            let (|Regex|_|) pattern (input : string) =
                let pattern = Pattern.handleGroupPatterns pattern
                let m = Regex.Match(input.Trim(), pattern)
                if m.Success then Some(m)
                else None

            /// Matches a short term string and returns the term source ref and the annotation number strings.
            /// 
            /// Example: "MS:1003022" --> term source ref: "MS"; annotation number: "1003022"
            let (|TermAnnotationShort|_|) input =
                match input with
                | Regex Pattern.TermAnnotationShortPattern value ->
                    let idspace = value.Groups.[Pattern.MatchGroups.idspace].Value
                    let localID = value.Groups.[Pattern.MatchGroups.localID].Value
                    {|IDSpace = idspace; LocalID = localID|}
                    |> Some
                | _ ->
                    None

            /// Matches a term string (either short or URI) and returns the term source ref and the annotation number strings.
            /// 
            /// Example 1: "MS:1003022" --> term source ref: "MS"; annotation number: "1003022"
            ///
            /// Example 2: "http://purl.obolibrary.org/obo/MS_1003022" --> term source ref: "MS"; annotation number: "1003022"
            let (|TermAnnotation|_|) input =
                match input with
                | Regex Pattern.TermAnnotationShortPattern value 
                | Regex Pattern.TermAnnotationURIPattern value 
                | Regex Pattern.TermAnnotationURIPattern_lessRestrictive value 
                | Regex Pattern.TermAnnotationURIPattern_MS_RO_PO value ->
                    let idspace = value.Groups.[Pattern.MatchGroups.idspace].Value
                    let localID = value.Groups.[Pattern.MatchGroups.localID].Value
                    {|IDSpace = idspace; LocalID = localID|}
                    |> Some
                | _ ->
                    None


        open Pattern
        open ActivePatterns
        open System
        open System.Text.RegularExpressions
            
        let tryParseTermAnnotationShort (str:string) =
            match str.Trim() with
            | Regex TermAnnotationShortPattern value ->
                let idspace = value.Groups.[Pattern.MatchGroups.idspace].Value
                let localid = value.Groups.[Pattern.MatchGroups.localID].Value
                {|IDSpace = idspace; LocalID = localid|} 
                |> Some
            | _ -> None

        /// <summary>
        /// This function can be used to extract `IDSPACE:LOCALID` (or: `Term Accession`) from Swate header strings or obofoundry conform URI strings.
        /// 
        /// **Example 1:** "http://purl.obolibrary.org/obo/GO_000001" --> "GO:000001"
        /// 
        /// **Example 2:** "Term Source REF (NFDI4PSO:0000064)" --> "NFDI4PSO:0000064"
        /// </summary>
        let tryParseTermAnnotation (str:string) =
            match str.Trim() with
            | Regex TermAnnotationShortPattern value 
            | Regex TermAnnotationURIPattern value 
            | Regex TermAnnotationURIPattern_lessRestrictive value 
            | Regex TermAnnotationURIPattern_MS_RO_PO value ->
                let idspace = value.Groups.[Pattern.MatchGroups.idspace].Value
                let localid = value.Groups.[Pattern.MatchGroups.localID].Value
                {|IDSpace = idspace; LocalID = localid|}
                |> Some
            | _ ->
                None

        /// Tries to parse 'str' to term accession and returns it in the format `Some "termsourceref:localtan"`. Exmp.: `Some "MS:000001"`
        let tryGetTermAnnotationShortString (str:string) = 
            tryParseTermAnnotation str
            |> Option.map (fun r -> r.IDSpace + ":" + r.LocalID)

        /// Parses 'str' to term accession and returns it in the format "termsourceref:localtan". Exmp.: "MS:000001"
        let getTermAnnotationShortString (str:string) =
            match tryGetTermAnnotationShortString str with
            | Some s -> s
            | None -> failwith $"Unable to parse '{str}' to term accession."

    let computeTanInfo (tan : string option) (tsr : string option) =
        match tan with
        | Some tan -> 
            match Regex.tryParseTermAnnotation tan with
            | Some ta -> Some ta
            | None ->
                match tsr with
                | Some "" | None -> None
                | Some tsr -> 
                    Some {| IDSpace = tsr; LocalID = tan |}
        | None -> None