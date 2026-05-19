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
    /// LabProcess.Equals is name-based, so without this two distinct process
    /// objects with the same name would collide in the set.
    let refEqProcess =
        { new IEqualityComparer<LabProcess> with
            member _.Equals(x, y)   = obj.ReferenceEquals(x, y)
            member _.GetHashCode(o) = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o) }

// ─────────────────────────────────────────────────────────────────────────────
// Path
// ─────────────────────────────────────────────────────────────────────────────

/// A directed walk through the process graph: an ordered sequence of LabProcess
/// instances connected through their shared I/O nodes.
/// Read-only view – produced by graph-traversal queries; does not own its processes.
[<AttachMembers>]
type Path(processes: ResizeArray<LabProcess>) =

    member _.Processes = processes

    /// First process in the path
    member _.Head = if processes.Count > 0 then Some processes.[0] else None

    /// Last process in the path
    member _.Last = if processes.Count > 0 then Some processes.[processes.Count - 1] else None

    member _.Length = processes.Count

    /// Whether this path contains the given IONode (as input or output of any process)
    member _.ContainsNode(node: IONode) : bool =
        let key = node.Key()
        processes |> Seq.exists (fun (p: LabProcess) ->
            p.Inputs  |> Seq.exists (fun (n: IONode) -> n.Key() = key) ||
            p.Outputs |> Seq.exists (fun (n: IONode) -> n.Key() = key))

    /// All distinct IONodes that appear anywhere in this path (inputs or outputs)
    member _.Nodes() : ResizeArray<IONode> =
        let acc  = ResizeArray<IONode>()
        let seen = HashSet<string>()
        for proc in processes do
            for n in proc.Inputs  do if seen.Add(n.Key()) then acc.Add(n)
            for n in proc.Outputs do if seen.Add(n.Key()) then acc.Add(n)
        acc

    /// All distinct Material nodes in this path
    member this.Materials() : ResizeArray<Material> =
        let acc = ResizeArray<Material>()
        for node in this.Nodes() do
            match node with
            | MaterialNode m -> acc.Add(m)
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
            for n in proc.Outputs do outputKeys.Add(n.Key()) |> ignore
        let acc  = ResizeArray<IONode>()
        let seen = HashSet<string>()
        for proc in processes do
            for n in proc.Inputs do
                let k = n.Key()
                if not (outputKeys.Contains(k)) && seen.Add(k) then acc.Add(n)
        acc

    /// Nodes that appear as outputs but never as inputs in this path (true sinks)
    member _.TerminalOutputs() : ResizeArray<IONode> =
        let inputKeys = HashSet<string>()
        for proc in processes do
            for n in proc.Inputs do inputKeys.Add(n.Key()) |> ignore
        let acc  = ResizeArray<IONode>()
        let seen = HashSet<string>()
        for proc in processes do
            for n in proc.Outputs do
                let k = n.Key()
                if not (inputKeys.Contains(k)) && seen.Add(k) then acc.Add(n)
        acc

    /// All PropertyValues from all sources (parameters, input/output node properties, protocol components)
    /// across all processes in this path
    member _.AllPropertyValues() : ResizeArray<PropertyValue> =
        collectPropertyValuesFromProcesses processes

    /// All PropertyValues from all sources whose name matches the given string
    member this.PropertyValuesByName(name: string) : ResizeArray<PropertyValue> =
        this.AllPropertyValues() |> Seq.filter (fun pv -> pv.Name = name) |> ResizeArray

    /// All FormalParameters defined on protocols executed by processes in this path
    member _.ProtocolParameters() : ResizeArray<FormalParameter> =
        let acc  = ResizeArray<FormalParameter>()
        let seen = HashSet<string>()
        for proc in processes do
            match proc.ExecutesProtocol with
            | Some (proto: LabProtocol) ->
                for fp: FormalParameter in proto.Parameters do
                    if seen.Add(fp.Name) then acc.Add(fp)
            | None -> ()
        acc

[<AutoOpen>]
module private PathTraversal =
    let propertyValueKey (pv: PropertyValue) =
        pv.Name + "|" + (pv.Value |> Option.defaultValue "") + "|" + (pv.NameTAN |> Option.defaultValue "")

    let addPropertyValue (result: ResizeArray<PropertyValue>) (seen: HashSet<string>) (pv: PropertyValue) =
        if seen.Add(propertyValueKey pv) then result.Add(pv)

    let addPropertyValuesFromNode (result: ResizeArray<PropertyValue>) (seen: HashSet<string>) (node: IONode) =
        match node with
        | MaterialNode m -> for pv: PropertyValue in m.AdditionalProperty do addPropertyValue result seen pv
        | DataNode d     -> for pv: PropertyValue in d.AdditionalProperty do addPropertyValue result seen pv

    let addProcessPropertyValues (result: ResizeArray<PropertyValue>) (seen: HashSet<string>) (proc: LabProcess) =
        for pv: PropertyValue in proc.ParameterValue do addPropertyValue result seen pv
        match proc.ExecutesProtocol with
        | Some (proto: LabProtocol) -> for pv: PropertyValue in proto.LabEquipment do addPropertyValue result seen pv
        | None -> ()

    let addPropertyValuesFromProcess (result: ResizeArray<PropertyValue>) (seen: HashSet<string>) (proc: LabProcess) =
        addProcessPropertyValues result seen proc
        for n: IONode in proc.Inputs do addPropertyValuesFromNode result seen n
        for n: IONode in proc.Outputs do addPropertyValuesFromNode result seen n

    let collectPropertyValuesFromProcesses (processes: seq<LabProcess>) : ResizeArray<PropertyValue> =
        let result = ResizeArray<PropertyValue>()
        let seen = HashSet<string>()
        for proc in processes do
            addPropertyValuesFromProcess result seen proc
        result

    let collectPropertyValuesFromProcessesWithProtocolName (protocolName: string option) (processes: seq<LabProcess>) : ResizeArray<PropertyValue> =
        let includeProcess (proc: LabProcess) =
            match protocolName with
            | Some pn ->
                match proc.ExecutesProtocol with
                | Some (proto: LabProtocol) -> proto.Name = Some pn
                | None -> false
            | None -> true

        processes
        |> Seq.filter includeProcess
        |> collectPropertyValuesFromProcesses

    let processMatchesProtocolName (protocolName: string option) (proc: LabProcess) =
        match protocolName with
        | Some pn ->
            match proc.ExecutesProtocol with
            | Some (proto: LabProtocol) -> proto.Name = Some pn
            | None -> false
        | None -> true

    let inScope (processes: ResizeArray<LabProcess>) (p: LabProcess) =
        processes |> Seq.exists (fun q -> q = p)

    let processRefKey (proc: LabProcess) =
        System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(proc).ToString()

    let inOptionalScope (scope: ResizeArray<LabProcess> option) (p: LabProcess) =
        scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> q = p))

    let dataRelatedForTraversal (tryGetProvider: string -> IFragmentSelectorProvider option) (a: Data) (b: Data) =
        match FragmentSelectorResolution.relateDataWith tryGetProvider a b with
        | Exact | Contains -> true
        | Disjoint | Unknown ->
            match FragmentSelectorResolution.relateDataWith tryGetProvider b a with
            | Exact | Contains -> true
            | Disjoint | Unknown -> false

    let nodeRelatedForTraversal (tryGetProvider: string -> IFragmentSelectorProvider option) (a: IONode) (b: IONode) =
        match a, b with
        | MaterialNode ma, MaterialNode mb -> ma = mb
        | DataNode da, DataNode db -> dataRelatedForTraversal tryGetProvider da db
        | _ -> false

    let processesFromExactBackEdges (node: IONode) =
        let acc = ResizeArray<LabProcess>()
        acc.AddRange(node.GetInputOf())
        acc.AddRange(node.GetOutputOf())
        acc

    let processUniverse (scope: ResizeArray<LabProcess> option) (node: IONode) =
        match scope with
        | Some s -> s
        | None -> processesFromExactBackEdges node

    let providerLookupFromProcesses (processes: seq<LabProcess>) =
        let dataset: Dataset option =
            processes
            |> Seq.tryPick (fun proc -> proc.ProcessOf)

        fun selectorFormat ->
            dataset
            |> Option.bind (fun ds -> ds.TryGetFragmentSelectorProvider selectorFormat)

    let distinctProcessEdges (edges: seq<LabProcess * IONode>) =
        let seen = HashSet<string>()
        let acc = ResizeArray<LabProcess * IONode>()
        for proc, matchedNode in edges do
            let key = processRefKey proc + "|" + matchedNode.Key()
            if seen.Add(key) then acc.Add(proc, matchedNode)
        acc

    let relatedInputEdges (scope: ResizeArray<LabProcess> option) (node: IONode) =
        let universe = processUniverse scope node
        let tryGetProvider = providerLookupFromProcesses universe
        universe
        |> Seq.collect (fun proc ->
            proc.Inputs
            |> Seq.filter (nodeRelatedForTraversal tryGetProvider node)
            |> Seq.map (fun matchedNode -> proc, matchedNode))
        |> distinctProcessEdges

    let relatedOutputEdges (scope: ResizeArray<LabProcess> option) (node: IONode) =
        let universe = processUniverse scope node
        let tryGetProvider = providerLookupFromProcesses universe
        universe
        |> Seq.collect (fun proc ->
            proc.Outputs
            |> Seq.filter (nodeRelatedForTraversal tryGetProvider node)
            |> Seq.map (fun matchedNode -> proc, matchedNode))
        |> distinctProcessEdges

    let relatedNodeProcesses (scope: ResizeArray<LabProcess> option) (node: IONode) =
        Seq.append (relatedInputEdges scope node) (relatedOutputEdges scope node)
        |> Seq.map fst
        |> Seq.distinct
        |> ResizeArray

    let processesForNode (processes: ResizeArray<LabProcess>) (node: IONode) : ResizeArray<LabProcess> =
        relatedNodeProcesses (Some processes) node

    let collectUpstreamPropertyValues
        (protocolName: string option)
        (scope: ResizeArray<LabProcess> option)
        (start: IONode)
        : ResizeArray<PropertyValue> =

        let result = ResizeArray<PropertyValue>()
        let seenPV = HashSet<string>()
        let visitedEdges = HashSet<string>()

        let rec walk (node: IONode) =
            for proc, matchedNode in relatedOutputEdges scope node do
                if inOptionalScope scope proc then
                    let edgeKey = processRefKey proc + "<-" + matchedNode.Key()
                    if visitedEdges.Add(edgeKey) then
                        let includeProcess = processMatchesProtocolName protocolName proc
                        for input in proc.GetInputsOfOutput(matchedNode) do
                            walk input
                            if includeProcess then addPropertyValuesFromNode result seenPV input
                        if includeProcess then
                            addProcessPropertyValues result seenPV proc
                            addPropertyValuesFromNode result seenPV matchedNode

        walk start
        result

    let collectDownstreamPropertyValues
        (protocolName: string option)
        (scope: ResizeArray<LabProcess> option)
        (start: IONode)
        : ResizeArray<PropertyValue> =

        let result = ResizeArray<PropertyValue>()
        let seenPV = HashSet<string>()
        let visitedEdges = HashSet<string>()

        let rec walk (node: IONode) =
            for proc, matchedNode in relatedInputEdges scope node do
                if inOptionalScope scope proc then
                    let edgeKey = processRefKey proc + "->" + matchedNode.Key()
                    if visitedEdges.Add(edgeKey) then
                        let includeProcess = processMatchesProtocolName protocolName proc
                        if includeProcess then
                            addPropertyValuesFromNode result seenPV matchedNode
                            addProcessPropertyValues result seenPV proc
                        for output in proc.GetOutputsOfInput(matchedNode) do
                            if includeProcess then addPropertyValuesFromNode result seenPV output
                            walk output

        walk start
        result

    let rec walkUpstream (processes: ResizeArray<LabProcess>) (proc: LabProcess) (visited: HashSet<string>) : ResizeArray<ResizeArray<LabProcess>> =
        if not (visited.Add(proc.Name)) then
            ResizeArray([ ResizeArray() ])
        else
            let preds =
                proc.Inputs
                |> Seq.collect (fun node -> relatedOutputEdges (Some processes) node |> Seq.map fst)
                |> Seq.filter (inScope processes)
                |> Seq.distinct
                |> ResizeArray
            if preds.Count = 0 then
                ResizeArray([ ResizeArray() ])
            else
                let results = ResizeArray()
                for pred in preds do
                    for chain in walkUpstream processes pred (HashSet(visited)) do
                        let ext = ResizeArray(chain)
                        ext.Add(pred)
                        results.Add(ext)
                results

    let rec walkDownstream (processes: ResizeArray<LabProcess>) (proc: LabProcess) (visited: HashSet<string>) : ResizeArray<ResizeArray<LabProcess>> =
        if not (visited.Add(proc.Name)) then
            ResizeArray([ ResizeArray() ])
        else
            let succs =
                proc.Outputs
                |> Seq.collect (fun node -> relatedInputEdges (Some processes) node |> Seq.map fst)
                |> Seq.filter (inScope processes)
                |> Seq.distinct
                |> ResizeArray
            if succs.Count = 0 then
                ResizeArray([ ResizeArray() ])
            else
                let results = ResizeArray()
                for succ in succs do
                    for chain in walkDownstream processes succ (HashSet(visited)) do
                        let ext = ResizeArray()
                        ext.Add(succ)
                        ext.AddRange(chain)
                        results.Add(ext)
                results

    let extendToMaximalPaths (processes: ResizeArray<LabProcess>) (proc: LabProcess) : ResizeArray<Path> =
        let upstream   = walkUpstream processes proc (HashSet())
        let downstream = walkDownstream processes proc (HashSet())
        let results = ResizeArray()
        for pre in upstream do
            for post in downstream do
                let chain = ResizeArray()
                chain.AddRange(pre)
                chain.Add(proc)
                chain.AddRange(post)
                results.Add(Path(chain))
        results

    let pathsThrough (processes: ResizeArray<LabProcess>) (node: IONode) : ResizeArray<Path> =
        let results = ResizeArray()
        for seed in processesForNode processes node do
            results.AddRange(extendToMaximalPaths processes seed)
        results

// ─────────────────────────────────────────────────────────────────────────────
// IONode discriminated union (forward-declared via namespace rec)
// ─────────────────────────────────────────────────────────────────────────────

/// An IONode is either a Material or a Data node.
type IONode =
    | MaterialNode of Material
    | DataNode of Data

    member this.EqualTo(other: IONode) =
        match this, other with
        | MaterialNode a, MaterialNode b -> a = b
        | DataNode a,     DataNode b     -> a = b
        | _ -> false

    /// Stable string key used for identity checks and HashSet deduplication.
    member this.Key() =
        match this with
        | MaterialNode m -> "M:" + m.Name
        | DataNode d     -> "D:" + d.Path + (d.Selector |> Option.defaultValue "")

    /// Processes for which this node is an input (forward back-edges).
    member this.GetInputOf() : HashSet<LabProcess> =
        match this with
        | MaterialNode m -> m.InputOf
        | DataNode d     -> d.InputOf

    /// Processes for which this node is an output (backward back-edges).
    member this.GetOutputOf() : HashSet<LabProcess> =
        match this with
        | MaterialNode m -> m.OutputOf
        | DataNode d     -> d.OutputOf

    // ── Graph traversal ───────────────────────────────────────────────────────

    /// All processes reachable from this node by BFS through both upstream
    /// and downstream edges. Optional `scope` restricts traversal to the given
    /// process list (e.g. a single Dataset).
    member this.AllConnectedProcesses(?scope: ResizeArray<LabProcess>) : ResizeArray<LabProcess> =
        let inScope (p: LabProcess) =
            scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> q = p))
        let seenN  = HashSet<string>()
        let seenP  = HashSet<string>()
        let result = ResizeArray<LabProcess>()
        seenN.Add(this.Key()) |> ignore
        let mutable frontier = ResizeArray<IONode>([| this |])
        while frontier.Count > 0 do
            let next = ResizeArray<IONode>()
            for node in frontier do
                for p: LabProcess in relatedNodeProcesses scope node do
                    if inScope p && seenP.Add(p.Name) then
                        result.Add(p)
                        for n: IONode in Seq.append p.Inputs p.Outputs do
                            if seenN.Add(n.Key()) then next.Add(n)
            frontier <- next
        result

    /// All in-scope processes in which this node appears as an input or output.
    member this.Processes(?scope: ResizeArray<LabProcess>) : ResizeArray<LabProcess> =
        let scope =
            match scope with
            | Some s -> s
            | None   -> this.AllConnectedProcesses()
        processesForNode scope this

    /// All maximal Paths that pass through this node within the given process scope.
    member this.PathsThrough(?scope: ResizeArray<LabProcess>) : ResizeArray<Path> =
        let scope =
            match scope with
            | Some s -> s
            | None   -> this.AllConnectedProcesses()
        pathsThrough scope this

    /// All IONodes connected to this node through the process graph
    /// (union of all upstream and downstream neighbours), excluding this node itself.
    member this.AllConnectedNodes(?scope: ResizeArray<LabProcess>) : ResizeArray<IONode> =
        let inScope (p: LabProcess) =
            scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> q = p))
        let seenN  = HashSet<string>()
        seenN.Add(this.Key()) |> ignore
        let result = ResizeArray<IONode>()
        let mutable frontier = ResizeArray<IONode>([| this |])
        while frontier.Count > 0 do
            let next = ResizeArray<IONode>()
            for node in frontier do
                for p: LabProcess in relatedNodeProcesses scope node do
                    if inScope p then
                        for n: IONode in Seq.append p.Inputs p.Outputs do
                            if seenN.Add(n.Key()) then
                                result.Add(n)
                                next.Add(n)
            frontier <- next
        result

    /// All PropertyValues from all sources (parameters, input/output node properties, protocol components)
    /// connected to this node through the graph.
    member this.AllPropertyValues(?scope: ResizeArray<LabProcess>) : ResizeArray<PropertyValue> =
        let result = ResizeArray<PropertyValue>()
        let seen = HashSet<string>()
        for pv in collectUpstreamPropertyValues None scope this do
            addPropertyValue result seen pv
        for pv in collectDownstreamPropertyValues None scope this do
            addPropertyValue result seen pv
        result

    /// All FormalParameters from protocols executed in processes connected to this node.
    member this.ProtocolParameters(?scope: ResizeArray<LabProcess>) : ResizeArray<FormalParameter> =
        let seen   = HashSet<string>()
        let result = ResizeArray<FormalParameter>()
        for p: LabProcess in this.AllConnectedProcesses(?scope = scope) do
            match p.ExecutesProtocol with
            | Some (proto: LabProtocol) ->
                for fp: FormalParameter in proto.Parameters do
                    if seen.Add(fp.Name) then result.Add(fp)
            | None -> ()
        result

    /// All Material nodes connected to this node through the graph.
    member this.ConnectedMaterials(?scope: ResizeArray<LabProcess>) : ResizeArray<Material> =
        let result = ResizeArray<Material>()
        for n in this.AllConnectedNodes(?scope = scope) do
            match n with
            | MaterialNode m -> result.Add(m)
            | _ -> ()
        result

    /// All Data nodes connected to this node through the graph.
    member this.ConnectedData(?scope: ResizeArray<LabProcess>) : ResizeArray<Data> =
        let result = ResizeArray<Data>()
        for n in this.AllConnectedNodes(?scope = scope) do
            match n with
            | DataNode d -> result.Add(d)
            | _ -> ()
        result

    // ── Directional traversal ─────────────────────────────────────────────────

    /// True if no in-scope process produces this node as output (no predecessors in scope).
    member this.IsRootNode(?scope: ResizeArray<LabProcess>) : bool =
        match scope with
        | Some s  -> 

            let inScope (p: LabProcess) =
                scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> q = p))
            relatedOutputEdges scope this |> Seq.exists (fun (p, _) -> inScope p) |> not
        | None ->
            this.GetOutputOf().Count = 0

    /// True if no in-scope process consumes this node as input (no successors in scope).
    member this.IsFinalNode(?scope: ResizeArray<LabProcess>) : bool =
        match scope with
        | Some s  -> 
            let inScope (p: LabProcess) =
                scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> q = p))
            relatedInputEdges scope this |> Seq.exists (fun (p, _) -> inScope p) |> not
        | None ->
            this.GetInputOf().Count = 0

    /// All processes reachable by walking upstream (OutputOf edges → Inputs).
    member this.UpstreamProcesses(?scope: ResizeArray<LabProcess>) : ResizeArray<LabProcess> =
        let inScope (p: LabProcess) =
            scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> q = p))
        let seenN = HashSet<string>()
        let seenP = HashSet<string>()
        let result = ResizeArray<LabProcess>()
        seenN.Add(this.Key()) |> ignore
        let mutable frontier = ResizeArray<IONode>([| this |])
        while frontier.Count > 0 do
            let next = ResizeArray<IONode>()
            for node in frontier do
                for p, matchedNode in relatedOutputEdges scope node do
                    if inScope p && seenP.Add(p.Name) then
                        result.Add(p)
                        for n: IONode in p.GetInputsOfOutput(matchedNode) do
                            if seenN.Add(n.Key()) then next.Add(n)
            frontier <- next
        result

    /// All processes reachable by walking downstream (InputOf edges → Outputs).
    member this.DownstreamProcesses(?scope: ResizeArray<LabProcess>) : ResizeArray<LabProcess> =
        let inScope (p: LabProcess) =
            scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> q = p))
        let seenN = HashSet<string>()
        let seenP = HashSet<string>()
        let result = ResizeArray<LabProcess>()
        seenN.Add(this.Key()) |> ignore
        let mutable frontier = ResizeArray<IONode>([| this |])
        while frontier.Count > 0 do
            let next = ResizeArray<IONode>()
            for node in frontier do
                for p, matchedNode in relatedInputEdges scope node do
                    if inScope p && seenP.Add(p.Name) then
                        result.Add(p)
                        for n: IONode in p.GetOutputsOfInput(matchedNode) do
                            if seenN.Add(n.Key()) then next.Add(n)
            frontier <- next
        result

    /// All IONodes reachable by walking upstream from this node.
    /// When a process has equal numbers of inputs and outputs the Nth output
    /// corresponds to the Nth input (positional N-to-N mapping). Falls back to
    /// all inputs when counts differ.
    member this.UpstreamNodes(?scope: ResizeArray<LabProcess>) : ResizeArray<IONode> =
        let inScope (p: LabProcess) =
            scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> q = p))
        let seenN = HashSet<string>()
        seenN.Add(this.Key()) |> ignore
        let result = ResizeArray<IONode>()
        let mutable frontier = ResizeArray<IONode>([| this |])
        while frontier.Count > 0 do
            let next = ResizeArray<IONode>()
            for node in frontier do
                for p, matchedNode in relatedOutputEdges scope node do
                    if inScope p then
                        for n: IONode in p.GetInputsOfOutput(matchedNode) do
                            if seenN.Add(n.Key()) then
                                result.Add(n)
                                next.Add(n)
            frontier <- next
        result

    /// All IONodes reachable by walking downstream from this node.
    /// When a process has equal numbers of inputs and outputs the Nth input
    /// corresponds to the Nth output (positional N-to-N mapping). Falls back to
    /// all outputs when counts differ.
    member this.DownstreamNodes(?scope: ResizeArray<LabProcess>) : ResizeArray<IONode> =
        let inScope (p: LabProcess) =
            scope |> Option.forall (fun s -> s |> Seq.exists (fun q -> q = p))
        let seenN = HashSet<string>()
        seenN.Add(this.Key()) |> ignore
        let result = ResizeArray<IONode>()
        let mutable frontier = ResizeArray<IONode>([| this |])
        while frontier.Count > 0 do
            let next = ResizeArray<IONode>()
            for node in frontier do
                for p, matchedNode in relatedInputEdges scope node do
                    if inScope p then
                        for n: IONode in p.GetOutputsOfInput(matchedNode) do
                            if seenN.Add(n.Key()) then
                                result.Add(n)
                                next.Add(n)
            frontier <- next
        result

    /// Upstream nodes that have no predecessors within scope (terminal sources).
    member this.RootNodes(?scope: ResizeArray<LabProcess>) : ResizeArray<IONode> =
        this.UpstreamNodes(?scope = scope)
        |> Seq.filter (fun n -> n.IsRootNode(?scope = scope))
        |> ResizeArray

    /// Downstream nodes that have no successors within scope (terminal sinks).
    member this.FinalNodes(?scope: ResizeArray<LabProcess>) : ResizeArray<IONode> =
        this.DownstreamNodes(?scope = scope)
        |> Seq.filter (fun n -> n.IsFinalNode(?scope = scope))
        |> ResizeArray

    /// All Material nodes reachable upstream from this node.
    member this.UpstreamMaterials(?scope: ResizeArray<LabProcess>) : ResizeArray<Material> =
        let result = ResizeArray<Material>()
        for n in this.UpstreamNodes(?scope = scope) do
            match n with
            | MaterialNode m -> result.Add(m)
            | _ -> ()
        result

    /// All Material nodes reachable downstream from this node.
    member this.DownstreamMaterials(?scope: ResizeArray<LabProcess>) : ResizeArray<Material> =
        let result = ResizeArray<Material>()
        for n in this.DownstreamNodes(?scope = scope) do
            match n with
            | MaterialNode m -> result.Add(m)
            | _ -> ()
        result

    /// All Data nodes reachable upstream from this node.
    member this.UpstreamData(?scope: ResizeArray<LabProcess>) : ResizeArray<Data> =
        let result = ResizeArray<Data>()
        for n in this.UpstreamNodes(?scope = scope) do
            match n with
            | DataNode d -> result.Add(d)
            | _ -> ()
        result

    /// All Data nodes reachable downstream from this node.
    member this.DownstreamData(?scope: ResizeArray<LabProcess>) : ResizeArray<Data> =
        let result = ResizeArray<Data>()
        for n in this.DownstreamNodes(?scope = scope) do
            match n with
            | DataNode d -> result.Add(d)
            | _ -> ()
        result

    /// PropertyValues from all sources in processes upstream of this node.
    /// Optional protocolName restricts to processes whose protocol name matches.
    member this.UpstreamPropertyValues(?protocolName: string, ?scope: ResizeArray<LabProcess>) : ResizeArray<PropertyValue> =
        collectUpstreamPropertyValues protocolName scope this

    /// PropertyValues from all sources in processes downstream of this node.
    /// Optional protocolName restricts to processes whose protocol name matches.
    member this.DownstreamPropertyValues(?protocolName: string, ?scope: ResizeArray<LabProcess>) : ResizeArray<PropertyValue> =
        collectDownstreamPropertyValues protocolName scope this

// ─────────────────────────────────────────────────────────────────────────────
// Material
// ─────────────────────────────────────────────────────────────────────────────

/// Input or output biological, chemical, or digital material in the process graph.
/// bioschemas.org/Sample
and [<AttachMembers>] Material(name: string, ?additionalType: string, ?additionalProperty: seq<PropertyValue>) as this =

    inherit DynamicObj()

    let mutable _name: string = name
    let mutable _additionalType: string option = additionalType
    let _additionalProperty: ResizeArray<PropertyValue> = ResizeArray()
    let _inputOf:  HashSet<LabProcess> = HashSet(refEqProcess)
    let _outputOf: HashSet<LabProcess> = HashSet(refEqProcess)

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

    /// Processes for which this material is an input (back-edge)
    member _.InputOf: HashSet<LabProcess> = _inputOf

    /// Processes for which this material is an output (back-edge)
    member _.OutputOf: HashSet<LabProcess> = _outputOf

    member this.AddAdditionalProperty(pv: PropertyValue) =
        if not (_additionalProperty |> Seq.exists (fun x -> x = pv)) then
            _additionalProperty.Add(pv)

    member _.RemoveAdditionalProperty(pv: PropertyValue) =
        _additionalProperty.Remove(pv) |> ignore

    // ── Query ─────────────────────────────────────────────────────────────────

    /// All IONodes connected to this material through the graph.
    member this.AllConnectedNodes(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).AllConnectedNodes(?scope = scope)

    /// All processes connected to this material through the graph.
    member this.AllConnectedProcesses(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).AllConnectedProcesses(?scope = scope)

    /// All in-scope processes in which this material appears as an input or output.
    member this.Processes(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).Processes(?scope = scope)

    /// All maximal Paths that pass through this material.
    member this.PathsThrough(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).PathsThrough(?scope = scope)

    /// All PropertyValues from all sources connected to this material through the graph.
    member this.AllPropertyValues(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).AllPropertyValues(?scope = scope)

    /// All FormalParameters from protocols connected to this material.
    member this.ProtocolParameters(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).ProtocolParameters(?scope = scope)

    /// All Material nodes connected to this material through the graph.
    member this.ConnectedMaterials(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).ConnectedMaterials(?scope = scope)

    /// All Data nodes connected to this material through the graph.
    member this.ConnectedData(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).ConnectedData(?scope = scope)

    // ── Directional traversal ─────────────────────────────────────────────────

    member this.IsRootNode(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).IsRootNode(?scope = scope)

    member this.IsFinalNode(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).IsFinalNode(?scope = scope)

    member this.UpstreamProcesses(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).UpstreamProcesses(?scope = scope)

    member this.DownstreamProcesses(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).DownstreamProcesses(?scope = scope)

    member this.UpstreamNodes(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).UpstreamNodes(?scope = scope)

    member this.DownstreamNodes(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).DownstreamNodes(?scope = scope)

    member this.RootNodes(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).RootNodes(?scope = scope)

    member this.FinalNodes(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).FinalNodes(?scope = scope)

    member this.UpstreamMaterials(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).UpstreamMaterials(?scope = scope)

    member this.DownstreamMaterials(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).DownstreamMaterials(?scope = scope)

    member this.UpstreamData(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).UpstreamData(?scope = scope)

    member this.DownstreamData(?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).DownstreamData(?scope = scope)

    member this.UpstreamPropertyValues(?protocolName: string, ?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).UpstreamPropertyValues(?protocolName = protocolName, ?scope = scope)

    member this.DownstreamPropertyValues(?protocolName: string, ?scope: ResizeArray<LabProcess>) =
        (MaterialNode this).DownstreamPropertyValues(?protocolName = protocolName, ?scope = scope)

    /// Two Materials with the same name are identical across datasets.
    override this.Equals(obj) =
        match obj with
        | :? Material as other -> this.Name = other.Name
        | _ -> false

    override this.GetHashCode() = hash this.Name

// ─────────────────────────────────────────────────────────────────────────────
// Data
// ─────────────────────────────────────────────────────────────────────────────

/// Data file produced or consumed by processes.
/// schema.org/MediaObject or File
and [<AttachMembers>] Data(path: string, ?selector: string, ?selectorFormat: string, ?encodingFormat: string, ?additionalType: string, ?additionalProperty: seq<PropertyValue>) as this =

    inherit DynamicObj()

    let mutable _path: string = path
    let mutable _selector: string option = selector
    let mutable _selectorFormat: string option = selectorFormat
    let mutable _encodingFormat: string option = encodingFormat
    let mutable _additionalType: string option = additionalType
    let _additionalProperty: ResizeArray<PropertyValue> = ResizeArray()
    let _inputOf:  HashSet<LabProcess> = HashSet(refEqProcess)
    let _outputOf: HashSet<LabProcess> = HashSet(refEqProcess)

    do
        additionalProperty |> Option.iter (fun pvs -> for pv in pvs do this.AddAdditionalProperty(pv))

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

    member _.AdditionalProperty = _additionalProperty

    /// Processes for which this data node is an input (back-edge)
    member _.InputOf: HashSet<LabProcess> = _inputOf

    /// Processes for which this data node is an output (back-edge)
    member _.OutputOf: HashSet<LabProcess> = _outputOf

    member this.AddAdditionalProperty(pv: PropertyValue) =
        if not (_additionalProperty |> Seq.exists (fun x -> x = pv)) then
            _additionalProperty.Add(pv)

    member _.RemoveAdditionalProperty(pv: PropertyValue) =
        _additionalProperty.Remove(pv) |> ignore

    // ── Query ─────────────────────────────────────────────────────────────────

    /// All IONodes connected to this data node through the graph.
    member this.AllConnectedNodes(?scope: ResizeArray<LabProcess>) =
        (DataNode this).AllConnectedNodes(?scope = scope)

    /// All processes connected to this data node through the graph.
    member this.AllConnectedProcesses(?scope: ResizeArray<LabProcess>) =
        (DataNode this).AllConnectedProcesses(?scope = scope)

    /// All in-scope processes in which this data node appears as an input or output.
    member this.Processes(?scope: ResizeArray<LabProcess>) =
        (DataNode this).Processes(?scope = scope)

    /// All maximal Paths that pass through this data node.
    member this.PathsThrough(?scope: ResizeArray<LabProcess>) =
        (DataNode this).PathsThrough(?scope = scope)

    /// All PropertyValues from all sources connected to this data node through the graph.
    member this.AllPropertyValues(?scope: ResizeArray<LabProcess>) =
        (DataNode this).AllPropertyValues(?scope = scope)

    /// All FormalParameters from protocols connected to this data node.
    member this.ProtocolParameters(?scope: ResizeArray<LabProcess>) =
        (DataNode this).ProtocolParameters(?scope = scope)

    /// All Material nodes connected to this data node through the graph.
    member this.ConnectedMaterials(?scope: ResizeArray<LabProcess>) =
        (DataNode this).ConnectedMaterials(?scope = scope)

    /// All Data nodes connected to this data node through the graph.
    member this.ConnectedData(?scope: ResizeArray<LabProcess>) =
        (DataNode this).ConnectedData(?scope = scope)

    // ── Directional traversal ─────────────────────────────────────────────────

    member this.IsRootNode(?scope: ResizeArray<LabProcess>) =
        (DataNode this).IsRootNode(?scope = scope)

    member this.IsFinalNode(?scope: ResizeArray<LabProcess>) =
        (DataNode this).IsFinalNode(?scope = scope)

    member this.UpstreamProcesses(?scope: ResizeArray<LabProcess>) =
        (DataNode this).UpstreamProcesses(?scope = scope)

    member this.DownstreamProcesses(?scope: ResizeArray<LabProcess>) =
        (DataNode this).DownstreamProcesses(?scope = scope)

    member this.UpstreamNodes(?scope: ResizeArray<LabProcess>) =
        (DataNode this).UpstreamNodes(?scope = scope)

    member this.DownstreamNodes(?scope: ResizeArray<LabProcess>) =
        (DataNode this).DownstreamNodes(?scope = scope)

    member this.RootNodes(?scope: ResizeArray<LabProcess>) =
        (DataNode this).RootNodes(?scope = scope)

    member this.FinalNodes(?scope: ResizeArray<LabProcess>) =
        (DataNode this).FinalNodes(?scope = scope)

    member this.UpstreamMaterials(?scope: ResizeArray<LabProcess>) =
        (DataNode this).UpstreamMaterials(?scope = scope)

    member this.DownstreamMaterials(?scope: ResizeArray<LabProcess>) =
        (DataNode this).DownstreamMaterials(?scope = scope)

    member this.UpstreamData(?scope: ResizeArray<LabProcess>) =
        (DataNode this).UpstreamData(?scope = scope)

    member this.DownstreamData(?scope: ResizeArray<LabProcess>) =
        (DataNode this).DownstreamData(?scope = scope)

    member this.UpstreamPropertyValues(?protocolName: string, ?scope: ResizeArray<LabProcess>) =
        (DataNode this).UpstreamPropertyValues(?protocolName = protocolName, ?scope = scope)

    member this.DownstreamPropertyValues(?protocolName: string, ?scope: ResizeArray<LabProcess>) =
        (DataNode this).DownstreamPropertyValues(?protocolName = protocolName, ?scope = scope)

    /// Two Data nodes with the same path and selector are identical.
    override this.Equals(obj) =
        match obj with
        | :? Data as other -> this.Path = other.Path && this.Selector = other.Selector
        | _ -> false

    override this.GetHashCode() = hash (this.Path, this.Selector)

// ─────────────────────────────────────────────────────────────────────────────
// LabProtocol
// ─────────────────────────────────────────────────────────────────────────────

/// Description of a planned procedure.
/// bioschemas.org/LabProtocol
and [<AttachMembers>] LabProtocol(?name: string, ?description: string, ?version: string, ?url: string, ?intendedUse: DefinedTerm, ?additionalType: string, ?parameters: seq<FormalParameter>, ?labEquipment: seq<PropertyValue>, ?additionalProperty: seq<PropertyValue>) as this =

    inherit DynamicObj()

    let mutable _name: string option = name
    let mutable _description: string option = description
    let mutable _version: string option = version
    let mutable _url: string option = url
    let mutable _intendedUse: DefinedTerm option = intendedUse
    let mutable _additionalType: string option = additionalType
    let _parameters: ResizeArray<FormalParameter> = ResizeArray()
    let _labEquipment: ResizeArray<PropertyValue> = ResizeArray()
    let _additionalProperty: ResizeArray<PropertyValue> = ResizeArray()

    do
        parameters         |> Option.iter (fun fps -> for fp in fps do this.AddParameter(fp))
        labEquipment       |> Option.iter (fun pvs -> for pv in pvs do this.AddLabEquipment(pv))
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

    /// Equipment, reagents, and software used in this protocol (components).
    member _.LabEquipment = _labEquipment

    member _.AdditionalProperty = _additionalProperty

    member this.AddParameter(fp: FormalParameter) =
        if not (_parameters |> Seq.exists (fun x -> x = fp)) then
            _parameters.Add(fp)

    member _.RemoveParameter(fp: FormalParameter) =
        _parameters.Remove(fp) |> ignore

    member _.TryGetParameter(name: string) =
        _parameters |> Seq.tryFind (fun fp -> fp.Name = name)

    member this.AddLabEquipment(pv: PropertyValue) =
        if not (_labEquipment |> Seq.exists (fun x -> x = pv)) then
            _labEquipment.Add(pv)

    member _.RemoveLabEquipment(pv: PropertyValue) =
        _labEquipment.Remove(pv) |> ignore

    member this.AddAdditionalProperty(pv: PropertyValue) =
        if not (_additionalProperty |> Seq.exists (fun x -> x = pv)) then
            _additionalProperty.Add(pv)

    member _.RemoveAdditionalProperty(pv: PropertyValue) =
        _additionalProperty.Remove(pv) |> ignore

    override this.Equals(obj) =
        match obj with
        | :? LabProtocol as other -> this.Name = other.Name && this.Version = other.Version
        | _ -> false

    override this.GetHashCode() = hash (this.Name, this.Version)

// ─────────────────────────────────────────────────────────────────────────────
// LabProcess
// ─────────────────────────────────────────────────────────────────────────────

/// Core transformation node. Connects inputs to outputs via a protocol.
/// bioschemas.org/LabProcess
and [<AttachMembers>] LabProcess(name: string, ?executesProtocol: LabProtocol, ?additionalType: string, ?inputs: seq<IONode>, ?outputs: seq<IONode>, ?parameterValue: seq<PropertyValue>) as this =

    inherit DynamicObj()

    let mutable _name: string = name
    let mutable _executesProtocol: LabProtocol option = executesProtocol
    let mutable _additionalType: string option = additionalType
    let mutable _processOf: Dataset option = None
    let _inputs: ResizeArray<IONode> = ResizeArray()
    let _outputs: ResizeArray<IONode> = ResizeArray()
    let _parameterValue: ResizeArray<PropertyValue> = ResizeArray()

    // ── Internal back-edge helpers ────────────────────────────────────────────

    let addInputBackEdge (node: IONode) (proc: LabProcess) =
        match node with
        | MaterialNode m ->
            m.InputOf.Add(proc) |> ignore
        | DataNode d ->
            d.InputOf.Add(proc) |> ignore

    let removeInputBackEdge (node: IONode) (proc: LabProcess) =
        match node with
        | MaterialNode m -> m.InputOf.Remove(proc) |> ignore
        | DataNode d     -> d.InputOf.Remove(proc) |> ignore

    let addOutputBackEdge (node: IONode) (proc: LabProcess) =
        match node with
        | MaterialNode m ->
            m.OutputOf.Add(proc) |> ignore
        | DataNode d ->
            d.OutputOf.Add(proc) |> ignore

    let removeOutputBackEdge (node: IONode) (proc: LabProcess) =
        match node with
        | MaterialNode m -> m.OutputOf.Remove(proc) |> ignore
        | DataNode d     -> d.OutputOf.Remove(proc) |> ignore

    /// Returns the canonical instance from the root dataset's registry, or the
    /// node itself if no dataset is assigned yet.
    let resolveNode (node: IONode) =
        match _processOf with
        | None    -> node
        | Some ds -> ds.CanonicalizeNode(node)

    do
        inputs         |> Option.iter (fun ns  -> for n  in ns  do this.AddInput(n))
        outputs        |> Option.iter (fun ns  -> for n  in ns  do this.AddOutput(n))
        parameterValue |> Option.iter (fun pvs -> for pv in pvs do this.AddParameterValue(pv))

    member _.Name
        with get() = _name
        and set v = _name <- v

    member _.ExecutesProtocol
        with get() = _executesProtocol
        and set v = _executesProtocol <- v

    member _.AdditionalType
        with get() = _additionalType
        and set v = _additionalType <- v

    /// Back-edge: the Dataset this process belongs to
    member _.ProcessOf
        with get() = _processOf
        and set v = _processOf <- v

    member _.Inputs = _inputs
    member _.Outputs = _outputs
    member _.ParameterValue = _parameterValue

    /// Returns the positional input peer(s) of the given output node.
    /// When this process has equal numbers of inputs and outputs the Nth output
    /// maps to the Nth input (N-to-N). Falls back to all inputs when counts differ
    /// or when the node is not found in Outputs.
    member this.GetInputsOfOutput(output: IONode) : IONode seq =
        if _inputs.Count = _outputs.Count then
            let idx = _outputs.IndexOf(output)
            if idx >= 0 then Seq.singleton _inputs.[idx]
            else _inputs :> seq<IONode>
        else _inputs :> seq<IONode>

    /// Returns the positional output peer(s) of the given input node.
    /// When this process has equal numbers of inputs and outputs the Nth input
    /// maps to the Nth output (N-to-N). Falls back to all outputs when counts differ
    /// or when the node is not found in Inputs.
    member this.GetOutputsOfInput(input: IONode) : IONode seq =
        if _inputs.Count = _outputs.Count then
            let idx = _inputs.IndexOf(input)
            if idx >= 0 then Seq.singleton _outputs.[idx]
            else _outputs :> seq<IONode>
        else _outputs :> seq<IONode>

    // ── Input CRUD ────────────────────────────────────────────────────────────

    /// Add input. Resolves the node against the root registry so back-edges are
    /// shared when an equal node already exists anywhere in the dataset hierarchy.
    member this.AddInput(node: IONode) =
        let node = resolveNode node
        _inputs.Add(node)
        addInputBackEdge node this

    /// Re-canonicalize all existing inputs and outputs against the given dataset's
    /// root registry. Called when the process is added to a dataset after its nodes
    /// were already populated. Migrates back-edges if the canonical instance differs.
    member this.CanonicalizeAllNodes(ds: Dataset) =
        for i in 0 .. _inputs.Count - 1 do
            let original  = _inputs.[i]
            let canonical = ds.CanonicalizeNode(original)
            match original, canonical with
            | MaterialNode mo, MaterialNode mc when not (obj.ReferenceEquals(mo, mc)) ->
                _inputs.[i] <- canonical
                removeInputBackEdge original this
                addInputBackEdge canonical this
            | DataNode do', DataNode dc when not (obj.ReferenceEquals(do', dc)) ->
                _inputs.[i] <- canonical
                removeInputBackEdge original this
                addInputBackEdge canonical this
            | _ -> ()
        for i in 0 .. _outputs.Count - 1 do
            let original  = _outputs.[i]
            let canonical = ds.CanonicalizeNode(original)
            match original, canonical with
            | MaterialNode mo, MaterialNode mc when not (obj.ReferenceEquals(mo, mc)) ->
                _outputs.[i] <- canonical
                removeOutputBackEdge original this
                addOutputBackEdge canonical this
            | DataNode do', DataNode dc when not (obj.ReferenceEquals(do', dc)) ->
                _outputs.[i] <- canonical
                removeOutputBackEdge original this
                addOutputBackEdge canonical this
            | _ -> ()

    member this.AddInputMaterial(m: Material) = this.AddInput(MaterialNode m)
    member this.AddInputData(d: Data)         = this.AddInput(DataNode d)

    member this.RemoveInput(node: IONode) =
        let removed = _inputs.Remove(node)
        if removed then removeInputBackEdge node this

    member this.RemoveInputMaterial(m: Material) = this.RemoveInput(MaterialNode m)
    member this.RemoveInputData(d: Data)         = this.RemoveInput(DataNode d)

    // ── Output CRUD ───────────────────────────────────────────────────────────

    /// Add output. Resolves the node against the root registry so back-edges are
    /// shared when an equal node already exists anywhere in the dataset hierarchy.
    member this.AddOutput(node: IONode) =
        let node = resolveNode node
        _outputs.Add(node)
        addOutputBackEdge node this

    member this.AddOutputMaterial(m: Material) = this.AddOutput(MaterialNode m)
    member this.AddOutputData(d: Data)         = this.AddOutput(DataNode d)

    member this.RemoveOutput(node: IONode) =
        let removed = _outputs.Remove(node)
        if removed then removeOutputBackEdge node this

    member this.RemoveOutputMaterial(m: Material) = this.RemoveOutput(MaterialNode m)
    member this.RemoveOutputData(d: Data)         = this.RemoveOutput(DataNode d)

    // ── ParameterValue CRUD ───────────────────────────────────────────────────

    member this.AddParameterValue(pv: PropertyValue) =
        //if not (_parameterValue |> Seq.exists (fun x -> x = pv)) then
        _parameterValue.Add(pv)

    member _.RemoveParameterValue(pv: PropertyValue) =
        _parameterValue.Remove(pv) |> ignore

    member _.TryGetParameterValue(name: string) =
        _parameterValue |> Seq.tryFind (fun pv -> pv.Name = name)

    member _.GetParameterValue(name: string) =
        _parameterValue |> Seq.find (fun pv -> pv.Name = name)

    // ── Query helpers ─────────────────────────────────────────────────────────

    /// Input nodes that are Materials.
    member _.InputMaterials() : ResizeArray<Material> =
        let result = ResizeArray()
        for n in _inputs do
            match n with
            | MaterialNode m -> result.Add(m)
            | _ -> ()
        result

    /// Input nodes that are Data.
    member _.InputData() : ResizeArray<Data> =
        let result = ResizeArray()
        for n in _inputs do
            match n with
            | DataNode d -> result.Add(d)
            | _ -> ()
        result

    /// Output nodes that are Materials.
    member _.OutputMaterials() : ResizeArray<Material> =
        let result = ResizeArray()
        for n in _outputs do
            match n with
            | MaterialNode m -> result.Add(m)
            | _ -> ()
        result

    /// Output nodes that are Data.
    member _.OutputData() : ResizeArray<Data> =
        let result = ResizeArray()
        for n in _outputs do
            match n with
            | DataNode d -> result.Add(d)
            | _ -> ()
        result

    /// FormalParameters defined on the protocol executed by this process.
    member _.ProtocolParameters() : ResizeArray<FormalParameter> =
        match _executesProtocol with
        | Some proto -> proto.Parameters
        | None       -> ResizeArray()

    /// PropertyValues from all sources (parameters, input/output node properties, protocol components)
    /// whose name matches the given string.
    member this.PropertyValuesByName(name: string) : ResizeArray<PropertyValue> =
        let result = ResizeArray<PropertyValue>()
        for pv: PropertyValue in _parameterValue do
            if pv.Name = name then result.Add(pv)
        for n: IONode in _inputs do
            match n with
            | MaterialNode m -> for pv: PropertyValue in m.AdditionalProperty do if pv.Name = name then result.Add(pv)
            | DataNode d     -> for pv: PropertyValue in d.AdditionalProperty do if pv.Name = name then result.Add(pv)
        for n: IONode in _outputs do
            match n with
            | MaterialNode m -> for pv: PropertyValue in m.AdditionalProperty do if pv.Name = name then result.Add(pv)
            | DataNode d     -> for pv: PropertyValue in d.AdditionalProperty do if pv.Name = name then result.Add(pv)
        match _executesProtocol with
        | Some proto -> for pv: PropertyValue in proto.LabEquipment do if pv.Name = name then result.Add(pv)
        | None -> ()
        result

    /// Identity: two processes with the same name within the same dataset are identical.
    override this.Equals(obj) =
        match obj with
        | :? LabProcess as other -> this.Name = other.Name
        | _ -> false

    override this.GetHashCode() = hash this.Name

// ─────────────────────────────────────────────────────────────────────────────
// Dataset
// ─────────────────────────────────────────────────────────────────────────────

/// Container and context for data and processes.
/// schema.org/Dataset
and [<AttachMembers>] Dataset(identifier: string, ?name: string, ?description: string, ?additionalType: string, ?processes: seq<LabProcess>, ?hasPart: seq<Dataset>, ?additionalProperty: seq<PropertyValue>) as this =

    inherit DynamicObj()

    let mutable _identifier: string = identifier
    let mutable _name: string option = name
    let mutable _description: string option = description
    let mutable _additionalType: string option = additionalType
    let mutable _partOf: Dataset option = None
    let _processes: ResizeArray<LabProcess> = ResizeArray()
    let _hasPart: ResizeArray<Dataset> = ResizeArray()
    let _additionalProperty: ResizeArray<PropertyValue> = ResizeArray()
    /// IONode registry — only meaningfully populated when this is the root dataset.
    let _nodeRegistry: Dictionary<string, IONode> = Dictionary<string, IONode>()
    let _fragmentSelectorProviders: Dictionary<string, IFragmentSelectorProvider> = Dictionary<string, IFragmentSelectorProvider>()

    do
        processes          |> Option.iter (fun ps  -> for p  in ps  do this.AddProcess(p))
        hasPart            |> Option.iter (fun ds  -> for d  in ds  do this.AddPart(d))
        additionalProperty |> Option.iter (fun pvs -> for pv in pvs do this.AddAdditionalProperty(pv))

    // ── Registry helpers ──────────────────────────────────────────────────────

    // Direct access to the backing dictionary of this dataset instance.
    member private _.NodeRegistryDirect = _nodeRegistry

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

    /// Evicts `node` from the root registry if no process remaining in the
    /// hierarchy still holds it as an input or output.
    member private this.TryEvictNode(node: IONode) =
        let root = this.RootDataset()
        let key  = node.Key()
        let stillUsed =
            Seq.append (node.GetInputOf() :> seq<LabProcess>) (node.GetOutputOf() :> seq<LabProcess>)
            |> Seq.exists (fun proc ->
                proc.ProcessOf
                |> Option.exists (fun ds -> obj.ReferenceEquals(ds.RootDataset(), root)))
        if not stillUsed then
            root.NodeRegistryDirect.Remove(key) |> ignore

    member _.Identifier
        with get() = _identifier
        and set v = _identifier <- v

    member _.Name
        with get() = _name
        and set v = _name <- v

    member _.Description
        with get() = _description
        and set v = _description <- v

    /// Decoration discriminator (e.g. "Investigation", "Study", "Assay")
    member _.AdditionalType
        with get() = _additionalType
        and set v = _additionalType <- v

    /// Back-edge: parent dataset
    member _.PartOf
        with get() = _partOf
        and set v = _partOf <- v

    member _.Processes = _processes
    member _.HasPart = _hasPart
    member _.AdditionalProperty = _additionalProperty

    // ── Process CRUD ──────────────────────────────────────────────────────────

    member this.AddProcess(proc: LabProcess) =
        if proc.ProcessOf.IsSome then
            if proc.ProcessOf.Value <> this then
                failwithf "Process '%s' already belongs to another dataset." proc.Name
        else
            _processes.Add(proc)
            proc.ProcessOf <- Some this
            // Canonicalize any nodes the process already carries against the root registry.
            proc.CanonicalizeAllNodes(this)

    member this.RemoveProcess(proc: LabProcess) =
        let removed = _processes.Remove(proc)
        if removed && proc.ProcessOf = Some this then
            proc.ProcessOf <- None
            for node in Seq.append proc.Inputs proc.Outputs do
                this.TryEvictNode(node)

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
            // Canonicalize every node in the child's subtree against the new root.
            for proc in child.AllProcesses() do
                proc.CanonicalizeAllNodes(this)

    member this.RemovePart(child: Dataset) =
        let removed = _hasPart.Remove(child)
        if removed && child.PartOf = Some this then
            // Collect nodes before disconnecting so the root reference is still valid.
            let nodesToCheck =
                child.AllProcesses()
                |> Seq.collect (fun p -> Seq.append p.Inputs p.Outputs)
                |> Seq.distinctBy (fun n -> n.Key())
                |> Seq.toList
            child.PartOf <- None
            // Evict nodes that are no longer used anywhere in the (now smaller) tree.
            for node in nodesToCheck do
                this.TryEvictNode(node)
            // Rebuild child's own registry now that it is a root again.
            child.NodeRegistryDirect.Clear()
            for proc in child.AllProcesses() do
                for node in Seq.append proc.Inputs proc.Outputs do
                    let key = node.Key()
                    if not (child.NodeRegistryDirect.ContainsKey(key)) then
                        child.NodeRegistryDirect.[key] <- node

    member _.TryGetPart(identifier: string) =
        _hasPart |> Seq.tryFind (fun d -> d.Identifier = identifier)

    // ── AdditionalProperty CRUD ───────────────────────────────────────────────

    member this.AddAdditionalProperty(pv: PropertyValue) =
        if not (_additionalProperty |> Seq.exists (fun x -> x = pv)) then
            _additionalProperty.Add(pv)

    member _.RemoveAdditionalProperty(pv: PropertyValue) =
        _additionalProperty.Remove(pv) |> ignore

    // ── Collection helpers ────────────────────────────────────────────────────

    /// All processes in this dataset and all nested datasets (depth-first)
    member this.AllProcesses() : ResizeArray<LabProcess> =
        let acc = ResizeArray()
        let rec collect (ds: Dataset) =
            acc.AddRange(ds.Processes)
            for child in ds.HasPart do collect child
        collect this
        acc

    /// All distinct Material nodes reachable from processes in this dataset
    member this.AllMaterials() : ResizeArray<Material> =
        let acc = ResizeArray()
        let seen = HashSet<string>()
        for proc in this.AllProcesses() do
            for node in proc.Inputs do
                match node with
                | MaterialNode m when seen.Add(m.Name) -> acc.Add(m)
                | _ -> ()
            for node in proc.Outputs do
                match node with
                | MaterialNode m when seen.Add(m.Name) -> acc.Add(m)
                | _ -> ()
        acc

    /// All distinct Data nodes reachable from processes in this dataset
    member this.AllData() : ResizeArray<Data> =
        let acc = ResizeArray()
        let seen = HashSet<string>()
        for proc in this.AllProcesses() do
            for node in proc.Inputs do
                match node with
                | DataNode d ->
                    let key = d.Path + (d.Selector |> Option.defaultValue "")
                    if seen.Add(key) then acc.Add(d)
                | _ -> ()
            for node in proc.Outputs do
                match node with
                | DataNode d ->
                    let key = d.Path + (d.Selector |> Option.defaultValue "")
                    if seen.Add(key) then acc.Add(d)
                | _ -> ()
        acc

    /// All distinct IONodes (materials and data) from all processes in this dataset.
    member this.AllNodes() : ResizeArray<IONode> =
        let acc  = ResizeArray<IONode>()
        let seen = HashSet<string>()
        for proc in this.AllProcesses() do
            for n: IONode in proc.Inputs  do if seen.Add(n.Key()) then acc.Add(n)
            for n: IONode in proc.Outputs do if seen.Add(n.Key()) then acc.Add(n)
        acc

    // ── Root / Final nodes ────────────────────────────────────────────────────

    /// All IONodes in this dataset with no predecessor process (terminal sources).
    member this.RootNodes() : ResizeArray<IONode> =
        let scope = this.AllProcesses()
        this.AllNodes() |> Seq.filter (fun n -> n.IsRootNode(scope)) |> ResizeArray

    /// All IONodes in this dataset with no successor process (terminal sinks).
    member this.FinalNodes() : ResizeArray<IONode> =
        let scope = this.AllProcesses()
        this.AllNodes() |> Seq.filter (fun n -> n.IsFinalNode(scope)) |> ResizeArray

    /// Root nodes that are Materials.
    member this.RootMaterials() : ResizeArray<Material> =
        this.RootNodes()
        |> Seq.choose (fun n -> match n with | MaterialNode m -> Some m | _ -> None)
        |> ResizeArray

    /// Final nodes that are Materials.
    member this.FinalMaterials() : ResizeArray<Material> =
        this.FinalNodes()
        |> Seq.choose (fun n -> match n with | MaterialNode m -> Some m | _ -> None)
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

    // ── Dataset-level property value queries ──────────────────────────────────

    /// All distinct PropertyValues from all sources across all processes in this dataset.
    /// Sources: process parameters, input/output node properties, protocol components.
    /// Optional protocolName restricts to processes whose protocol name matches.
    member this.AllPropertyValues(?protocolName: string) : ResizeArray<PropertyValue> =
        this.AllProcesses()
        |> collectPropertyValuesFromProcessesWithProtocolName protocolName

    /// All PropertyValues from all sources connected to `node` (upstream + downstream) within this dataset.
    /// Optional protocolName restricts to processes whose protocol name matches.
    member this.PropertyValuesForNode(node: IONode, ?protocolName: string) : ResizeArray<PropertyValue> =
        let scope      = this.AllProcesses()
        let upstream   = node.UpstreamPropertyValues(?protocolName = protocolName, scope = scope)
        let downstream = node.DownstreamPropertyValues(?protocolName = protocolName, scope = scope)
        let seenPV = HashSet<string>()
        let result = ResizeArray<PropertyValue>()
        for pv: PropertyValue in Seq.append upstream downstream do
            let key = propertyValueKey pv
            if seenPV.Add(key) then result.Add(pv)
        result

    /// PropertyValues from all sources in processes upstream of `node` within this dataset.
    member this.UpstreamPropertyValuesForNode(node: IONode, ?protocolName: string) : ResizeArray<PropertyValue> =
        node.UpstreamPropertyValues(?protocolName = protocolName, scope = this.AllProcesses())

    /// PropertyValues from all sources in processes downstream of `node` within this dataset.
    member this.DownstreamPropertyValuesForNode(node: IONode, ?protocolName: string) : ResizeArray<PropertyValue> =
        node.DownstreamPropertyValues(?protocolName = protocolName, scope = this.AllProcesses())

    // ── Querying ──────────────────────────────────────────────────────────────

    // ── Dataset-scoped node and path queries ──────────────────────────────────

    /// All processes in this dataset in which `node` appears as an input or output.
    member this.ProcessesForNode(node: IONode) : ResizeArray<LabProcess> =
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

    member this.MaterialsUpstreamOf(node: IONode) : ResizeArray<Material> =
        node.UpstreamMaterials(scope = this.AllProcesses())

    member this.MaterialsDownstreamOf(node: IONode) : ResizeArray<Material> =
        node.DownstreamMaterials(scope = this.AllProcesses())

    member this.DataUpstreamOf(node: IONode) : ResizeArray<Data> =
        node.UpstreamData(scope = this.AllProcesses())

    member this.DataDownstreamOf(node: IONode) : ResizeArray<Data> =
        node.DownstreamData(scope = this.AllProcesses())

    /// All IONodes connected to `node` within this dataset, excluding `node` itself.
    member this.AllConnectedNodes(node: IONode) : ResizeArray<IONode> =
        node.AllConnectedNodes(scope = this.AllProcesses())

    member this.ConnectedMaterialsForNode(node: IONode) : ResizeArray<Material> =
        node.ConnectedMaterials(scope = this.AllProcesses())

    member this.ConnectedDataForNode(node: IONode) : ResizeArray<Data> =
        node.ConnectedData(scope = this.AllProcesses())

    member this.AllPropertyValuesForNode(node: IONode) : ResizeArray<PropertyValue> =
        node.AllPropertyValues(scope = this.AllProcesses())

    member this.ProtocolParametersForNode(node: IONode) : ResizeArray<FormalParameter> =
        node.ProtocolParameters(scope = this.AllProcesses())

    /// All processes whose executed protocol's intendedUse name matches.
    member this.FindProcessesByProtocolType(intendedUse: string) : ResizeArray<LabProcess> =
        this.AllProcesses()
        |> Seq.filter (fun p ->
            match p.ExecutesProtocol with
            | Some proto ->
                match proto.IntendedUse with
                | Some dt -> dt.Name = intendedUse
                | None    -> false
            | None -> false)
        |> ResizeArray

    /// All processes that have a PropertyValue (from any source) with the given name and value.
    member this.FindProcessesByPropertyValue(paramName: string, paramValue: string) : ResizeArray<LabProcess> =
        let pvMatch (pv: PropertyValue) = pv.Name = paramName && pv.Value = Some paramValue
        let nodeMatch (n: IONode) =
            match n with
            | MaterialNode m -> m.AdditionalProperty |> Seq.exists pvMatch
            | DataNode d     -> d.AdditionalProperty |> Seq.exists pvMatch
        this.AllProcesses()
        |> Seq.filter (fun p ->
            (p.ParameterValue |> Seq.exists pvMatch) ||
            (p.Inputs  |> Seq.exists nodeMatch) ||
            (p.Outputs |> Seq.exists nodeMatch) ||
            (match p.ExecutesProtocol with
             | Some proto -> proto.LabEquipment |> Seq.exists pvMatch
             | None -> false))
        |> ResizeArray

    /// All processes that have a PropertyValue (from any source) with the given name (any value).
    member this.FindProcessesByPropertyName(paramName: string) : ResizeArray<LabProcess> =
        let pvMatch (pv: PropertyValue) = pv.Name = paramName
        let nodeMatch (n: IONode) =
            match n with
            | MaterialNode m -> m.AdditionalProperty |> Seq.exists pvMatch
            | DataNode d     -> d.AdditionalProperty |> Seq.exists pvMatch
        this.AllProcesses()
        |> Seq.filter (fun p ->
            (p.ParameterValue |> Seq.exists pvMatch) ||
            (p.Inputs  |> Seq.exists nodeMatch) ||
            (p.Outputs |> Seq.exists nodeMatch) ||
            (match p.ExecutesProtocol with
             | Some proto -> proto.LabEquipment |> Seq.exists pvMatch
             | None -> false))
        |> ResizeArray

    /// Find qualifying processes by protocol type, then filter their
    /// ParameterValues with a caller-supplied predicate.
    /// Returns all terminal-output Materials of the downstream subgraphs of qualifying processes.
    member this.MaterialsResultingFromConditionBy
        (protocolType: string, paramPredicate: PropertyValue -> bool) : ResizeArray<Material> =
        let qualifying =
            this.FindProcessesByProtocolType(protocolType)
            |> Seq.filter (fun p -> p.ParameterValue |> Seq.exists paramPredicate)
        let seen   = HashSet<string>()
        let result = ResizeArray<Material>()
        for proc in qualifying do
            for path in extendToMaximalPaths (this.AllProcesses()) proc do
                for node in path.TerminalOutputs() do
                    match node with
                    | MaterialNode m when seen.Add(m.Name) -> result.Add(m)
                    | _ -> ()
        result

    /// Identity: datasets with the same identifier are identical.
    override this.Equals(obj) =
        match obj with
        | :? Dataset as other -> this.Identifier = other.Identifier
        | _ -> false

    override this.GetHashCode() = hash this.Identifier
