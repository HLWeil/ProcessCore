namespace rec ProcessCore

open Fable.Core
open System.Collections.Generic
open DynamicObj

/// Generic Data-fragment relation helpers used by traversal and tests.
module FragmentSelectorResolution =

    let relateDataWithProvider (provider: IFragmentSelectorProvider) (container: Data) (candidate: Data) : FragmentRelation =
        FragmentSelector.relate
            container.Path
            container.Selector
            container.SelectorFormat
            candidate.Path
            candidate.Selector
            candidate.SelectorFormat
            (fun s -> if s = provider.SelectorFormat then Some provider else None)

    let relateDataWith (tryGetProvider: string -> IFragmentSelectorProvider option) (container: Data) (candidate: Data): FragmentRelation =
        FragmentSelector.relate
            container.Path
            container.Selector
            container.SelectorFormat
            candidate.Path
            candidate.Selector
            candidate.SelectorFormat
            tryGetProvider

    let relateData (container: Data) (candidate: Data) : FragmentRelation =
        relateDataWith (fun _ -> None) (container) (candidate)

[<AutoOpen>]
module private Comparers =
    /// Reference-equality comparer for back-edge HashSets.
    /// Process.Equals is name-based, so without this two distinct process
    /// objects with the same name would collide in the set.
    let refEqProcess =
        { new IEqualityComparer<Process> with
            member _.Equals(x, y)   = obj.ReferenceEquals(x, y)
            member _.GetHashCode(o) = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o) }

// ─────────────────────────────────────────────────────────────────────────────
// Path
// ─────────────────────────────────────────────────────────────────────────────

/// A directed walk through the process graph: an ordered sequence of Process
/// instances connected through their shared I/O nodes.
/// Read-only view – produced by graph-traversal queries; does not own its processes.
[<AttachMembers>]
type Path(processes: ResizeArray<Process>) =

    member _.Processes = processes

    /// First process in the path
    member _.Head = if processes.Count > 0 then Some processes.[0] else None

    /// Last process in the path
    member _.Last = if processes.Count > 0 then Some processes.[processes.Count - 1] else None

    member _.Length = processes.Count

    /// Whether this path contains the given IONode (as input or output of any process)
    member _.ContainsNode(node: IONode) : bool =
        let key = node.Key()
        processes |> Seq.exists (fun (p: Process) ->
            p.Input |> Option.exists (fun (n: IONode) -> n.Key() = key) ||
            p.Output |> Option.exists (fun (n: IONode) -> n.Key() = key))

    /// All distinct IONodes that appear anywhere in this path (inputs or outputs)
    member _.Nodes() : ResizeArray<IONode> =
        let acc  = ResizeArray<IONode>()
        let seen = HashSet<string>()
        for proc in processes do
            proc.Input |> Option.iter (fun (n: IONode) -> if seen.Add(n.Key()) then acc.Add(n))
            proc.Output |> Option.iter (fun (n: IONode) -> if seen.Add(n.Key()) then acc.Add(n))
        acc

    /// All distinct Sample nodes in this path
    member this.Samples() : ResizeArray<Sample> =
        let acc = ResizeArray<Sample>()
        for node in this.Nodes() do
            match node with
            | SampleNode m -> acc.Add(m)
            | _ -> ()
        acc

    /// All distinct Data nodes in this path
    member this.DataNodes() : ResizeArray<Data> =
        let acc = ResizeArray<Data>()
        for node in this.Nodes() do
            match node with
            | DataNode d -> acc.Add(d)
            | _ -> ()
        acc

    /// Nodes that appear as inputs but never as outputs in this path (true sources)
    member _.TerminalInputs() : ResizeArray<IONode> =
        let outputKeys = HashSet<string>()
        for proc in processes do
            proc.Output |> Option.iter (fun n -> outputKeys.Add(n.Key()) |> ignore)
        let acc  = ResizeArray<IONode>()
        let seen = HashSet<string>()
        for proc in processes do
            match proc.Input with
            | Some n ->
                let k = n.Key()
                if not (outputKeys.Contains(k)) && seen.Add(k) then acc.Add(n)
            | None -> ()
        acc

    /// Nodes that appear as outputs but never as inputs in this path (true sinks)
    member _.TerminalOutputs() : ResizeArray<IONode> =
        let inputKeys = HashSet<string>()
        for proc in processes do
            proc.Input |> Option.iter (fun n -> inputKeys.Add(n.Key()) |> ignore)
        let acc  = ResizeArray<IONode>()
        let seen = HashSet<string>()
        for proc in processes do
            match proc.Output with
            | Some n ->
                let k = n.Key()
                if not (inputKeys.Contains(k)) && seen.Add(k) then acc.Add(n)
            | None -> ()
        acc

    /// All Annotations from all sources (parameters, input/output node properties, recipe components)
    /// across all processes in this path
    member _.AllAnnotations() : ResizeArray<Annotation> =
        PathTraversal.collectAnnotationsFromProcesses processes

    /// All Annotations from all sources whose name matches the given string
    member this.AnnotationsByName(name: string) : ResizeArray<Annotation> =
        this.AllAnnotations() |> Seq.filter (fun pv -> pv.Name = name) |> ResizeArray

    /// All FormalParameters defined on recipes executed by processes in this path
    member _.RecipeParameters() : ResizeArray<FormalParameter> =
        let acc  = ResizeArray<FormalParameter>()
        let seen = HashSet<string>()
        for proc in processes do
            match proc.ExecutesRecipe with
            | Some (proto: Recipe) ->
                for fp: FormalParameter in proto.Parameters do
                    if seen.Add(fp.Name) then acc.Add(fp)
            | None -> ()
        acc

module PathTraversal =
    let annotationKey (pv: Annotation) =
        pv.Name + "|" + (pv.Value |> Option.defaultValue "") + "|" + (pv.NameTAN |> Option.defaultValue "")

    let addAnnotation (result: ResizeArray<Annotation>) (seen: HashSet<string>) (pv: Annotation) =
        if seen.Add(annotationKey pv) then result.Add(pv)

    let addAnnotationsFromNode (result: ResizeArray<Annotation>) (seen: HashSet<string>) (node: IONode) =
        match node with
        | SampleNode m -> for pv: Annotation in m.AdditionalProperty do addAnnotation result seen pv
        | DataNode d -> for pv: Annotation in d.AdditionalProperty do addAnnotation result seen pv

    let addProcessAnnotations (result: ResizeArray<Annotation>) (seen: HashSet<string>) (proc: Process) =
        for pv: Annotation in proc.ParameterValue do addAnnotation result seen pv
        match proc.ExecutesRecipe with
        | Some (proto: Recipe) ->
            for pv: Annotation in proto.Components do addAnnotation result seen pv
        | None -> ()

    let addAnnotationsFromProcess (result: ResizeArray<Annotation>) (seen: HashSet<string>) (proc: Process) =
        addProcessAnnotations result seen proc
        proc.Input |> Option.iter (addAnnotationsFromNode result seen)
        proc.Output |> Option.iter (addAnnotationsFromNode result seen)

    let collectAnnotationsFromProcesses (processes: seq<Process>) : ResizeArray<Annotation> =
        let result = ResizeArray<Annotation>()
        let seen = HashSet<string>()
        for proc in processes do
            addAnnotationsFromProcess result seen proc
        result

    let collectAnnotationsFromProcessesWithRecipeName (recipeName: string option) (processes: seq<Process>) : ResizeArray<Annotation> =
        let includeProcess (proc: Process) =
            match recipeName with
            | Some pn ->
                match proc.ExecutesRecipe with
                | Some (proto: Recipe) -> proto.Name = Some pn
                | None -> false
            | None -> true

        processes
        |> Seq.filter includeProcess
        |> collectAnnotationsFromProcesses

    let processMatchesRecipeName (recipeName: string option) (proc: Process) =
        match recipeName with
        | Some pn ->
            match proc.ExecutesRecipe with
            | Some (proto: Recipe) -> proto.Name = Some pn
            | None -> false
        | None -> true

    let inScope (processes: ResizeArray<Process>) (p: Process) =
        processes |> Seq.exists (fun q -> obj.ReferenceEquals(q, p))

    let containsProcessRef (processes: seq<Process>) (proc: Process) =
        processes |> Seq.exists (fun candidate -> obj.ReferenceEquals(candidate, proc))

    let inOptionalScope (scope: ResizeArray<Process> option) (p: Process) =
        scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> obj.ReferenceEquals(q, p)))

    let distinctProcesses (processes: seq<Process>) =
        let seen = ResizeArray<Process>()
        processes
        |> Seq.filter (fun proc ->
            if containsProcessRef seen proc then false
            else
                seen.Add(proc)
                true)

    let dataRelatedForTraversal (tryGetProvider: string -> IFragmentSelectorProvider option) (a: Data) (b: Data) =
        match FragmentSelectorResolution.relateDataWith tryGetProvider a b with
        | Exact -> true // to-do: These should be an OR match case when https://github.com/fable-compiler/Fable/issues/4649 is fixed
        | Contains -> true
        | Disjoint ->
            match FragmentSelectorResolution.relateDataWith tryGetProvider b a with
            | Exact -> true
            | Contains -> true
            | Disjoint -> false
            | Unknown -> false
        | Unknown ->
            match FragmentSelectorResolution.relateDataWith tryGetProvider b a with
            | Exact -> true
            | Contains -> true
            | Disjoint -> false
            | Unknown -> false

    let nodeRelatedForTraversal (tryGetProvider: string -> IFragmentSelectorProvider option) (a: IONode) (b: IONode) =
        match a, b with
        | SampleNode ma, SampleNode mb -> ma = mb
        | DataNode da, DataNode db -> dataRelatedForTraversal tryGetProvider da db
        | _ -> false

    let processesFromExactBackEdges (node: IONode) =
        let acc = ResizeArray<Process>()
        acc.AddRange(node.GetInputOf())
        acc.AddRange(node.GetOutputOf())
        acc

    let processUniverse (scope: ResizeArray<Process> option) (node: IONode) =
        match scope with
        | Some s -> s
        | None -> processesFromExactBackEdges node

    let providerLookupFromProcesses (processes: seq<Process>) =
        let dataset: Dataset option =
            processes
            |> Seq.tryPick (fun proc -> proc.ProcessOf)

        fun selectorFormat ->
            dataset
            |> Option.bind (fun ds -> ds.TryGetFragmentSelectorProvider selectorFormat)

    let distinctProcessEdges (edges: seq<Process * IONode>) =
        let acc = ResizeArray<Process * IONode>()
        for proc, matchedNode in edges do
            let alreadySeen =
                acc
                |> Seq.exists (fun (candidate, node) ->
                    obj.ReferenceEquals(candidate, proc) && node.Key() = matchedNode.Key())
            if not alreadySeen then acc.Add(proc, matchedNode)
        acc

    let relatedInputEdges (scope: ResizeArray<Process> option) (node: IONode) =
        let universe = processUniverse scope node
        let tryGetProvider = providerLookupFromProcesses universe
        universe
        |> Seq.choose (fun proc ->
            proc.Input
            |> Option.filter (nodeRelatedForTraversal tryGetProvider node)
            |> Option.map (fun matchedNode -> proc, matchedNode))
        |> distinctProcessEdges

    let relatedOutputEdges (scope: ResizeArray<Process> option) (node: IONode) =
        let universe = processUniverse scope node
        let tryGetProvider = providerLookupFromProcesses universe
        universe
        |> Seq.choose (fun proc ->
            proc.Output
            |> Option.filter (nodeRelatedForTraversal tryGetProvider node)
            |> Option.map (fun matchedNode -> proc, matchedNode))
        |> distinctProcessEdges

    let relatedNodeProcesses (scope: ResizeArray<Process> option) (node: IONode) =
        Seq.append (relatedInputEdges scope node) (relatedOutputEdges scope node)
        |> Seq.map fst
        |> distinctProcesses
        |> ResizeArray

    let processesForNode (processes: ResizeArray<Process>) (node: IONode) : ResizeArray<Process> =
        relatedNodeProcesses (Some processes) node

    let collectUpstreamAnnotations
        (recipeName: string option)
        (scope: ResizeArray<Process> option)
        (start: IONode)
        : ResizeArray<Annotation> =

        let result = ResizeArray<Annotation>()
        let seenPV = HashSet<string>()
        let visitedEdges = ResizeArray<Process * string>()

        let rec walk (node: IONode) =
            for proc, matchedNode in relatedOutputEdges scope node do
                if inOptionalScope scope proc then
                    let nodeKey = matchedNode.Key()
                    let visited = visitedEdges |> Seq.exists (fun (candidate, key) -> obj.ReferenceEquals(candidate, proc) && key = nodeKey)
                    if not visited then
                        visitedEdges.Add(proc, nodeKey)
                        let includeProcess = processMatchesRecipeName recipeName proc
                        match proc.Input with
                        | Some input ->
                            walk input
                            if includeProcess then addAnnotationsFromNode result seenPV input
                        | None -> ()
                        if includeProcess then
                            addProcessAnnotations result seenPV proc
                            addAnnotationsFromNode result seenPV matchedNode

        walk start
        result

    let collectDownstreamAnnotations
        (recipeName: string option)
        (scope: ResizeArray<Process> option)
        (start: IONode)
        : ResizeArray<Annotation> =

        let result = ResizeArray<Annotation>()
        let seenPV = HashSet<string>()
        let visitedEdges = ResizeArray<Process * string>()

        let rec walk (node: IONode) =
            for proc, matchedNode in relatedInputEdges scope node do
                if inOptionalScope scope proc then
                    let nodeKey = matchedNode.Key()
                    let visited = visitedEdges |> Seq.exists (fun (candidate, key) -> obj.ReferenceEquals(candidate, proc) && key = nodeKey)
                    if not visited then
                        visitedEdges.Add(proc, nodeKey)
                        let includeProcess = processMatchesRecipeName recipeName proc
                        if includeProcess then
                            addAnnotationsFromNode result seenPV matchedNode
                            addProcessAnnotations result seenPV proc
                        match proc.Output with
                        | Some output ->
                            if includeProcess then addAnnotationsFromNode result seenPV output
                            walk output
                        | None -> ()

        walk start
        result

    let rec walkUpstream (processes: ResizeArray<Process>) (proc: Process) (visited: ResizeArray<Process>) : ResizeArray<ResizeArray<Process>> =
        if containsProcessRef visited proc then
            ResizeArray([ ResizeArray() ])
        else
            visited.Add(proc)
            let preds =
                proc.Input
                |> Option.toList
                |> Seq.collect (fun node -> relatedOutputEdges (Some processes) node |> Seq.map fst)
                |> Seq.filter (inScope processes)
                |> distinctProcesses
                |> ResizeArray
            if preds.Count = 0 then
                ResizeArray([ ResizeArray() ])
            else
                let results = ResizeArray()
                for pred in preds do
                    for chain in walkUpstream processes pred (ResizeArray(visited)) do
                        let ext = ResizeArray(chain)
                        ext.Add(pred)
                        results.Add(ext)
                results

    let rec walkDownstream (processes: ResizeArray<Process>) (proc: Process) (visited: ResizeArray<Process>) : ResizeArray<ResizeArray<Process>> =
        if containsProcessRef visited proc then
            ResizeArray([ ResizeArray() ])
        else
            visited.Add(proc)
            let succs =
                proc.Output
                |> Option.toList
                |> Seq.collect (fun node -> relatedInputEdges (Some processes) node |> Seq.map fst)
                |> Seq.filter (inScope processes)
                |> distinctProcesses
                |> ResizeArray
            if succs.Count = 0 then
                ResizeArray([ ResizeArray() ])
            else
                let results = ResizeArray()
                for succ in succs do
                    for chain in walkDownstream processes succ (ResizeArray(visited)) do
                        let ext = ResizeArray()
                        ext.Add(succ)
                        ext.AddRange(chain)
                        results.Add(ext)
                results

    let extendToMaximalPaths (processes: ResizeArray<Process>) (proc: Process) : ResizeArray<Path> =
        let upstream   = walkUpstream processes proc (ResizeArray())
        let downstream = walkDownstream processes proc (ResizeArray())
        let results = ResizeArray()
        for pre in upstream do
            for post in downstream do
                let chain = ResizeArray()
                chain.AddRange(pre)
                chain.Add(proc)
                chain.AddRange(post)
                results.Add(Path(chain))
        results

    let pathsThrough (processes: ResizeArray<Process>) (node: IONode) : ResizeArray<Path> =
        let results = ResizeArray()
        for seed in processesForNode processes node do
            results.AddRange(extendToMaximalPaths processes seed)
        results

// ─────────────────────────────────────────────────────────────────────────────
// IONode discriminated union (forward-declared via namespace rec)
// ─────────────────────────────────────────────────────────────────────────────

/// An IONode is either a Sample or a Data node.
[<AttachMembers>]
type IONode =
    | SampleNode of Sample
    | DataNode of Data

    member this.TrySample() =
        match this with
        | SampleNode m -> Some m
        | _ -> None

    member this.TryData() =
        match this with
        | DataNode d -> Some d
        | _ -> None

    member this.AsSample() =
        match this with
        | SampleNode m -> m
        | other -> failwithf "Expected Sample node but got %A" other

    member this.AsData() =
        match this with
        | DataNode d -> d
        | other -> failwithf "Expected Data node but got %A" other

    member this.EqualTo(other: IONode) =
        match this, other with
        | SampleNode a, SampleNode b -> a = b
        | DataNode a,     DataNode b     -> a = b
        | _ -> false

    /// Stable string key used for identity checks and HashSet deduplication.
    member this.Key() =
        match this with
        | SampleNode m -> "M:" + m.Name
        | DataNode d     -> "D:" + d.Path + (d.Selector |> Option.defaultValue "")

    /// Processes for which this node is an input (forward back-edges).
    member this.GetInputOf() : HashSet<Process> =
        match this with
        | SampleNode m -> m.InputOf
        | DataNode d     -> d.InputOf

    /// Processes for which this node is an output (backward back-edges).
    member this.GetOutputOf() : HashSet<Process> =
        match this with
        | SampleNode m -> m.OutputOf
        | DataNode d     -> d.OutputOf

    // ── Graph traversal ───────────────────────────────────────────────────────

    /// All processes reachable from this node by BFS through both upstream
    /// and downstream edges. Optional `scope` restricts traversal to the given
    /// process list (e.g. a single Dataset).
    member this.AllConnectedProcesses(?scope: ResizeArray<Process>) : ResizeArray<Process> =
        let inScope (p: Process) =
            scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> obj.ReferenceEquals(q, p)))
        let seenN  = HashSet<string>()
        let seenP  = HashSet<Process>(refEqProcess)
        let result = ResizeArray<Process>()
        seenN.Add(this.Key()) |> ignore
        let mutable frontier = ResizeArray<IONode>([| this |])
        while frontier.Count > 0 do
            let next = ResizeArray<IONode>()
            for node in frontier do
                for p: Process in PathTraversal.relatedNodeProcesses scope node do
                    if inScope p && seenP.Add(p) then
                        result.Add(p)
                        p.Input |> Option.iter (fun n -> if seenN.Add(n.Key()) then next.Add(n))
                        p.Output |> Option.iter (fun n -> if seenN.Add(n.Key()) then next.Add(n))
            frontier <- next
        result

    /// All in-scope processes in which this node appears as an input or output.
    member this.Processes(?scope: ResizeArray<Process>) : ResizeArray<Process> =
        let scope =
            match scope with
            | Some s -> s
            | None   -> this.AllConnectedProcesses()
        PathTraversal.processesForNode scope this

    /// All maximal Paths that pass through this node within the given process scope.
    member this.PathsThrough(?scope: ResizeArray<Process>) : ResizeArray<Path> =
        let scope =
            match scope with
            | Some s -> s
            | None   -> this.AllConnectedProcesses()
        PathTraversal.pathsThrough scope this

    /// All IONodes connected to this node through the process graph
    /// (union of all upstream and downstream neighbours), excluding this node itself.
    member this.AllConnectedNodes(?scope: ResizeArray<Process>) : ResizeArray<IONode> =
        let inScope (p: Process) =
            scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> obj.ReferenceEquals(q, p)))
        let seenN  = HashSet<string>()
        seenN.Add(this.Key()) |> ignore
        let result = ResizeArray<IONode>()
        let mutable frontier = ResizeArray<IONode>([| this |])
        while frontier.Count > 0 do
            let next = ResizeArray<IONode>()
            for node in frontier do
                for p: Process in PathTraversal.relatedNodeProcesses scope node do
                    if inScope p then
                        for n in [p.Input; p.Output] |> List.choose id do
                            if seenN.Add(n.Key()) then
                                result.Add(n)
                                next.Add(n)
            frontier <- next
        result

    /// All Annotations from all sources (parameters, input/output node properties, recipe components)
    /// connected to this node through the graph.
    member this.AllAnnotations(?scope: ResizeArray<Process>) : ResizeArray<Annotation> =
        let result = ResizeArray<Annotation>()
        let seen = HashSet<string>()
        for pv in PathTraversal.collectUpstreamAnnotations None scope this do
            PathTraversal.addAnnotation result seen pv
        for pv in PathTraversal.collectDownstreamAnnotations None scope this do
            PathTraversal.addAnnotation result seen pv
        result

    /// All FormalParameters from recipes executed in processes connected to this node.
    member this.RecipeParameters(?scope: ResizeArray<Process>) : ResizeArray<FormalParameter> =
        let seen   = HashSet<string>()
        let result = ResizeArray<FormalParameter>()
        for p: Process in this.AllConnectedProcesses(?scope = scope) do
            match p.ExecutesRecipe with
            | Some (proto: Recipe) ->
                for fp: FormalParameter in proto.Parameters do
                    if seen.Add(fp.Name) then result.Add(fp)
            | None -> ()
        result

    /// All Sample nodes connected to this node through the graph.
    member this.ConnectedSamples(?scope: ResizeArray<Process>) : ResizeArray<Sample> =
        let result = ResizeArray<Sample>()
        for n in this.AllConnectedNodes(?scope = scope) do
            match n with
            | SampleNode m -> result.Add(m)
            | _ -> ()
        result

    /// All Data nodes connected to this node through the graph.
    member this.ConnectedData(?scope: ResizeArray<Process>) : ResizeArray<Data> =
        let result = ResizeArray<Data>()
        for n in this.AllConnectedNodes(?scope = scope) do
            match n with
            | DataNode d -> result.Add(d)
            | _ -> ()
        result

    // ── Directional traversal ─────────────────────────────────────────────────

    /// True if no in-scope process produces this node as output (no predecessors in scope).
    member this.IsRootNode(?scope: ResizeArray<Process>) : bool =
        match scope with
        | Some s  ->

            let inScope (p: Process) =
                scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> obj.ReferenceEquals(q, p)))
            PathTraversal.relatedOutputEdges scope this |> Seq.exists (fun (p, _) -> inScope p) |> not
        | None ->
            this.GetOutputOf().Count = 0

    /// True if no in-scope process consumes this node as input (no successors in scope).
    member this.IsFinalNode(?scope: ResizeArray<Process>) : bool =
        match scope with
        | Some s  ->
            let inScope (p: Process) =
                scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> obj.ReferenceEquals(q, p)))
            PathTraversal.relatedInputEdges scope this |> Seq.exists (fun (p, _) -> inScope p) |> not
        | None ->
            this.GetInputOf().Count = 0

    /// All processes reachable by walking upstream (OutputOf edges → Inputs).
    member this.UpstreamProcesses(?scope: ResizeArray<Process>) : ResizeArray<Process> =
        let inScope (p: Process) =
            scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> obj.ReferenceEquals(q, p)))
        let seenN = HashSet<string>()
        let seenP = HashSet<Process>(refEqProcess)
        let result = ResizeArray<Process>()
        seenN.Add(this.Key()) |> ignore
        let mutable frontier = ResizeArray<IONode>([| this |])
        while frontier.Count > 0 do
            let next = ResizeArray<IONode>()
            for node in frontier do
                for p, matchedNode in PathTraversal.relatedOutputEdges scope node do
                    if inScope p && seenP.Add(p) then
                        result.Add(p)
                        match p.Input with
                        | Some n ->
                            if seenN.Add(n.Key()) then next.Add(n)
                        | None -> ()
            frontier <- next
        result

    /// All processes reachable by walking downstream (InputOf edges → Outputs).
    member this.DownstreamProcesses(?scope: ResizeArray<Process>) : ResizeArray<Process> =
        let inScope (p: Process) =
            scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> obj.ReferenceEquals(q, p)))
        let seenN = HashSet<string>()
        let seenP = HashSet<Process>(refEqProcess)
        let result = ResizeArray<Process>()
        seenN.Add(this.Key()) |> ignore
        let mutable frontier = ResizeArray<IONode>([| this |])
        while frontier.Count > 0 do
            let next = ResizeArray<IONode>()
            for node in frontier do
                for p, matchedNode in PathTraversal.relatedInputEdges scope node do
                    if inScope p && seenP.Add(p) then
                        result.Add(p)
                        match p.Output with
                        | Some n ->
                            if seenN.Add(n.Key()) then next.Add(n)
                        | None -> ()
            frontier <- next
        result

    /// All IONodes reachable by following each producing process's direct input.
    member this.UpstreamNodes(?scope: ResizeArray<Process>) : ResizeArray<IONode> =
        let inScope (p: Process) =
            scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> obj.ReferenceEquals(q, p)))
        let seenN = HashSet<string>()
        seenN.Add(this.Key()) |> ignore
        let result = ResizeArray<IONode>()
        let mutable frontier = ResizeArray<IONode>([| this |])
        while frontier.Count > 0 do
            let next = ResizeArray<IONode>()
            for node in frontier do
                for p, matchedNode in PathTraversal.relatedOutputEdges scope node do
                    if inScope p then
                        match p.Input with
                        | Some n ->
                            if seenN.Add(n.Key()) then
                                result.Add(n)
                                next.Add(n)
                        | None -> ()
            frontier <- next
        result

    /// All IONodes reachable by following each consuming process's direct output.
    member this.DownstreamNodes(?scope: ResizeArray<Process>) : ResizeArray<IONode> =
        let inScope (p: Process) =
            scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> obj.ReferenceEquals(q, p)))
        let seenN = HashSet<string>()
        seenN.Add(this.Key()) |> ignore
        let result = ResizeArray<IONode>()
        let mutable frontier = ResizeArray<IONode>([| this |])
        while frontier.Count > 0 do
            let next = ResizeArray<IONode>()
            for node in frontier do
                for p, matchedNode in PathTraversal.relatedInputEdges scope node do
                    if inScope p then
                        match p.Output with
                        | Some n ->
                            if seenN.Add(n.Key()) then
                                result.Add(n)
                                next.Add(n)
                        | None -> ()
            frontier <- next
        result

    /// Upstream nodes that have no predecessors within scope (terminal sources).
    member this.RootNodes(?scope: ResizeArray<Process>) : ResizeArray<IONode> =
        this.UpstreamNodes(?scope = scope)
        |> Seq.filter (fun n -> n.IsRootNode(?scope = scope))
        |> ResizeArray

    /// Downstream nodes that have no successors within scope (terminal sinks).
    member this.FinalNodes(?scope: ResizeArray<Process>) : ResizeArray<IONode> =
        this.DownstreamNodes(?scope = scope)
        |> Seq.filter (fun n -> n.IsFinalNode(?scope = scope))
        |> ResizeArray

    /// All Sample nodes reachable upstream from this node.
    member this.UpstreamSamples(?scope: ResizeArray<Process>) : ResizeArray<Sample> =
        let result = ResizeArray<Sample>()
        for n in this.UpstreamNodes(?scope = scope) do
            match n with
            | SampleNode m -> result.Add(m)
            | _ -> ()
        result

    /// All Sample nodes reachable downstream from this node.
    member this.DownstreamSamples(?scope: ResizeArray<Process>) : ResizeArray<Sample> =
        let result = ResizeArray<Sample>()
        for n in this.DownstreamNodes(?scope = scope) do
            match n with
            | SampleNode m -> result.Add(m)
            | _ -> ()
        result

    /// All Data nodes reachable upstream from this node.
    member this.UpstreamData(?scope: ResizeArray<Process>) : ResizeArray<Data> =
        let result = ResizeArray<Data>()
        for n in this.UpstreamNodes(?scope = scope) do
            match n with
            | DataNode d -> result.Add(d)
            | _ -> ()
        result

    /// All Data nodes reachable downstream from this node.
    member this.DownstreamData(?scope: ResizeArray<Process>) : ResizeArray<Data> =
        let result = ResizeArray<Data>()
        for n in this.DownstreamNodes(?scope = scope) do
            match n with
            | DataNode d -> result.Add(d)
            | _ -> ()
        result

    /// Annotations from all sources in processes upstream of this node.
    /// Optional recipeName restricts to processes whose recipe name matches.
    member this.UpstreamAnnotations(?recipeName: string, ?scope: ResizeArray<Process>) : ResizeArray<Annotation> =
        PathTraversal.collectUpstreamAnnotations recipeName scope this

    /// Annotations from all sources in processes downstream of this node.
    /// Optional recipeName restricts to processes whose recipe name matches.
    member this.DownstreamAnnotations(?recipeName: string, ?scope: ResizeArray<Process>) : ResizeArray<Annotation> =
        PathTraversal.collectDownstreamAnnotations recipeName scope this

// ─────────────────────────────────────────────────────────────────────────────
// Sample
// ─────────────────────────────────────────────────────────────────────────────

/// Input or output biological, chemical, or digital sample in the process graph.
/// bioschemas.org/Sample
and [<AttachMembers>] Sample(name: string, ?additionalType: string, ?additionalProperty: seq<Annotation>) as this =

    inherit DynamicObj()

    let mutable _name: string = name
    let mutable _additionalType: string option = additionalType
    let _additionalProperty: ResizeArray<Annotation> = ResizeArray()
    let _inputOf:  HashSet<Process> = HashSet(refEqProcess)
    let _outputOf: HashSet<Process> = HashSet(refEqProcess)

    do
        additionalProperty |> Option.iter (fun pvs -> for pv in pvs do this.AddAdditionalProperty(pv))

    member _.Name
        with get() = _name
        and set v = _name <- v

    /// Decoration discriminator (e.g. "Sample", "Source")
    member _.AdditionalType
        with get() = _additionalType
        and set v = _additionalType <- v

    member _.AdditionalProperty = _additionalProperty

    /// Processes for which this sample is an input (back-edge)
    member _.InputOf: HashSet<Process> = _inputOf

    /// Processes for which this sample is an output (back-edge)
    member _.OutputOf: HashSet<Process> = _outputOf

    member this.AddAdditionalProperty(pv: Annotation) =
        if not (_additionalProperty |> Seq.exists (fun x -> x = pv)) then
            _additionalProperty.Add(pv)

    member _.RemoveAdditionalProperty(pv: Annotation) =
        _additionalProperty.Remove(pv) |> ignore

    // ── Query ─────────────────────────────────────────────────────────────────

    /// All IONodes connected to this sample through the graph.
    member this.AllConnectedNodes(?scope: ResizeArray<Process>) =
        (SampleNode this).AllConnectedNodes(?scope = scope)

    /// All processes connected to this sample through the graph.
    member this.AllConnectedProcesses(?scope: ResizeArray<Process>) =
        (SampleNode this).AllConnectedProcesses(?scope = scope)

    /// All in-scope processes in which this sample appears as an input or output.
    member this.Processes(?scope: ResizeArray<Process>) =
        (SampleNode this).Processes(?scope = scope)

    /// All maximal Paths that pass through this sample.
    member this.PathsThrough(?scope: ResizeArray<Process>) =
        (SampleNode this).PathsThrough(?scope = scope)

    /// All Annotations from all sources connected to this sample through the graph.
    member this.AllAnnotations(?scope: ResizeArray<Process>) =
        (SampleNode this).AllAnnotations(?scope = scope)

    /// All FormalParameters from recipes connected to this sample.
    member this.RecipeParameters(?scope: ResizeArray<Process>) =
        (SampleNode this).RecipeParameters(?scope = scope)

    /// All Sample nodes connected to this sample through the graph.
    member this.ConnectedSamples(?scope: ResizeArray<Process>) =
        (SampleNode this).ConnectedSamples(?scope = scope)

    /// All Data nodes connected to this sample through the graph.
    member this.ConnectedData(?scope: ResizeArray<Process>) =
        (SampleNode this).ConnectedData(?scope = scope)

    // ── Directional traversal ─────────────────────────────────────────────────

    member this.IsRootNode(?scope: ResizeArray<Process>) =
        (SampleNode this).IsRootNode(?scope = scope)

    member this.IsFinalNode(?scope: ResizeArray<Process>) =
        (SampleNode this).IsFinalNode(?scope = scope)

    member this.UpstreamProcesses(?scope: ResizeArray<Process>) =
        (SampleNode this).UpstreamProcesses(?scope = scope)

    member this.DownstreamProcesses(?scope: ResizeArray<Process>) =
        (SampleNode this).DownstreamProcesses(?scope = scope)

    member this.UpstreamNodes(?scope: ResizeArray<Process>) =
        (SampleNode this).UpstreamNodes(?scope = scope)

    member this.DownstreamNodes(?scope: ResizeArray<Process>) =
        (SampleNode this).DownstreamNodes(?scope = scope)

    member this.RootNodes(?scope: ResizeArray<Process>) =
        (SampleNode this).RootNodes(?scope = scope)

    member this.FinalNodes(?scope: ResizeArray<Process>) =
        (SampleNode this).FinalNodes(?scope = scope)

    member this.UpstreamSamples(?scope: ResizeArray<Process>) =
        (SampleNode this).UpstreamSamples(?scope = scope)

    member this.DownstreamSamples(?scope: ResizeArray<Process>) =
        (SampleNode this).DownstreamSamples(?scope = scope)

    member this.UpstreamData(?scope: ResizeArray<Process>) =
        (SampleNode this).UpstreamData(?scope = scope)

    member this.DownstreamData(?scope: ResizeArray<Process>) =
        (SampleNode this).DownstreamData(?scope = scope)

    member this.UpstreamAnnotations(?recipeName: string, ?scope: ResizeArray<Process>) =
        (SampleNode this).UpstreamAnnotations(?recipeName = recipeName, ?scope = scope)

    member this.DownstreamAnnotations(?recipeName: string, ?scope: ResizeArray<Process>) =
        (SampleNode this).DownstreamAnnotations(?recipeName = recipeName, ?scope = scope)

    /// Two Samples with the same name are identical across datasets.
    override this.Equals(obj) =
        match obj with
        | :? Sample as other -> this.Name = other.Name
        | _ -> false

    override this.GetHashCode() = hash this.Name

    #if FABLE_COMPILER_PYTHON
    // Python calls these dunder methods for native `hash(x)` and `x == y`.
    member this.``__hash__``() =
        Fable.Core.PyInterop.emitPyExpr (this) "int($0.GetHashCode())"
    #endif

// ─────────────────────────────────────────────────────────────────────────────
// Data
// ─────────────────────────────────────────────────────────────────────────────

/// Data file or selected fragment produced or consumed by processes.
/// schema.org/MediaObject or File
and [<AttachMembers>] Data(path: string, ?selector: string, ?selectorFormat: string, ?encodingFormat: string, ?additionalType: string, ?hasPart: seq<Data>, ?additionalProperty: seq<Annotation>) as this =

    inherit DynamicObj()

    let mutable _path: string = path
    let mutable _selector: string option = selector
    let mutable _selectorFormat: string option = selectorFormat
    let mutable _encodingFormat: string option = encodingFormat
    let mutable _additionalType: string option = additionalType
    let _hasPart: ResizeArray<Data> = ResizeArray()
    let _additionalProperty: ResizeArray<Annotation> = ResizeArray()
    let _inputOf:  HashSet<Process> = HashSet(refEqProcess)
    let _outputOf: HashSet<Process> = HashSet(refEqProcess)

    do
        hasPart |> Option.iter (fun data -> for d in data do this.AddPart(d))
        additionalProperty |> Option.iter (fun pvs -> for pv in pvs do this.AddAdditionalProperty(pv))

    member _.Name
        with get() = 
            match _selector with
            | Some sel when sel <> "" -> _path + "#" + sel
            | _ -> _path
        

    member _.Path
        with get() = _path
        and set v = _path <- v

    /// Fragment selector
    member _.Selector
        with get() = _selector
        and set v = _selector <- v

    /// Formal description of the selector syntax (e.g. RFC 7111)
    member _.SelectorFormat
        with get() = _selectorFormat
        and set v = _selectorFormat <- v

    /// MIME type
    member _.EncodingFormat
        with get() = _encodingFormat
        and set v = _encodingFormat <- v

    /// Decoration discriminator (e.g. "Raw Data")
    member _.AdditionalType
        with get() = _additionalType
        and set v = _additionalType <- v

    member _.HasPart = _hasPart

    member _.AdditionalProperty = _additionalProperty

    /// Processes for which this data node is an input (back-edge)
    member _.InputOf: HashSet<Process> = _inputOf

    /// Processes for which this data node is an output (back-edge)
    member _.OutputOf: HashSet<Process> = _outputOf

    member this.AddAdditionalProperty(pv: Annotation) =
        if not (_additionalProperty |> Seq.exists (fun x -> x = pv)) then
            _additionalProperty.Add(pv)

    member _.RemoveAdditionalProperty(pv: Annotation) =
        _additionalProperty.Remove(pv) |> ignore

    member this.AddPart(data: Data) =
        if not (_hasPart |> Seq.exists (fun x -> x = data)) then
            _hasPart.Add(data)

    member _.RemovePart(data: Data) =
        _hasPart.Remove(data) |> ignore

    member _.TryGetPart(path: string) =
        _hasPart |> Seq.tryFind (fun d -> d.Path = path)

    // ── Query ─────────────────────────────────────────────────────────────────

    /// All IONodes connected to this data node through the graph.
    member this.AllConnectedNodes(?scope: ResizeArray<Process>) =
        (DataNode this).AllConnectedNodes(?scope = scope)

    /// All processes connected to this data node through the graph.
    member this.AllConnectedProcesses(?scope: ResizeArray<Process>) =
        (DataNode this).AllConnectedProcesses(?scope = scope)

    /// All in-scope processes in which this data node appears as an input or output.
    member this.Processes(?scope: ResizeArray<Process>) =
        (DataNode this).Processes(?scope = scope)

    /// All maximal Paths that pass through this data node.
    member this.PathsThrough(?scope: ResizeArray<Process>) =
        (DataNode this).PathsThrough(?scope = scope)

    /// All Annotations from all sources connected to this data node through the graph.
    member this.AllAnnotations(?scope: ResizeArray<Process>) =
        (DataNode this).AllAnnotations(?scope = scope)

    /// All FormalParameters from recipes connected to this data node.
    member this.RecipeParameters(?scope: ResizeArray<Process>) =
        (DataNode this).RecipeParameters(?scope = scope)

    /// All Sample nodes connected to this data node through the graph.
    member this.ConnectedSamples(?scope: ResizeArray<Process>) =
        (DataNode this).ConnectedSamples(?scope = scope)

    /// All Data nodes connected to this data node through the graph.
    member this.ConnectedData(?scope: ResizeArray<Process>) =
        (DataNode this).ConnectedData(?scope = scope)

    // ── Directional traversal ─────────────────────────────────────────────────

    member this.IsRootNode(?scope: ResizeArray<Process>) =
        (DataNode this).IsRootNode(?scope = scope)

    member this.IsFinalNode(?scope: ResizeArray<Process>) =
        (DataNode this).IsFinalNode(?scope = scope)

    member this.UpstreamProcesses(?scope: ResizeArray<Process>) =
        (DataNode this).UpstreamProcesses(?scope = scope)

    member this.DownstreamProcesses(?scope: ResizeArray<Process>) =
        (DataNode this).DownstreamProcesses(?scope = scope)

    member this.UpstreamNodes(?scope: ResizeArray<Process>) =
        (DataNode this).UpstreamNodes(?scope = scope)

    member this.DownstreamNodes(?scope: ResizeArray<Process>) =
        (DataNode this).DownstreamNodes(?scope = scope)

    member this.RootNodes(?scope: ResizeArray<Process>) =
        (DataNode this).RootNodes(?scope = scope)

    member this.FinalNodes(?scope: ResizeArray<Process>) =
        (DataNode this).FinalNodes(?scope = scope)

    member this.UpstreamSamples(?scope: ResizeArray<Process>) =
        (DataNode this).UpstreamSamples(?scope = scope)

    member this.DownstreamSamples(?scope: ResizeArray<Process>) =
        (DataNode this).DownstreamSamples(?scope = scope)

    member this.UpstreamData(?scope: ResizeArray<Process>) =
        (DataNode this).UpstreamData(?scope = scope)

    member this.DownstreamData(?scope: ResizeArray<Process>) =
        (DataNode this).DownstreamData(?scope = scope)

    member this.UpstreamAnnotations(?recipeName: string, ?scope: ResizeArray<Process>) =
        (DataNode this).UpstreamAnnotations(?recipeName = recipeName, ?scope = scope)

    member this.DownstreamAnnotations(?recipeName: string, ?scope: ResizeArray<Process>) =
        (DataNode this).DownstreamAnnotations(?recipeName = recipeName, ?scope = scope)

    /// Two Data nodes with the same path and selector are identical.
    override this.Equals(obj) =
        match obj with
        | :? Data as other -> this.Path = other.Path && this.Selector = other.Selector
        | _ -> false

    override this.GetHashCode() = hash (this.Path, this.Selector)

    #if FABLE_COMPILER_PYTHON
    // Python calls these dunder methods for native `hash(x)` and `x == y`.
    member this.``__hash__``() =
        Fable.Core.PyInterop.emitPyExpr (this) "int($0.GetHashCode())"
    #endif

/// Datamap descriptor for a data object or selected data fragment.
and [<AttachMembers>] DataContext(data: Data, ?explication: DefinedTerm, ?objectType: DefinedTerm, ?unit: DefinedTerm, ?label: string, ?description: string, ?generatedBy: string) =

    inherit DynamicObj()

    let mutable _data: Data = data
    let mutable _explication: DefinedTerm option = explication
    let mutable _objectType: DefinedTerm option = objectType
    let mutable _unit: DefinedTerm option = unit
    let mutable _label: string option = label
    let mutable _description: string option = description
    let mutable _generatedBy: string option = generatedBy

    member _.Data
        with get() = _data
        and set v = _data <- v

    member _.Explication
        with get() = _explication
        and set v = _explication <- v

    member _.ObjectType
        with get() = _objectType
        and set v = _objectType <- v

    member _.Unit
        with get() = _unit
        and set v = _unit <- v

    member _.Label
        with get() = _label
        and set v = _label <- v

    member _.Description
        with get() = _description
        and set v = _description <- v

    member _.GeneratedBy
        with get() = _generatedBy
        and set v = _generatedBy <- v

    member this.ExplicationEquals(term: DefinedTerm) =
        this.Explication
        |> Option.exists (fun explication -> explication.SemanticallyEquals(term))

    member this.ObjectTypeEquals(term: DefinedTerm) =
        this.ObjectType
        |> Option.exists (fun objectType -> objectType.SemanticallyEquals(term))

    member this.UnitEquals(term: DefinedTerm) =
        this.Unit
        |> Option.exists (fun unitTerm -> unitTerm.SemanticallyEquals(term))

    override this.Equals(obj) =
        match obj with
        | :? DataContext as other ->
            this.Data = other.Data &&
            this.Explication = other.Explication &&
            this.ObjectType = other.ObjectType
        | _ -> false

    override this.GetHashCode() =
        hash (this.Data, this.Explication, this.ObjectType)
// ─────────────────────────────────────────────────────────────────────────────
// Recipe
// ─────────────────────────────────────────────────────────────────────────────

/// Description of a planned procedure.
/// bioschemas.org/LabProtocol
and [<AttachMembers>] Recipe(?name: string, ?description: string, ?version: string, ?url: string, ?intendedUse: DefinedTerm, ?additionalType: string, ?parameters: seq<FormalParameter>, ?components: seq<Annotation>, ?additionalProperty: seq<Annotation>) as this =

    inherit DynamicObj()

    let mutable _name: string option = name
    let mutable _description: string option = description
    let mutable _version: string option = version
    let mutable _url: string option = url
    let mutable _intendedUse: DefinedTerm option = intendedUse
    let mutable _additionalType: string option = additionalType
    let _parameters: ResizeArray<FormalParameter> = ResizeArray()
    let _components: ResizeArray<Annotation> = ResizeArray()
    let _additionalProperty: ResizeArray<Annotation> = ResizeArray()

    do
        parameters         |> Option.iter (fun fps -> for fp in fps do this.AddParameter(fp))
        components         |> Option.iter (fun pvs -> for pv in pvs do this.AddComponent(pv))
        additionalProperty |> Option.iter (fun pvs -> for pv in pvs do this.AddAdditionalProperty(pv))

    member _.Name
        with get() = _name
        and set v = _name <- v

    member _.Description
        with get() = _description
        and set v = _description <- v

    member _.Version
        with get() = _version
        and set v = _version <- v

    member _.Url
        with get() = _url
        and set v = _url <- v

    member _.IntendedUse
        with get() = _intendedUse
        and set v = _intendedUse <- v

    member _.AdditionalType
        with get() = _additionalType
        and set v = _additionalType <- v

    member _.Parameters = _parameters

    /// Equipment, reagents, and software used in this recipe (components).
    member _.Components = _components

    member _.AdditionalProperty = _additionalProperty

    member this.AddParameter(fp: FormalParameter) =
        if not (_parameters |> Seq.exists (fun x -> x = fp)) then
            _parameters.Add(fp)

    member _.RemoveParameter(fp: FormalParameter) =
        _parameters.Remove(fp) |> ignore

    member _.TryGetParameter(name: string) =
        _parameters |> Seq.tryFind (fun fp -> fp.Name = name)

    member this.AddComponent(pv: Annotation) =
        if not (_components |> Seq.exists (fun x -> x = pv)) then
            _components.Add(pv)

    member _.RemoveComponent(pv: Annotation) =
        _components.Remove(pv) |> ignore

    member this.AddAdditionalProperty(pv: Annotation) =
        if not (_additionalProperty |> Seq.exists (fun x -> x = pv)) then
            _additionalProperty.Add(pv)

    member _.RemoveAdditionalProperty(pv: Annotation) =
        _additionalProperty.Remove(pv) |> ignore

    override this.Equals(obj) =
        match obj with
        | :? Recipe as other ->
            this.Name = other.Name &&
            this.Version = other.Version &&
            this.Url = other.Url
        | _ -> false

    override this.GetHashCode() = hash (this.Name, this.Version, this.Url)

    #if FABLE_COMPILER_PYTHON
    // Python calls these dunder methods for native `hash(x)` and `x == y`.
    member this.``__hash__``() =
        Fable.Core.PyInterop.emitPyExpr (this) "int($0.GetHashCode())"
    #endif
// ─────────────────────────────────────────────────────────────────────────────
// Process
// ─────────────────────────────────────────────────────────────────────────────

/// Core transformation node. Connects inputs to outputs by executing a recipe.
/// bioschemas.org/LabProcess
and [<AttachMembers>] Process(name: string, ?executesRecipe: Recipe, ?additionalType: string, ?input: IONode, ?output: IONode, ?parameterValue: seq<Annotation>) as this =

    inherit DynamicObj()

    let mutable _name: string = name
    let mutable _executesRecipe: Recipe option = executesRecipe
    let mutable _additionalType: string option = additionalType
    let mutable _processOf: Dataset option = None
    let mutable _input: IONode option = None
    let mutable _output: IONode option = None
    let _parameterValue: ResizeArray<Annotation> = ResizeArray()

    // ── Internal back-edge helpers ────────────────────────────────────────────

    let addInputBackEdge (node: IONode) (proc: Process) =
        match node with
        | SampleNode m ->
            m.InputOf.Add(proc) |> ignore
        | DataNode d ->
            d.InputOf.Add(proc) |> ignore

    let removeInputBackEdge (node: IONode) (proc: Process) =
        match node with
        | SampleNode m -> m.InputOf.Remove(proc) |> ignore
        | DataNode d     -> d.InputOf.Remove(proc) |> ignore

    let addOutputBackEdge (node: IONode) (proc: Process) =
        match node with
        | SampleNode m ->
            m.OutputOf.Add(proc) |> ignore
        | DataNode d ->
            d.OutputOf.Add(proc) |> ignore

    let removeOutputBackEdge (node: IONode) (proc: Process) =
        match node with
        | SampleNode m -> m.OutputOf.Remove(proc) |> ignore
        | DataNode d     -> d.OutputOf.Remove(proc) |> ignore

    /// Returns the canonical instance from the root dataset's registry, or the
    /// node itself if no dataset is assigned yet.
    let resolveNode (node: IONode) =
        match _processOf with
        | None    -> node
        | Some ds -> ds.CanonicalizeNode(node)

    do
        input          |> Option.iter (fun n -> this.SetInput(n))
        output         |> Option.iter (fun n -> this.SetOutput(n))
        parameterValue |> Option.iter (fun pvs -> for pv in pvs do this.AddParameterValue(pv))

    member _.Name
        with get() = _name
        and set v = _name <- v

    member this.ExecutesRecipe
        with get() = _executesRecipe
        and set v =
            let previous = _executesRecipe
            _executesRecipe <-
                match _processOf, v with
                | Some ds, Some recipe -> Some (ds.CanonicalizeRecipe(recipe))
                | _, value -> value
            match _processOf with
            | Some ds -> previous |> Option.iter ds.TryEvictRecipe
            | None -> ()

    member _.AdditionalType
        with get() = _additionalType
        and set v = _additionalType <- v

    /// Back-edge: the Dataset this process belongs to
    member _.ProcessOf
        with get() = _processOf
        and set v = _processOf <- v

    member _.Input = _input
    member _.Output = _output
    member _.ParameterValue = _parameterValue

    // ── Input CRUD ────────────────────────────────────────────────────────────

    /// Add input. Resolves the node against the root registry so back-edges are
    /// shared when an equal node already exists anywhere in the dataset hierarchy.
    member this.SetInput(node: IONode) =
        let node = resolveNode node
        let previous = _input
        previous |> Option.iter (fun old -> removeInputBackEdge old this)
        _input <- Some node
        addInputBackEdge node this
        match _processOf with
        | Some ds -> previous |> Option.iter ds.TryEvictNode
        | None -> ()

    /// Re-canonicalize all existing inputs and outputs against the given dataset's
    /// root registry. Called when the process is added to a dataset after its nodes
    /// were already populated. Migrates back-edges if the canonical instance differs.
    member this.CanonicalizeAllNodes(ds: Dataset) =
        _input |> Option.iter (fun original ->
            let canonical = ds.CanonicalizeNode(original)
            match original, canonical with
            | SampleNode mo, SampleNode mc when not (obj.ReferenceEquals(mo, mc)) ->
                _input <- Some canonical
                removeInputBackEdge original this
                addInputBackEdge canonical this
            | DataNode do', DataNode dc when not (obj.ReferenceEquals(do', dc)) ->
                _input <- Some canonical
                removeInputBackEdge original this
                addInputBackEdge canonical this
            | _ -> ())
        _output |> Option.iter (fun original ->
            let canonical = ds.CanonicalizeNode(original)
            match original, canonical with
            | SampleNode mo, SampleNode mc when not (obj.ReferenceEquals(mo, mc)) ->
                _output <- Some canonical
                removeOutputBackEdge original this
                addOutputBackEdge canonical this
            | DataNode do', DataNode dc when not (obj.ReferenceEquals(do', dc)) ->
                _output <- Some canonical
                removeOutputBackEdge original this
                addOutputBackEdge canonical this
            | _ -> ())
        _executesRecipe <- _executesRecipe |> Option.map ds.CanonicalizeRecipe

    member this.SetInputSample(m: Sample) = this.SetInput(SampleNode m)
    member this.SetInputData(d: Data) = this.SetInput(DataNode d)

    member this.ClearInput() =
        let previous = _input
        previous |> Option.iter (fun node -> removeInputBackEdge node this)
        _input <- None
        match _processOf with
        | Some ds -> previous |> Option.iter ds.TryEvictNode
        | None -> ()

    // ── Output CRUD ───────────────────────────────────────────────────────────

    /// Add output. Resolves the node against the root registry so back-edges are
    /// shared when an equal node already exists anywhere in the dataset hierarchy.
    member this.SetOutput(node: IONode) =
        let node = resolveNode node
        let previous = _output
        previous |> Option.iter (fun old -> removeOutputBackEdge old this)
        _output <- Some node
        addOutputBackEdge node this
        match _processOf with
        | Some ds -> previous |> Option.iter ds.TryEvictNode
        | None -> ()

    member this.SetOutputSample(m: Sample) = this.SetOutput(SampleNode m)
    member this.SetOutputData(d: Data) = this.SetOutput(DataNode d)

    member this.ClearOutput() =
        let previous = _output
        previous |> Option.iter (fun node -> removeOutputBackEdge node this)
        _output <- None
        match _processOf with
        | Some ds -> previous |> Option.iter ds.TryEvictNode
        | None -> ()

    // ── ParameterValue CRUD ───────────────────────────────────────────────────

    member this.AddParameterValue(pv: Annotation) =
        //if not (_parameterValue |> Seq.exists (fun x -> x = pv)) then
        _parameterValue.Add(pv)

    member _.RemoveParameterValue(pv: Annotation) =
        _parameterValue.Remove(pv) |> ignore

    member _.TryGetParameterValue(name: string) =
        _parameterValue |> Seq.tryFind (fun pv -> pv.Name = name)

    member _.GetParameterValue(name: string) =
        _parameterValue |> Seq.find (fun pv -> pv.Name = name)

    // ── Query helpers ─────────────────────────────────────────────────────────

    /// Input nodes that are Samples.
    member _.InputSample() : Sample option =
        match _input with Some (SampleNode m) -> Some m | _ -> None

    /// Input nodes that are Data.
    member _.InputData() : Data option =
        match _input with Some (DataNode d) -> Some d | _ -> None

    /// Output nodes that are Samples.
    member _.OutputSample() : Sample option =
        match _output with Some (SampleNode m) -> Some m | _ -> None

    /// Output nodes that are Data.
    member _.OutputData() : Data option =
        match _output with Some (DataNode d) -> Some d | _ -> None

    /// FormalParameters defined on the recipe executed by this process.
    member _.RecipeParameters() : ResizeArray<FormalParameter> =
        match _executesRecipe with
        | Some proto -> proto.Parameters
        | None       -> ResizeArray()

    /// Annotations from all sources (parameters, input/output node properties, recipe components)
    /// whose name matches the given string.
    member this.AnnotationsByName(name: string) : ResizeArray<Annotation> =
        let result = ResizeArray<Annotation>()
        for pv: Annotation in _parameterValue do
            if pv.Name = name then result.Add(pv)
        for n: IONode in _input |> Option.toList do
            match n with
            | SampleNode m -> for pv: Annotation in m.AdditionalProperty do if pv.Name = name then result.Add(pv)
            | DataNode d -> for pv: Annotation in d.AdditionalProperty do if pv.Name = name then result.Add(pv)
        for n: IONode in _output |> Option.toList do
            match n with
            | SampleNode m -> for pv: Annotation in m.AdditionalProperty do if pv.Name = name then result.Add(pv)
            | DataNode d -> for pv: Annotation in d.AdditionalProperty do if pv.Name = name then result.Add(pv)
        match _executesRecipe with
        | Some proto ->
            for pv: Annotation in proto.Components do if pv.Name = name then result.Add(pv)
        | None -> ()
        result

    /// Identity: two processes with the same name within the same dataset are identical.
    override this.Equals(obj) =
        match obj with
        | :? Process as other -> this.Name = other.Name
        | _ -> false

    override this.GetHashCode() = hash this.Name

    #if FABLE_COMPILER_PYTHON
    // Python calls these dunder methods for native `hash(x)` and `x == y`.
    member this.``__hash__``() =
        Fable.Core.PyInterop.emitPyExpr (this) "int($0.GetHashCode())"
    #endif

// ─────────────────────────────────────────────────────────────────────────────
// Dataset
// ─────────────────────────────────────────────────────────────────────────────

/// Container and context for data, processes, administrative metadata, and datamap entries.
/// schema.org/Dataset
and [<AttachMembers>] Dataset(identifier: string, ?title: string, ?description: string, ?additionalType: string, ?license: string, ?datePublished: string, ?dateCreated: string, ?dateModified: string, ?processes: seq<Process>, ?hasPart: seq<Dataset>, ?dataFiles: seq<Data>, ?agents: seq<Agent>, ?citations: seq<ScholarlyArticle>, ?dataContexts: seq<DataContext>, ?additionalProperty: seq<Annotation>) as this =

    inherit DynamicObj()

    let mutable _identifier: string = identifier
    let mutable _title: string option = title
    let mutable _description: string option = description
    let mutable _additionalType: string option = additionalType
    let mutable _license: string option = license
    let mutable _datePublished: string option = datePublished
    let mutable _dateCreated: string option = dateCreated
    let mutable _dateModified: string option = dateModified
    let mutable _partOf: Dataset option = None
    let _processes: ResizeArray<Process> = ResizeArray()
    let _hasPart: ResizeArray<Dataset> = ResizeArray()
    let _dataFiles: ResizeArray<Data> = ResizeArray()
    let _agents: ResizeArray<Agent> = ResizeArray()
    let _citations: ResizeArray<ScholarlyArticle> = ResizeArray()
    let _dataContexts: ResizeArray<DataContext> = ResizeArray()
    let _additionalProperty: ResizeArray<Annotation> = ResizeArray()
    /// IONode registry — only meaningfully populated when this is the root dataset.
    let _nodeRegistry: Dictionary<string, IONode> = Dictionary<string, IONode>()
    let _nodePins: HashSet<string> = HashSet<string>()
    let _recipeRegistry: Dictionary<string, Recipe> = Dictionary<string, Recipe>()
    let _recipePins: HashSet<string> = HashSet<string>()
    let _fragmentSelectorProviders: Dictionary<string, IFragmentSelectorProvider> = Dictionary<string, IFragmentSelectorProvider>()

    let valueKey (value: string) =
        string value.Length + ":" + value

    let optionKey (value: string option) =
        match value with
        | Some v -> "S" + valueKey v
        | None -> "N"

    let fieldsKey (values: seq<string>) =
        values
        |> Seq.map valueKey
        |> String.concat ""

    let recipeKey (recipe: Recipe) =
        fieldsKey [
            optionKey recipe.Name
            optionKey recipe.Version
            optionKey recipe.Url
        ]

    let definedTermKey (dt: DefinedTerm) =
        fieldsKey [
            dt.Name
            optionKey dt.TAN
            optionKey dt.InDefinedTermSet
        ]

    let dataContextKey (dc: DataContext) =
        fieldsKey [
            dc.Data.Path
            optionKey dc.Data.Selector
            match dc.Explication with
            | Some dt -> "E" + definedTermKey dt
            | None -> "N"
            match dc.ObjectType with
            | Some dt -> "O" + definedTermKey dt
            | None -> "N"
            match dc.Unit with
            | Some dt -> "U" + definedTermKey dt
            | None -> "N"
            optionKey dc.Label
            optionKey dc.Description
            optionKey dc.GeneratedBy
        ]

    let indexOfProcessByReference (proc: Process) =
        let mutable index = -1
        let mutable i = 0
        while index < 0 && i < _processes.Count do
            if obj.ReferenceEquals(_processes.[i], proc) then
                index <- i
            i <- i + 1
        index

    do
        processes          |> Option.iter (fun ps  -> for p  in ps  do this.AddProcess(p))
        hasPart            |> Option.iter (fun ds  -> for d  in ds  do this.AddPart(d))
        dataFiles          |> Option.iter (fun ds  -> for d  in ds  do this.AddDataFile(d))
        agents           |> Option.iter (fun ps  -> for p  in ps  do this.AddAgent(p))
        citations          |> Option.iter (fun cs  -> for c  in cs  do this.AddCitation(c))
        dataContexts       |> Option.iter (fun pvs -> for pv in pvs do this.AddDataContext(pv))
        additionalProperty |> Option.iter (fun pvs -> for pv in pvs do this.AddAdditionalProperty(pv))

    // ── Registry helpers ──────────────────────────────────────────────────────

    // Direct access to the backing dictionary of this dataset instance.
    member private _.NodeRegistryDirect = _nodeRegistry

    member private _.NodePinsDirect = _nodePins

    member private _.RecipeRegistryDirect = _recipeRegistry

    member private _.RecipePinsDirect = _recipePins

    member private _.FragmentSelectorProvidersDirect = _fragmentSelectorProviders

    /// Walk PartOf until reaching the root dataset of this hierarchy.
    member private this.RootDataset() : Dataset =
        match _partOf with
        | None   -> this
        | Some p -> p.RootDataset()

    /// Register a fragment selector provider in the root dataset of this hierarchy.
    member this.RegisterFragmentSelectorProvider(provider: IFragmentSelectorProvider) =
        this.RootDataset().FragmentSelectorProvidersDirect.[provider.SelectorFormat] <- provider

    /// Returns a fragment selector provider from the root dataset, if registered.
    member this.TryGetFragmentSelectorProvider(selectorFormat: string) : IFragmentSelectorProvider option =
        let providers = this.RootDataset().FragmentSelectorProvidersDirect
        match providers.TryGetValue selectorFormat with
        | true, provider -> Some provider
        | false, _ -> None

    /// Remove a fragment selector provider from the root dataset.
    member this.UnregisterFragmentSelectorProvider(selectorFormat: string) =
        this.RootDataset().FragmentSelectorProvidersDirect.Remove(selectorFormat) |> ignore

    member this.GetFragmentSelectorProviders() : seq<IFragmentSelectorProvider> =
        this.RootDataset().FragmentSelectorProvidersDirect.Values :> seq<IFragmentSelectorProvider>

    /// Returns the canonical IONode for `node` from the root registry.
    /// Registers and returns `node` itself if its key is not yet present.
    member this.CanonicalizeNode(node: IONode) : IONode =
        let registry = this.RootDataset().NodeRegistryDirect
        let key = node.Key()
        match registry.TryGetValue(key) with
        | true, existing -> existing
        | false, _       ->
            registry.[key] <- node
            node

    /// Pins a canonical node in the root registry independently of process use.
    member internal this.PinNode(node: IONode) : IONode =
        let canonical = this.CanonicalizeNode(node)
        this.RootDataset().NodePinsDirect.Add(canonical.Key()) |> ignore
        canonical

    /// Releases an ARC-owned node pin and evicts the node when otherwise unused.
    member internal this.UnpinNode(node: IONode) =
        this.RootDataset().NodePinsDirect.Remove(node.Key()) |> ignore
        this.TryEvictNode(node)

    /// Returns the hierarchy-wide canonical Recipe using Recipe identity.
    member this.CanonicalizeRecipe(recipe: Recipe) : Recipe =
        // Spreadsheet/table builders commonly attach an empty Recipe and populate
        // its name afterwards. Such incomplete objects do not yet have a stable
        // identity and must not collapse into one another.
        match recipe.Name with
        | None -> recipe
        | Some _ ->
            let registry = this.RootDataset().RecipeRegistryDirect
            let key = recipeKey recipe
            match registry.TryGetValue(key) with
            | true, existing -> existing
            | false, _ ->
                registry.[key] <- recipe
                recipe

    /// Pins a canonical recipe in the root registry independently of process use.
    member internal this.PinRecipe(recipe: Recipe) : Recipe =
        let canonical = this.CanonicalizeRecipe(recipe)
        if canonical.Name.IsSome then
            this.RootDataset().RecipePinsDirect.Add(recipeKey canonical) |> ignore
        canonical

    /// Releases an ARC-owned recipe pin and evicts it when otherwise unused.
    member internal this.UnpinRecipe(recipe: Recipe) =
        if recipe.Name.IsSome then
            this.RootDataset().RecipePinsDirect.Remove(recipeKey recipe) |> ignore
        this.TryEvictRecipe(recipe)

    /// Evicts `node` from the root registry if no process remaining in the
    /// hierarchy still holds it as an input or output.
    member internal this.TryEvictNode(node: IONode) =
        let root = this.RootDataset()
        let key  = node.Key()
        let usedByProcess =
            Seq.append (node.GetInputOf() :> seq<Process>) (node.GetOutputOf() :> seq<Process>)
            |> Seq.exists (fun proc ->
                proc.ProcessOf
                |> Option.exists (fun ds -> obj.ReferenceEquals(ds.RootDataset(), root)))
        let storedAsDataFile =
            root.AllDataFiles()
            |> Seq.exists (fun data -> (DataNode data).Key() = key)
        let pinned = root.NodePinsDirect.Contains(key)
        if not (usedByProcess || storedAsDataFile || pinned) then
            root.NodeRegistryDirect.Remove(key) |> ignore

    /// Evicts a recipe when neither a process nor an ARC store uses it.
    member internal this.TryEvictRecipe(recipe: Recipe) =
        let root = this.RootDataset()
        if recipe.Name.IsSome then
            let key = recipeKey recipe
            let usedByProcess =
                root.AllProcesses()
                |> Seq.exists (fun proc ->
                    proc.ExecutesRecipe
                    |> Option.exists (fun current -> current.Name.IsSome && recipeKey current = key))
            if not (usedByProcess || root.RecipePinsDirect.Contains(key)) then
                root.RecipeRegistryDirect.Remove(key) |> ignore

    member _.Identifier
        with get() = _identifier
        and set v = _identifier <- v

    member _.Title
        with get() = _title
        and set v = _title <- v

    member _.Description
        with get() = _description
        and set v = _description <- v

    /// Decoration discriminator (e.g. "Investigation", "Study", "Assay")
    member _.AdditionalType
        with get() = _additionalType
        and set v = _additionalType <- v

    member _.License
        with get() = _license
        and set v = _license <- v

    member _.DatePublished
        with get() = _datePublished
        and set v = _datePublished <- v

    member _.DateCreated
        with get() = _dateCreated
        and set v = _dateCreated <- v

    member _.DateModified
        with get() = _dateModified
        and set v = _dateModified <- v

    /// Back-edge: parent dataset
    member _.PartOf
        with get() = _partOf
        and set v = _partOf <- v

    member _.Processes = _processes
    member _.HasPart = _hasPart
    member _.DataFiles = _dataFiles
    member _.Agents = _agents
    member _.Citations = _citations
    member _.DataContexts = _dataContexts
    member _.AdditionalProperty = _additionalProperty

    // ── Process CRUD ──────────────────────────────────────────────────────────

    member this.AddProcess(proc: Process) =
        if proc.ProcessOf.IsSome then
            if proc.ProcessOf.Value <> this then
                failwithf "Process '%s' already belongs to another dataset." proc.Name
        else
            _processes.Add(proc)
            proc.ProcessOf <- Some this
            // Canonicalize any nodes the process already carries against the root registry.
            proc.CanonicalizeAllNodes(this)

    member this.RemoveProcess(proc: Process) =
        let index = indexOfProcessByReference proc
        let removed = index >= 0
        if removed then _processes.RemoveAt(index)
        if removed && proc.ProcessOf = Some this then
            let previousRecipe = proc.ExecutesRecipe
            proc.ProcessOf <- None
            proc.Input |> Option.iter this.TryEvictNode
            proc.Output |> Option.iter this.TryEvictNode
            previousRecipe |> Option.iter this.TryEvictRecipe

    member _.TryGetProcess(name: string) =
        _processes |> Seq.tryFind (fun p -> p.Name = name)

    member _.GetProcess(name: string) =
        _processes |> Seq.find (fun p -> p.Name = name)

    // ── HasPart CRUD ──────────────────────────────────────────────────────────

    member this.AddPart(child: Dataset) =
        if not (_hasPart |> Seq.exists (fun d -> d = child)) then
            _hasPart.Add(child)
            child.PartOf <- Some this
            for provider in child.FragmentSelectorProvidersDirect.Values do
                this.RegisterFragmentSelectorProvider(provider)
            let rec canonicalizeDataFiles (dataset: Dataset) =
                for i in 0 .. dataset.DataFiles.Count - 1 do
                    dataset.DataFiles.[i] <- this.CanonicalizeDataTree(dataset.DataFiles.[i])
                for part in dataset.HasPart do
                    canonicalizeDataFiles part
            canonicalizeDataFiles child
            // Canonicalize every node in the child's subtree against the new root.
            for proc in child.AllProcesses() do
                proc.CanonicalizeAllNodes(this)

    member this.RemovePart(child: Dataset) =
        let removed = _hasPart.Remove(child)
        if removed && child.PartOf = Some this then
            // Collect nodes before disconnecting so the root reference is still valid.
            let nodesToCheck =
                Seq.append
                    (child.AllProcesses()
                     |> Seq.collect (fun p -> [p.Input; p.Output] |> List.choose id))
                    (child.AllDataFiles() |> Seq.map DataNode)
                |> Seq.distinctBy (fun n -> n.Key())
                |> Seq.toList
            let recipesToCheck =
                child.AllProcesses()
                |> Seq.choose (fun proc -> proc.ExecutesRecipe)
                |> Seq.distinctBy recipeKey
                |> Seq.toList
            child.PartOf <- None
            // Evict nodes that are no longer used anywhere in the (now smaller) tree.
            for node in nodesToCheck do
                this.TryEvictNode(node)
            for recipe in recipesToCheck do
                this.TryEvictRecipe(recipe)
            // Rebuild child's own registry now that it is a root again.
            child.NodeRegistryDirect.Clear()
            child.RecipeRegistryDirect.Clear()
            for data in child.AllDataFiles() do
                child.CanonicalizeNode(DataNode data) |> ignore
            for proc in child.AllProcesses() do
                proc.CanonicalizeAllNodes(child)

    member _.TryGetPart(identifier: string) =
        _hasPart |> Seq.tryFind (fun d -> d.Identifier = identifier)

    member private this.CanonicalizeDataTree(data: Data) : Data =
        let canonical =
            match this.CanonicalizeNode(DataNode data) with
            | DataNode value -> value
            | SampleNode _ -> failwith "A Data identity key resolved to a Sample."
        if obj.ReferenceEquals(canonical, data) then
            for i in 0 .. canonical.HasPart.Count - 1 do
                canonical.HasPart.[i] <- this.CanonicalizeDataTree(canonical.HasPart.[i])
        canonical

    member this.AddDataFile(data: Data) =
        let canonical = this.CanonicalizeDataTree(data)
        if not (_dataFiles |> Seq.exists (fun d -> d = canonical)) then
            _dataFiles.Add(canonical)

    member this.RemoveDataFile(data: Data) =
        match _dataFiles |> Seq.tryFind (fun current -> current = data) with
        | Some stored ->
            let nodes =
                let rec collect (current: Data) =
                    seq {
                        yield DataNode current
                        for part in current.HasPart do
                            yield! collect part
                    }
                collect stored |> Seq.toList
            _dataFiles.Remove(stored) |> ignore
            for node in nodes do
                this.TryEvictNode(node)
        | None -> ()

    member _.TryGetDataFile(path: string) =
        _dataFiles |> Seq.tryFind (fun d -> d.Path = path)

    member this.AddAgent(agent: Agent) =
        if not (_agents |> Seq.exists (fun p -> p = agent)) then
            _agents.Add(agent)

    member _.RemoveAgent(agent: Agent) =
        _agents.Remove(agent) |> ignore

    member this.AddCitation(article: ScholarlyArticle) =
        if not (_citations |> Seq.exists (fun c -> c = article)) then
            _citations.Add(article)

    member _.RemoveCitation(article: ScholarlyArticle) =
        _citations.Remove(article) |> ignore

    member this.AddDataContext(dataContext: DataContext) =
        if not (_dataContexts |> Seq.exists (fun x -> x = dataContext)) then
            _dataContexts.Add(dataContext)

    member _.RemoveDataContext(dataContext: DataContext) =
        _dataContexts.Remove(dataContext) |> ignore

    // ── AdditionalProperty CRUD ───────────────────────────────────────────────

    member this.AddAdditionalProperty(pv: Annotation) =
        if not (_additionalProperty |> Seq.exists (fun x -> x = pv)) then
            _additionalProperty.Add(pv)

    member _.RemoveAdditionalProperty(pv: Annotation) =
        _additionalProperty.Remove(pv) |> ignore

    // ── Collection helpers ────────────────────────────────────────────────────

    /// All processes in this dataset and all nested datasets (depth-first)
    member this.AllProcesses() : ResizeArray<Process> =
        let acc = ResizeArray()
        let rec collect (ds: Dataset) =
            acc.AddRange(ds.Processes)
            for child in ds.HasPart do collect child
        collect this
        acc

    /// All distinct Sample nodes reachable from processes in this dataset
    member this.AllSamples() : ResizeArray<Sample> =
        let acc = ResizeArray()
        let seen = HashSet<string>()
        for proc in this.AllProcesses() do
            for node in [proc.Input; proc.Output] |> List.choose id do
                match node with
                | SampleNode m when seen.Add(m.Name) -> acc.Add(m)
                | _ -> ()
        acc

    /// All distinct Data nodes reachable from processes in this dataset
    member this.AllData() : ResizeArray<Data> =
        let acc = ResizeArray()
        let seen = HashSet<string>()
        let rec addData (d: Data) =
            let key = d.Path + "|" + (d.Selector |> Option.defaultValue "")
            if seen.Add(key) then acc.Add(d)
            for child in d.HasPart do
                addData child
        let addNode (node: IONode) =
            match node with
            | DataNode d -> addData d
            | _ -> ()
        let rec collectDataset (ds: Dataset) =
            for d in ds.DataFiles do
                addData d
            for proc in ds.Processes do
                proc.Input |> Option.iter addNode
                proc.Output |> Option.iter addNode
            for child in ds.HasPart do
                collectDataset child
        collectDataset this
        acc

    /// All distinct IONodes (samples and data) from all processes in this dataset.
    member this.AllNodes() : ResizeArray<IONode> =
        let acc  = ResizeArray<IONode>()
        let seen = HashSet<string>()
        for proc in this.AllProcesses() do
            proc.Input |> Option.iter (fun n -> if seen.Add(n.Key()) then acc.Add(n))
            proc.Output |> Option.iter (fun n -> if seen.Add(n.Key()) then acc.Add(n))
        acc

    member this.AllDataFiles() : ResizeArray<Data> =
        let acc = ResizeArray<Data>()
        let seen = HashSet<string>()
        let rec addData (d: Data) =
            let key = d.Path + "|" + (d.Selector |> Option.defaultValue "")
            if seen.Add(key) then acc.Add(d)
            for child in d.HasPart do addData child
        let rec collect (ds: Dataset) =
            for d in ds.DataFiles do addData d
            for child in ds.HasPart do collect child
        collect this
        acc

    member this.AllAgents() : ResizeArray<Agent> =
        let acc = ResizeArray<Agent>()
        let seen = HashSet<string>()
        let agentKey (p: Agent) =
            p.Id |> Option.defaultValue (p.GivenName + "|" + (p.FamilyName |> Option.defaultValue "") + "|" + (p.Email |> Option.defaultValue ""))
        let rec collect (ds: Dataset) =
            for agent in ds.Agents do
                if seen.Add(agentKey agent) then acc.Add(agent)
            for child in ds.HasPart do collect child
        collect this
        acc

    member this.AllCitations() : ResizeArray<ScholarlyArticle> =
        let acc = ResizeArray<ScholarlyArticle>()
        let seen = HashSet<string>()
        let articleKey (article: ScholarlyArticle) =
            article.Id |> Option.defaultValue (article.Headline + "|" + (article.Identifier |> Option.defaultValue ""))
        let rec collect (ds: Dataset) =
            for citation in ds.Citations do
                if seen.Add(articleKey citation) then acc.Add(citation)
            for child in ds.HasPart do collect child
        collect this
        acc

    member this.AllOrganizations() : ResizeArray<Organization> =
        let acc = ResizeArray<Organization>()
        let seen = HashSet<string>()
        let orgKey (org: Organization) =
            org.Id |> Option.defaultValue org.Name
        for agent in this.AllAgents() do
            match agent.Affiliation with
            | Some org when seen.Add(orgKey org) -> acc.Add(org)
            | _ -> ()
        for citation in this.AllCitations() do
            for author in citation.Authors do
                match author.Affiliation with
                | Some org when seen.Add(orgKey org) -> acc.Add(org)
                | _ -> ()
        acc

    member this.AllDataContexts() : ResizeArray<DataContext> =
        let acc = ResizeArray<DataContext>()
        let seen = HashSet<string>()
        let rec collect (ds: Dataset) =
            for dc in ds.DataContexts do
                if seen.Add(dataContextKey dc) then acc.Add(dc)
            for child in ds.HasPart do collect child
        collect this
        acc

    member this.DataContextsForData(data: Data) : ResizeArray<DataContext> =
        let acc = ResizeArray<DataContext>()
        let seen = HashSet<string>()
        for dc in this.AllDataContexts() do
            if dc.Data = data then
                if seen.Add(dataContextKey dc) then acc.Add(dc)
        acc

    // ── Root / Final nodes ────────────────────────────────────────────────────

    member this.DataContextsForPath(path: string) : ResizeArray<DataContext> =
        let acc = ResizeArray<DataContext>()
        let seen = HashSet<string>()
        for dc in this.AllDataContexts() do
            if dc.Data.Path = path then
                if seen.Add(dataContextKey dc) then acc.Add(dc)
        acc

    member this.DataContextsCoveringData(data: Data) : ResizeArray<DataContext> =
        let acc = ResizeArray<DataContext>()
        let seen = HashSet<string>()
        for dc in this.AllDataContexts() do
            match FragmentSelectorResolution.relateDataWith this.TryGetFragmentSelectorProvider dc.Data data with
            | Exact | Contains ->
                if seen.Add(dataContextKey dc) then acc.Add(dc)
            | Disjoint | Unknown -> ()
        acc

    member this.DataWithDataContextByExplication(term: DefinedTerm) : ResizeArray<Data * DataContext> =
        let acc = ResizeArray<Data * DataContext>()
        let seen = HashSet<string>()
        for data in this.AllData() do
            for dc in this.DataContextsCoveringData(data) do
                if dc.ExplicationEquals(term) then
                    let key = data.Path + "|" + (data.Selector |> Option.defaultValue "") + "|" + dataContextKey dc
                    if seen.Add(key) then acc.Add(data, dc)
        acc

    /// All IONodes in this dataset with no predecessor process (terminal sources).
    member this.RootNodes() : ResizeArray<IONode> =
        let scope = this.AllProcesses()
        this.AllNodes() |> Seq.filter (fun n -> n.IsRootNode(scope)) |> ResizeArray

    /// All IONodes in this dataset with no successor process (terminal sinks).
    member this.FinalNodes() : ResizeArray<IONode> =
        let scope = this.AllProcesses()
        this.AllNodes() |> Seq.filter (fun n -> n.IsFinalNode(scope)) |> ResizeArray

    /// Root nodes that are Samples.
    member this.RootSamples() : ResizeArray<Sample> =
        this.RootNodes()
        |> Seq.choose (fun n -> match n with | SampleNode m -> Some m | _ -> None)
        |> ResizeArray

    /// Final nodes that are Samples.
    member this.FinalSamples() : ResizeArray<Sample> =
        this.FinalNodes()
        |> Seq.choose (fun n -> match n with | SampleNode m -> Some m | _ -> None)
        |> ResizeArray

    /// Root nodes that are Data.
    member this.RootData() : ResizeArray<Data> =
        this.RootNodes()
        |> Seq.choose (fun n -> match n with | DataNode d -> Some d | _ -> None)
        |> ResizeArray

    /// Final nodes that are Data.
    member this.FinalData() : ResizeArray<Data> =
        this.FinalNodes()
        |> Seq.choose (fun n -> match n with | DataNode d -> Some d | _ -> None)
        |> ResizeArray

    member this.TryGetSampleByName(name: string) : Sample option =
        match _nodeRegistry.TryGetValue("M:" + name) with
        | true, SampleNode m -> Some m
        | _ -> None

    // to-do: Implement properly
    //member this.TryGetDataByPath(path: string, ?selector: string) : Data option =
    //    match _nodeRegistry.TryGetValue("D:" + path + "|" + (selector |> Option.defaultValue "")) with
    //    | true, DataNode d -> Some d
    //    | _ -> None

    // ── Dataset-level property value queries ──────────────────────────────────

    /// All distinct Annotations from all sources across all processes in this dataset.
    /// Sources: process parameters, input/output node properties, recipe components.
    /// Optional recipeName restricts to processes whose recipe name matches.
    member this.AllAnnotations(?recipeName: string) : ResizeArray<Annotation> =
        let result =
            this.AllProcesses()
            |> PathTraversal.collectAnnotationsFromProcessesWithRecipeName recipeName
        let seen = HashSet<string>()
        for pv in result do
            seen.Add(PathTraversal.annotationKey pv) |> ignore
        if recipeName.IsNone then
            let rec collectDataset (ds: Dataset) =
                for pv in ds.AdditionalProperty do
                    PathTraversal.addAnnotation result seen pv
                for citation in ds.Citations do
                    for pv in citation.AdditionalProperty do
                        PathTraversal.addAnnotation result seen pv
                    for author in citation.Authors do
                        for pv in author.AdditionalProperty do
                            PathTraversal.addAnnotation result seen pv
                for agent in ds.Agents do
                    for pv in agent.AdditionalProperty do
                        PathTraversal.addAnnotation result seen pv
                for child in ds.HasPart do
                    collectDataset child
            collectDataset this
        result

    /// All Annotations from all sources connected to `node` (upstream + downstream) within this dataset.
    /// Optional recipeName restricts to processes whose recipe name matches.
    member this.AnnotationsForNode(node: IONode, ?recipeName: string) : ResizeArray<Annotation> =
        let scope      = this.AllProcesses()
        let upstream   = node.UpstreamAnnotations(?recipeName = recipeName, scope = scope)
        let downstream = node.DownstreamAnnotations(?recipeName = recipeName, scope = scope)
        let seenPV = HashSet<string>()
        let result = ResizeArray<Annotation>()
        for pv: Annotation in Seq.append upstream downstream do
            let key = PathTraversal.annotationKey pv
            if seenPV.Add(key) then result.Add(pv)
        result

    /// Annotations from all sources in processes upstream of `node` within this dataset.
    member this.UpstreamAnnotationsForNode(node: IONode, ?recipeName: string) : ResizeArray<Annotation> =
        node.UpstreamAnnotations(?recipeName = recipeName, scope = this.AllProcesses())

    /// Annotations from all sources in processes downstream of `node` within this dataset.
    member this.DownstreamAnnotationsForNode(node: IONode, ?recipeName: string) : ResizeArray<Annotation> =
        node.DownstreamAnnotations(?recipeName = recipeName, scope = this.AllProcesses())

    // ── Querying ──────────────────────────────────────────────────────────────

    // ── Dataset-scoped node and path queries ──────────────────────────────────

    /// All processes in this dataset in which `node` appears as an input or output.
    member this.ProcessesForNode(node: IONode) : ResizeArray<Process> =
        node.Processes(scope = this.AllProcesses())

    /// All maximal Paths through `node` within this dataset.
    member this.PathsThrough(node: IONode) : ResizeArray<Path> =
        node.PathsThrough(scope = this.AllProcesses())

    /// All IONodes reachable upstream from `node` within this dataset.
    member this.NodesUpstreamOf(node: IONode) : ResizeArray<IONode> =
        node.UpstreamNodes(scope = this.AllProcesses())

    /// All IONodes reachable downstream from `node` within this dataset.
    member this.NodesDownstreamOf(node: IONode) : ResizeArray<IONode> =
        node.DownstreamNodes(scope = this.AllProcesses())

    member this.SamplesUpstreamOf(node: IONode) : ResizeArray<Sample> =
        node.UpstreamSamples(scope = this.AllProcesses())

    member this.SamplesDownstreamOf(node: IONode) : ResizeArray<Sample> =
        node.DownstreamSamples(scope = this.AllProcesses())

    member this.DataUpstreamOf(node: IONode) : ResizeArray<Data> =
        node.UpstreamData(scope = this.AllProcesses())

    member this.DataDownstreamOf(node: IONode) : ResizeArray<Data> =
        node.DownstreamData(scope = this.AllProcesses())

    /// All IONodes connected to `node` within this dataset, excluding `node` itself.
    member this.AllConnectedNodes(node: IONode) : ResizeArray<IONode> =
        node.AllConnectedNodes(scope = this.AllProcesses())

    member this.ConnectedSamplesForNode(node: IONode) : ResizeArray<Sample> =
        node.ConnectedSamples(scope = this.AllProcesses())

    member this.ConnectedDataForNode(node: IONode) : ResizeArray<Data> =
        node.ConnectedData(scope = this.AllProcesses())

    member this.AllAnnotationsForNode(node: IONode) : ResizeArray<Annotation> =
        node.AllAnnotations(scope = this.AllProcesses())

    member this.RecipeParametersForNode(node: IONode) : ResizeArray<FormalParameter> =
        node.RecipeParameters(scope = this.AllProcesses())

    /// All processes whose executed recipe's intendedUse name matches.
    member this.FindProcessesByRecipeType(intendedUse: string) : ResizeArray<Process> =
        this.AllProcesses()
        |> Seq.filter (fun p ->
            match p.ExecutesRecipe with
            | Some proto ->
                match proto.IntendedUse with
                | Some dt -> dt.Name = intendedUse
                | None    -> false
            | None -> false)
        |> ResizeArray

    /// All processes that have a Annotation (from any source) with the given name and value.
    member this.FindProcessesByAnnotation(paramName: string, paramValue: string) : ResizeArray<Process> =
        let pvMatch (pv: Annotation) = pv.Name = paramName && pv.Value = Some paramValue
        let nodeMatch (n: IONode) =
            match n with
            | SampleNode m -> m.AdditionalProperty |> Seq.exists pvMatch
            | DataNode d -> d.AdditionalProperty |> Seq.exists pvMatch
        this.AllProcesses()
        |> Seq.filter (fun p ->
            (p.ParameterValue |> Seq.exists pvMatch) ||
            (p.Input |> Option.exists nodeMatch) ||
            (p.Output |> Option.exists nodeMatch) ||
            (match p.ExecutesRecipe with
             | Some proto ->
                 (proto.Components |> Seq.exists pvMatch)
             | None -> false))
        |> ResizeArray

    /// All processes that have a Annotation (from any source) with the given name (any value).
    member this.FindProcessesByPropertyName(paramName: string) : ResizeArray<Process> =
        let pvMatch (pv: Annotation) = pv.Name = paramName
        let nodeMatch (n: IONode) =
            match n with
            | SampleNode m -> m.AdditionalProperty |> Seq.exists pvMatch
            | DataNode d -> d.AdditionalProperty |> Seq.exists pvMatch
        this.AllProcesses()
        |> Seq.filter (fun p ->
            (p.ParameterValue |> Seq.exists pvMatch) ||
            (p.Input |> Option.exists nodeMatch) ||
            (p.Output |> Option.exists nodeMatch) ||
            (match p.ExecutesRecipe with
             | Some proto ->
                 (proto.Components |> Seq.exists pvMatch) ||
                 (proto.AdditionalProperty |> Seq.exists pvMatch)
             | None -> false))
        |> ResizeArray

    /// Find qualifying processes by recipe type, then filter their
    /// ParameterValues with a caller-supplied predicate.
    /// Returns all terminal-output Samples of the downstream subgraphs of qualifying processes.
    member this.SamplesResultingFromConditionBy
        (recipeType: string, paramPredicate: Annotation -> bool) : ResizeArray<Sample> =
        let qualifying =
            this.FindProcessesByRecipeType(recipeType)
            |> Seq.filter (fun p -> p.ParameterValue |> Seq.exists paramPredicate)
        let seen   = HashSet<string>()
        let result = ResizeArray<Sample>()
        for proc in qualifying do
            for path in PathTraversal.extendToMaximalPaths (this.AllProcesses()) proc do
                for node in path.TerminalOutputs() do
                    match node with
                    | SampleNode m when seen.Add(m.Name) -> result.Add(m)
                    | _ -> ()
        result

    /// Identity: datasets with the same identifier are identical.
    override this.Equals(obj) =
        match obj with
        | :? Dataset as other -> this.Identifier = other.Identifier
        | _ -> false

    override this.GetHashCode() = hash this.Identifier

    #if FABLE_COMPILER_PYTHON
    // Python calls these dunder methods for native `hash(x)` and `x == y`.
    member this.``__hash__``() =
        Fable.Core.PyInterop.emitPyExpr (this) "int($0.GetHashCode())"
    #endif

