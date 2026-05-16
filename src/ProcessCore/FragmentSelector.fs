namespace ProcessCore

/// Semantic relation between two fragment selectors.
type FragmentRelation =
    | Exact
    | Contains
    | Disjoint
    | Unknown

/// Selector-provider contract used by the core traversal layer.
type IFragmentSelectorProvider =
    
    abstract SelectorFormat: string

    /// Relates the first selector as the possible container of the second selector.
    abstract TryRelate: container: string -> candidate: string -> FragmentRelation option

/// Typed selector-provider contract for implementations of a selector language.
[<AbstractClass>]
type FragmentSelectorProviderBase<'Selector>() =    

    abstract SelectorFormat: string
    abstract TryParse: string -> 'Selector option
    abstract ToSelectorString: 'Selector -> string
    abstract Relate: container: 'Selector -> candidate: 'Selector -> FragmentRelation

    interface IFragmentSelectorProvider with

        member this.SelectorFormat =
            this.SelectorFormat

        member this.TryRelate container candidate =
            match this.TryParse container, this.TryParse candidate with
            | Some c, Some s -> this.Relate c s |> Some
            | _ -> None

module FragmentSelector =

    let relate
        (containerPath: string)
        (containerSelector: string option)
        (containerSelectorFormat: string option)
        (candidatePath: string)
        (candidateSelector: string option)
        (candidateSelectorFormat: string option)
        (tryGetProvider: string -> IFragmentSelectorProvider option)
        : FragmentRelation =

        if containerPath <> candidatePath then Disjoint
        else
            match containerSelector, candidateSelector with
            | None, None -> Exact
            | None, Some _ -> Contains
            | Some _, None -> Unknown
            | Some a, Some b when a = b -> Exact
            | Some a, Some b ->
                match containerSelectorFormat, candidateSelectorFormat with
                | Some cf, Some xf when cf = xf ->
                    match tryGetProvider cf with
                    | Some provider ->
                        match provider.TryRelate a b with
                        | Some r -> r
                        | None -> Unknown
                    | None -> Unknown
                | _ -> Unknown
