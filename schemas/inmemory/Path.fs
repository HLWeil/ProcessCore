namespace rec ArcDataModel

open Fable.Core
open System.Collections.Generic

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
        processes |> Seq.exists (fun p ->
            p.Inputs  |> Seq.exists (fun n -> n.Key() = key) ||
            p.Outputs |> Seq.exists (fun n -> n.Key() = key))

    /// All distinct IONodes that appear anywhere in this path (inputs or outputs)
    member _.Nodes() : ResizeArray<IONode> =
        let acc  = ResizeArray()
        let seen = HashSet<string>()
        for proc in processes do
            for n in proc.Inputs  do if seen.Add(n.Key()) then acc.Add(n)
            for n in proc.Outputs do if seen.Add(n.Key()) then acc.Add(n)
        acc

    /// All distinct Material nodes in this path
    member this.Materials() : ResizeArray<Material> =
        let acc = ResizeArray()
        for node in this.Nodes() do
            match node with
            | MaterialNode m -> acc.Add(m)
            | _ -> ()
        acc

    /// All distinct Data nodes in this path
    member this.DataNodes() : ResizeArray<Data> =
        let acc = ResizeArray()
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
        let acc  = ResizeArray()
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
        let acc  = ResizeArray()
        let seen = HashSet<string>()
        for proc in processes do
            for n in proc.Outputs do
                let k = n.Key()
                if not (inputKeys.Contains(k)) && seen.Add(k) then acc.Add(n)
        acc

    /// All PropertyValues from all sources (parameters, input/output node properties, protocol components)
    /// across all processes in this path
    member _.AllPropertyValues() : ResizeArray<PropertyValue> =
        let acc  = ResizeArray<PropertyValue>()
        let seen = HashSet<string>()
        let addPV (pv: PropertyValue) =
            let key = pv.Name + "|" + (pv.Value |> Option.defaultValue "") + "|" + (pv.NameTAN |> Option.defaultValue "")
            if seen.Add(key) then acc.Add(pv)
        for proc in processes do
            for pv in proc.ParameterValue do addPV pv
            for n in proc.Inputs do
                match n with
                | MaterialNode m -> for pv in m.AdditionalProperty do addPV pv
                | DataNode d     -> for pv in d.AdditionalProperty do addPV pv
            for n in proc.Outputs do
                match n with
                | MaterialNode m -> for pv in m.AdditionalProperty do addPV pv
                | DataNode d     -> for pv in d.AdditionalProperty do addPV pv
            match proc.ExecutesProtocol with
            | Some proto -> for pv in proto.LabEquipment do addPV pv
            | None -> ()
        acc

    /// All PropertyValues from all sources whose name matches the given string
    member this.PropertyValuesByName(name: string) : ResizeArray<PropertyValue> =
        this.AllPropertyValues() |> Seq.filter (fun pv -> pv.Name = name) |> ResizeArray

    /// All FormalParameters defined on protocols executed by processes in this path
    member _.ProtocolParameters() : ResizeArray<FormalParameter> =
        let acc  = ResizeArray()
        let seen = HashSet<string>()
        for proc in processes do
            match proc.ExecutesProtocol with
            | Some proto ->
                for fp in proto.Parameters do
                    if seen.Add(fp.Name) then acc.Add(fp)
            | None -> ()
        acc


// ─────────────────────────────────────────────────────────────────────────────
// ProcessGraph – graph traversal and composable query API
// ─────────────────────────────────────────────────────────────────────────────

/// Operates over a flat collection of LabProcess nodes (e.g. Dataset.AllProcesses()).
/// All queries are composable: use the primitive finders to build complex queries.
[<AttachMembers>]
type ProcessGraph(processes: ResizeArray<LabProcess>) =

    member _.Processes = processes

    // ── Primitive finders ─────────────────────────────────────────────────────

    member _.TryGetProcess(name: string) =
        processes |> Seq.tryFind (fun p -> p.Name = name)

    /// Processes whose executed protocol's intendedUse name matches.
    member _.FindProcessesByProtocolType(intendedUse: string) : ResizeArray<LabProcess> =
        processes
        |> Seq.filter (fun p ->
            match p.ExecutesProtocol with
            | Some proto ->
                match proto.IntendedUse with
                | Some dt -> dt.Name = intendedUse
                | None    -> false
            | None -> false)
        |> ResizeArray

    /// Processes that have a PropertyValue (from any source) with the given name and value.
    member _.FindProcessesByPropertyValue(paramName: string, paramValue: string) : ResizeArray<LabProcess> =
        let pvMatch (pv: PropertyValue) = pv.Name = paramName && pv.Value = Some paramValue
        let nodeMatch (n: IONode) =
            match n with
            | MaterialNode m -> m.AdditionalProperty |> Seq.exists pvMatch
            | DataNode d     -> d.AdditionalProperty |> Seq.exists pvMatch
        processes
        |> Seq.filter (fun p ->
            (p.ParameterValue |> Seq.exists pvMatch) ||
            (p.Inputs  |> Seq.exists nodeMatch) ||
            (p.Outputs |> Seq.exists nodeMatch) ||
            (match p.ExecutesProtocol with
             | Some proto -> proto.LabEquipment |> Seq.exists pvMatch
             | None -> false))
        |> ResizeArray

    /// Processes that have a PropertyValue (from any source) whose name matches (any value).
    member _.FindProcessesByPropertyName(paramName: string) : ResizeArray<LabProcess> =
        let pvMatch (pv: PropertyValue) = pv.Name = paramName
        let nodeMatch (n: IONode) =
            match n with
            | MaterialNode m -> m.AdditionalProperty |> Seq.exists pvMatch
            | DataNode d     -> d.AdditionalProperty |> Seq.exists pvMatch
        processes
        |> Seq.filter (fun p ->
            (p.ParameterValue |> Seq.exists pvMatch) ||
            (p.Inputs  |> Seq.exists nodeMatch) ||
            (p.Outputs |> Seq.exists nodeMatch) ||
            (match p.ExecutesProtocol with
             | Some proto -> proto.LabEquipment |> Seq.exists pvMatch
             | None -> false))
        |> ResizeArray

    /// All processes in which the given node appears as an input or output.
    member _.ProcessesForNode(node: IONode) : ResizeArray<LabProcess> =
        match node with
        | MaterialNode m ->
            let v = ResizeArray()
            v.AddRange(m.InputOf  |> Seq.filter (fun p -> processes |> Seq.exists (fun q -> q = p)))
            v.AddRange(m.OutputOf |> Seq.filter (fun p -> processes |> Seq.exists (fun q -> q = p)))
            v |> Seq.distinct |> ResizeArray
        | DataNode d ->
            let v = ResizeArray()
            v.AddRange(d.InputOf  |> Seq.filter (fun p -> processes |> Seq.exists (fun q -> q = p)))
            v.AddRange(d.OutputOf |> Seq.filter (fun p -> processes |> Seq.exists (fun q -> q = p)))
            v |> Seq.distinct |> ResizeArray

    // ── Core graph walk (private) ──────────────────────────────────────────────

    /// All upstream chains ending just before `proc` (not including `proc`).
    member private this.WalkUpstream(proc: LabProcess, visited: HashSet<string>) : ResizeArray<ResizeArray<LabProcess>> =
        if not (visited.Add(proc.Name)) then
            ResizeArray([ ResizeArray() ])
        else
            let preds : ResizeArray<LabProcess> =
                proc.Inputs
                |> Seq.collect (fun node ->
                    match node with
                    | MaterialNode m -> m.OutputOf :> seq<LabProcess>
                    | DataNode d     -> d.OutputOf :> seq<LabProcess>)
                |> Seq.filter (fun p -> processes |> Seq.exists (fun q -> q = p))
                |> Seq.distinct
                |> ResizeArray
            if preds.Count = 0 then
                ResizeArray([ ResizeArray() ])
            else
                let results = ResizeArray()
                for pred in preds do
                    for chain in this.WalkUpstream(pred, HashSet(visited)) do
                        let ext = ResizeArray(chain)
                        ext.Add(pred)
                        results.Add(ext)
                results

    /// All downstream chains starting just after `proc` (not including `proc`).
    member private this.WalkDownstream(proc: LabProcess, visited: HashSet<string>) : ResizeArray<ResizeArray<LabProcess>> =
        if not (visited.Add(proc.Name)) then
            ResizeArray([ ResizeArray() ])
        else
            let succs : ResizeArray<LabProcess> =
                proc.Outputs
                |> Seq.collect (fun node ->
                    match node with
                    | MaterialNode m -> m.InputOf :> seq<LabProcess>
                    | DataNode d     -> d.InputOf :> seq<LabProcess>)
                |> Seq.filter (fun p -> processes |> Seq.exists (fun q -> q = p))
                |> Seq.distinct
                |> ResizeArray
            if succs.Count = 0 then
                ResizeArray([ ResizeArray() ])
            else
                let results = ResizeArray()
                for succ in succs do
                    for chain in this.WalkDownstream(succ, HashSet(visited)) do
                        let ext = ResizeArray()
                        ext.Add(succ)
                        ext.AddRange(chain)
                        results.Add(ext)
                results

    /// Builds all maximal Paths that include the given process.
    member private this.ExtendToMaximalPath(proc: LabProcess) : ResizeArray<Path> =
        let upstream   = this.WalkUpstream(proc, HashSet())
        let downstream = this.WalkDownstream(proc, HashSet())
        let results = ResizeArray()
        for pre in upstream do
            for post in downstream do
                let chain = ResizeArray()
                chain.AddRange(pre)
                chain.Add(proc)
                chain.AddRange(post)
                results.Add(Path(chain))
        results

    // ── Path-based queries ────────────────────────────────────────────────────

    /// All maximal Paths that pass through the given IONode.
    member this.PathsThrough(node: IONode) : ResizeArray<Path> =
        let seeds = this.ProcessesForNode(node)
        let results = ResizeArray()
        for seed in seeds do
            results.AddRange(this.ExtendToMaximalPath(seed))
        results

    // ── Directed node-reachability queries ────────────────────────────────────

    /// All IONodes reachable by walking downstream from `node` (not including `node` itself).
    member this.NodesDownstreamOf(node: IONode) : ResizeArray<IONode> =
        let startingProcs =
            match node with
            | MaterialNode m -> m.InputOf  |> Seq.filter (fun p -> processes |> Seq.exists (fun q -> q = p)) |> ResizeArray
            | DataNode d     -> d.InputOf  |> Seq.filter (fun p -> processes |> Seq.exists (fun q -> q = p)) |> ResizeArray
        let acc  = ResizeArray()
        let seen = HashSet<string>()
        for proc in startingProcs do
            let chains = this.WalkDownstream(proc, HashSet())
            let allProcs = ResizeArray()
            allProcs.Add(proc)
            for chain in chains do allProcs.AddRange(chain)
            for p in allProcs do
                for n in p.Inputs  do if seen.Add(n.Key()) then acc.Add(n)
                for n in p.Outputs do if seen.Add(n.Key()) then acc.Add(n)
        acc

    /// All IONodes reachable by walking upstream from `node` (not including `node` itself).
    member this.NodesUpstreamOf(node: IONode) : ResizeArray<IONode> =
        let startingProcs =
            match node with
            | MaterialNode m -> m.OutputOf |> Seq.filter (fun p -> processes |> Seq.exists (fun q -> q = p)) |> ResizeArray
            | DataNode d     -> d.OutputOf |> Seq.filter (fun p -> processes |> Seq.exists (fun q -> q = p)) |> ResizeArray
        let acc  = ResizeArray()
        let seen = HashSet<string>()
        for proc in startingProcs do
            let chains = this.WalkUpstream(proc, HashSet())
            let allProcs = ResizeArray()
            allProcs.Add(proc)
            for chain in chains do allProcs.AddRange(chain)
            for p in allProcs do
                for n in p.Inputs  do if seen.Add(n.Key()) then acc.Add(n)
                for n in p.Outputs do if seen.Add(n.Key()) then acc.Add(n)
        acc

    /// All Material nodes reachable downstream of `node`.
    member this.MaterialsDownstreamOf(node: IONode) : ResizeArray<Material> =
        let acc = ResizeArray()
        for n in this.NodesDownstreamOf(node) do
            match n with
            | MaterialNode m -> acc.Add(m)
            | _ -> ()
        acc

    /// All Material nodes reachable upstream of `node`.
    member this.MaterialsUpstreamOf(node: IONode) : ResizeArray<Material> =
        let acc = ResizeArray()
        for n in this.NodesUpstreamOf(node) do
            match n with
            | MaterialNode m -> acc.Add(m)
            | _ -> ()
        acc

    /// All Data nodes reachable downstream of `node`.
    member this.DataDownstreamOf(node: IONode) : ResizeArray<Data> =
        let acc = ResizeArray()
        for n in this.NodesDownstreamOf(node) do
            match n with
            | DataNode d -> acc.Add(d)
            | _ -> ()
        acc

    /// All Data nodes reachable upstream of `node`.
    member this.DataUpstreamOf(node: IONode) : ResizeArray<Data> =
        let acc = ResizeArray()
        for n in this.NodesUpstreamOf(node) do
            match n with
            | DataNode d -> acc.Add(d)
            | _ -> ()
        acc

    // ── Path-neighbourhood queries ────────────────────────────────────────────

    /// Use-case 3: All IONodes connected to `node` through the process graph
    /// (union of all nodes in all paths that pass through `node`).
    member this.AllConnectedNodes(node: IONode) : ResizeArray<IONode> =
        let acc  = ResizeArray()
        let seen = HashSet<string>()
        for path in this.PathsThrough(node) do
            for n in path.Nodes() do
                if seen.Add(n.Key()) then acc.Add(n)
        acc

    /// Use-case 3 (typed): All Material nodes connected to `node`.
    member this.ConnectedMaterialsForNode(node: IONode) : ResizeArray<Material> =
        let acc = ResizeArray()
        for n in this.AllConnectedNodes(node) do
            match n with
            | MaterialNode m -> acc.Add(m)
            | _ -> ()
        acc

    /// Use-case 3 (typed): All Data nodes connected to `node`.
    member this.ConnectedDataForNode(node: IONode) : ResizeArray<Data> =
        let acc = ResizeArray()
        for n in this.AllConnectedNodes(node) do
            match n with
            | DataNode d -> acc.Add(d)
            | _ -> ()
        acc

    /// Use-case 2: All PropertyValues from all sources in all paths that contain `node`.
    member this.AllPropertyValuesForNode(node: IONode) : ResizeArray<PropertyValue> =
        let acc  = ResizeArray()
        let seen = HashSet<string>()
        for path in this.PathsThrough(node) do
            for pv: PropertyValue in path.AllPropertyValues() do
                let key = pv.Name + "|" + (pv.Value |> Option.defaultValue "") + "|" + (pv.NameTAN |> Option.defaultValue "")
                if seen.Add(key) then acc.Add(pv)
        acc

    /// Use-case 2 (extended): All FormalParameters from protocols executed in paths through `node`.
    member this.ProtocolParametersForNode(node: IONode) : ResizeArray<FormalParameter> =
        let acc  = ResizeArray()
        let seen = HashSet<string>()
        for path in this.PathsThrough(node) do
            for fp in path.ProtocolParameters() do
                if seen.Add(fp.Name) then acc.Add(fp)
        acc

    // ── Use-case 1: Dataset-level conditional query ───────────────────────────

    /// Use-case 1: All Material nodes that are terminal outputs of paths containing
    /// a process whose protocolType = `protocolType` AND which has a parameterValue
    /// with `paramName` = `paramValue`.
    member this.MaterialsResultingFromCondition
        (protocolType: string, paramName: string, paramValue: string) : ResizeArray<Material> =
        let qualifyingProcs =
            this.FindProcessesByProtocolType(protocolType)
            |> Seq.filter (fun p ->
                p.ParameterValue
                |> Seq.exists (fun pv -> pv.Name = paramName && pv.Value = Some paramValue))
            |> ResizeArray

        let result = ResizeArray()
        let seen   = HashSet<string>()
        for proc in qualifyingProcs do
            for path in this.ExtendToMaximalPath(proc) do
                for n in path.TerminalOutputs() do
                    match n with
                    | MaterialNode m when seen.Add(m.Name) -> result.Add(m)
                    | _ -> ()
        result

    /// Overload: Find qualifying processes by protocol type only, then filter by a
    /// caller-supplied predicate on ParameterValues (enables composable usage).
    member this.MaterialsResultingFromCondition
        (protocolType: string, paramPredicate: PropertyValue -> bool) : ResizeArray<Material> =
        let qualifyingProcs =
            this.FindProcessesByProtocolType(protocolType)
            |> Seq.filter (fun p -> p.ParameterValue |> Seq.exists paramPredicate)
            |> ResizeArray

        let result = ResizeArray()
        let seen   = HashSet<string>()
        for proc in qualifyingProcs do
            for path in this.ExtendToMaximalPath(proc) do
                for n in path.TerminalOutputs() do
                    match n with
                    | MaterialNode m when seen.Add(m.Name) -> result.Add(m)
                    | _ -> ()
        result
