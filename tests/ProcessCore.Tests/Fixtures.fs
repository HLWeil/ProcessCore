module ProcessCore.Tests.Fixtures

open ProcessCore

module Utils =

    let firstDiff s1 s2 =
        let s1 = Seq.append (Seq.map Some s1) (Seq.initInfinite (fun _ -> None))
        let s2 = Seq.append (Seq.map Some s2) (Seq.initInfinite (fun _ -> None))
        Seq.mapi2 (fun i s p -> i,s,p) s1 s2
        |> Seq.find (function |_,Some s,Some p when s=p -> false |_-> true)


module Expect =

    open Utils

    /// Expects the `actual` sequence to equal the `expected` one.
    let sequenceEqual actual expected message =
      match firstDiff actual expected with
      | _,None,None -> ()
      | i,Some a, Some e ->
        failwithf "%s. Sequence does not match at position %i. Expected item: %O, but got %O."
          message i e a
      | i,None,Some e ->
        failwithf "%s. Sequence actual shorter than expected, at pos %i for expected item %O."
          message i e
      | i,Some a,None ->
        failwithf "%s. Sequence actual longer than expected, at pos %i found item %O."
          message i a


// ─────────────────────────────────────────────────────────────────────────────
// Fixture A — Linear Chain
//
//   Source1 --[p1]--> Sample1 --[p2]--> Sample2 --[p3]--> rawData1.csv
//
//   p1  protocol "extraction", IntendedUse = DefinedTerm("cell growth")
//       ParameterValue: temperature = "37" (unit "°C"), rpm = "200" (unit "rpm")
//   p2  protocol "digestion"
//       ParameterValue: enzyme = "Trypsin" (term, with TAN)
//   p3  no protocol
// ─────────────────────────────────────────────────────────────────────────────

type FixtureA =
    { DS       : Dataset
      P1       : Process
      P2       : Process
      P3       : Process
      Source1  : Sample
      Sample1  : Sample
      Sample2  : Sample
      RawData1 : Data }

let makeFixtureA () : FixtureA =
    let source1  = Sample("Source1",  additionalType = "Source")
    let sample1  = Sample("Sample1",  additionalType = "Sample")
    let sample2  = Sample("Sample2",  additionalType = "Sample")
    let rawData1 = Data("rawData1.csv")

    // p1 — extraction / cell growth
    let proto1 = Recipe("extraction")
    proto1.IntendedUse <- Some (DefinedTerm("cell growth"))
    proto1.AddParameter(FormalParameter("temperature"))
    proto1.AddParameter(FormalParameter("rpm"))

    let p1 = Process("p1")
    p1.ExecutesProtocol <- Some proto1
    p1.AddParameterValue(Annotation("temperature", value = "37",  unit = "°C",  additionalType = "ParameterValue"))
    p1.AddParameterValue(Annotation("rpm",         value = "200", unit = "rpm", additionalType = "ParameterValue"))
    p1.AddInputSample(source1)
    p1.AddOutputSample(sample1)

    // p2 — digestion
    let proto2 = Recipe("digestion")

    let p2 = Process("p2")
    p2.ExecutesProtocol <- Some proto2
    p2.AddParameterValue(
        Annotation("enzyme",
                      value    = "Trypsin",
                      valueTAN = "http://purl.obolibrary.org/obo/NCIT_C17077",
                      additionalType = "ParameterValue"))
    p2.AddInputSample(sample1)
    p2.AddOutputSample(sample2)

    // p3 — no protocol
    let p3 = Process("p3")
    p3.AddInputSample(sample2)
    p3.AddOutputData(rawData1)

    let ds = Dataset("DS-A")
    ds.AddProcess(p1)
    ds.AddProcess(p2)
    ds.AddProcess(p3)

    { DS = ds; P1 = p1; P2 = p2; P3 = p3
      Source1 = source1; Sample1 = sample1; Sample2 = sample2; RawData1 = rawData1 }

// ─────────────────────────────────────────────────────────────────────────────
// Fixture B — Branching Graph
//
//   Source1 --[p1]--> Sample1 --[p2]--> SampleA
//                             --[p3]--> SampleB
//
//   p1  protocol "extraction", IntendedUse = DefinedTerm("cell growth"),
//       ParameterValue: temperature = "37" (unit "°C")
// ─────────────────────────────────────────────────────────────────────────────

type FixtureB =
    { DS      : Dataset
      P1      : Process
      P2      : Process
      P3      : Process
      Source1 : Sample
      Sample1 : Sample
      SampleA : Sample
      SampleB : Sample }

let makeFixtureB () : FixtureB =
    let source1 = Sample("Source1", additionalType = "Source")
    let sample1 = Sample("Sample1", additionalType = "Sample")
    let sampleA = Sample("SampleA", additionalType = "Sample")
    let sampleB = Sample("SampleB", additionalType = "Sample")

    let proto1 = Recipe("extraction")
    proto1.IntendedUse <- Some (DefinedTerm("cell growth"))

    let p1 = Process("p1")
    p1.ExecutesProtocol <- Some proto1
    p1.AddParameterValue(Annotation("temperature", value = "37", unit = "°C", additionalType = "ParameterValue"))
    p1.AddInputSample(source1)
    p1.AddOutputSample(sample1)

    let p2 = Process("p2")
    p2.AddInputSample(sample1)
    p2.AddOutputSample(sampleA)

    let p3 = Process("p3")
    p3.AddInputSample(sample1)
    p3.AddOutputSample(sampleB)

    let ds = Dataset("DS-B")
    ds.AddProcess(p1)
    ds.AddProcess(p2)
    ds.AddProcess(p3)

    { DS = ds; P1 = p1; P2 = p2; P3 = p3
      Source1 = source1; Sample1 = sample1; SampleA = sampleA; SampleB = sampleB }

// ─────────────────────────────────────────────────────────────────────────────
// Fixture C — Merging Graph
//
//   Source1 --[p1]--> Sample1 \
//                              [p3]--> FinalSample
//   Source2 --[p2]--> Sample2 /
// ─────────────────────────────────────────────────────────────────────────────

type FixtureC =
    { DS          : Dataset
      P1          : Process
      P2          : Process
      P3          : Process
      Source1     : Sample
      Source2     : Sample
      Sample1     : Sample
      Sample2     : Sample
      FinalSample : Sample }

let makeFixtureC () : FixtureC =
    let source1     = Sample("Source1",     additionalType = "Source")
    let source2     = Sample("Source2",     additionalType = "Source")
    let sample1     = Sample("Sample1",     additionalType = "Sample")
    let sample2     = Sample("Sample2",     additionalType = "Sample")
    let finalSample = Sample("FinalSample", additionalType = "Sample")

    let p1 = Process("p1")
    p1.AddInputSample(source1)
    p1.AddOutputSample(sample1)

    let p2 = Process("p2")
    p2.AddInputSample(source2)
    p2.AddOutputSample(sample2)

    let p3 = Process("p3")
    p3.AddInputSample(sample1)
    p3.AddInputSample(sample2)
    p3.AddOutputSample(finalSample)

    let ds = Dataset("DS-C")
    ds.AddProcess(p1)
    ds.AddProcess(p2)
    ds.AddProcess(p3)

    { DS = ds; P1 = p1; P2 = p2; P3 = p3
      Source1 = source1; Source2 = source2
      Sample1 = sample1; Sample2 = sample2; FinalSample = finalSample }

// ─────────────────────────────────────────────────────────────────────────────
// Fixture D — Nested Datasets
//
//   Dataset("parent")
//     ├─ Dataset("child1")  [p1: Source1 → Sample1]
//     └─ Dataset("child2")  [p2: Sample1 → rawData1.csv]
//
//   Sample1 is the SAME object instance used as p1's output and p2's input.
// ─────────────────────────────────────────────────────────────────────────────

type FixtureD =
    { Parent   : Dataset
      Child1   : Dataset
      Child2   : Dataset
      P1       : Process
      P2       : Process
      Source1  : Sample
      Sample1  : Sample   // shared — same object instance in both children
      RawData1 : Data }

let makeFixtureD () : FixtureD =
    let source1  = Sample("Source1",      additionalType = "Source")
    let sample1  = Sample("Sample1",      additionalType = "Sample")  // shared
    let rawData1 = Data("rawData1.csv")

    let p1 = Process("p1")
    p1.AddInputSample(source1)
    p1.AddOutputSample(sample1)

    let p2 = Process("p2")
    p2.AddInputSample(sample1)   // same object as p1's output
    p2.AddOutputData(rawData1)

    let child1 = Dataset("child1")
    child1.AddProcess(p1)

    let child2 = Dataset("child2")
    child2.AddProcess(p2)

    let parent = Dataset("parent")
    parent.AddPart(child1)
    parent.AddPart(child2)

    { Parent = parent; Child1 = child1; Child2 = child2
      P1 = p1; P2 = p2
      Source1 = source1; Sample1 = sample1; RawData1 = rawData1 }

// ─────────────────────────────────────────────────────────────────────────────
// Fixture E — IOGroupedProcesses
//
//   Two processes each containing two input/output pairs, i.e. two distinct 1D paths.
//
//   Source1 --[p1]--> Sample1 --[p2]--> Data1
//   Source2 --[p1]--> Sample2 --[p2]--> Data2
// ─────────────────────────────────────────────────────────────────────────────


type FixtureE =
    { DS        : Dataset
      P1        : Process
      P1PV      : Annotation
      P2        : Process
      P2PV      : Annotation
      Source1   : Sample
      Source1PV : Annotation
      Source2   : Sample
      Source2PV : Annotation
      Sample1   : Sample
      Sample1PV : Annotation
      Sample2   : Sample
      Sample2PV : Annotation
      Data1     : Data
      Data1PV   : Annotation
      Data2     : Data
      Data2PV   : Annotation }

let makeFixtureE () : FixtureE =

    let source1PV = Annotation("source1_characteristic", value = "source1_val", additionalType = "Characteristic")
    let source2PV = Annotation("source2_characteristic", value = "source2_val", additionalType = "Characteristic")
    let sample1PV = Annotation("sample1_characteristic", value = "sample1_val", additionalType = "Characteristic")
    let sample2PV = Annotation("sample2_characteristic", value = "sample2_val", additionalType = "Characteristic")
    let p1PV      = Annotation("p1_parameter", value = "p1_val", additionalType = "ParameterValue")
    let p2PV      = Annotation("p2_parameter", value = "p2_val", additionalType = "ParameterValue")
    let data1PV   = Annotation("data1_characteristic", value = "data1_val", additionalType = "Characteristic")
    let data2PV   = Annotation("data2_characteristic", value = "data2_val", additionalType = "Characteristic")

    let source1 = Sample("Source1", additionalType = "Source", additionalProperty = [source1PV])
    let source2 = Sample("Source2", additionalType = "Source", additionalProperty = [source2PV])
    let sample1 = Sample("Sample1", additionalType = "Sample", additionalProperty = [sample1PV])
    let sample2 = Sample("Sample2", additionalType = "Sample", additionalProperty = [sample2PV])
    let data1   = Data("Data1", additionalProperty = [data1PV])
    let data2   = Data("Data2", additionalProperty = [data2PV])

    let p1 = Process("p1")
    let p2 = Process("p2")

    p1.AddParameterValue(p1PV)
    p2.AddParameterValue(p2PV)

    p1.AddInputSample(source1)
    p1.AddInputSample(source2)
    p1.AddOutputSample(sample1)
    p1.AddOutputSample(sample2)
    p2.AddInputSample(sample1)
    p2.AddInputSample(sample2)
    p2.AddOutputData(data1)
    p2.AddOutputData(data2)

    let ds = Dataset("DS-E")

    ds.AddProcess(p1)
    ds.AddProcess(p2)
    { DS = ds; P1 = p1; P1PV = p1PV; P2 = p2; P2PV = p2PV
      Source1 = source1; Source1PV = source1PV
      Source2 = source2; Source2PV = source2PV
      Sample1 = sample1; Sample1PV = sample1PV
      Sample2 = sample2; Sample2PV = sample2PV
      Data1 = data1; Data1PV = data1PV
      Data2 = data2; Data2PV = data2PV }

// ─────────────────────────────────────────────────────────────────────────────
// Fixture FourSources
//
// A chain with a central process that has Annotations in all four source
// locations simultaneously, flanked by upstream and downstream processes each
// carrying a unique PV. Used by section 6 (Annotation Sources) tests.
//
//   UpstreamNode --[UpstreamProc]--> InputNode --[Process]--> OutputNode --[DownstreamProc]--> DownstreamNode
//
//   Process  has: ParamPV (ParameterValue), InputPV (input node AdditionalProperty),
//                 OutputPV (output node AdditionalProperty), ComponentPV (protocol Component)
//   UpstreamProc   has: UpstreamOnlyPV
//   DownstreamProc has: DownstreamOnlyPV
// ─────────────────────────────────────────────────────────────────────────────

type FixtureFourSources =
    {
        DS               : Dataset
        Process          : Process
        UpstreamProc     : Process
        DownstreamProc   : Process
        UpstreamNode     : Sample
        InputNode        : Sample
        OutputNode       : Sample
        DownstreamNode   : Sample
        ParamPV          : Annotation // from ParameterValue
        InputPV          : Annotation // from input node AdditionalProperty
        OutputPV         : Annotation // from output node AdditionalProperty
        ComponentPV      : Annotation // from protocol Component
        UpstreamOnlyPV   : Annotation // only on UpstreamProc
        DownstreamOnlyPV : Annotation // only on DownstreamProc
    }

let makeFixtureFourSources () : FixtureFourSources =
    let upstreamNode   = Sample("FS_UpstreamNode",   additionalType = "Source")
    let inputNode      = Sample("FS_InputNode",      additionalType = "Sample")
    let outputNode     = Sample("FS_OutputNode",     additionalType = "Sample")
    let downstreamNode = Sample("FS_DownstreamNode", additionalType = "Sample")

    // upstream process
    let upstreamProc   = Process("fs-upstream-process")
    let upstreamOnlyPV = Annotation("upstream_param", value = "upstream_val", additionalType = "ParameterValue")
    upstreamProc.AddParameterValue(upstreamOnlyPV)
    upstreamProc.AddInputSample(upstreamNode)
    upstreamProc.AddOutputSample(inputNode)

    // central process — all four sources
    let proto   = Recipe("four-source-protocol")
    let compPV  = Annotation("instrument",    value = "Orbitrap",  additionalType = "Component")
    proto.AddComponent(compPV)

    let proc    = Process("fs-four-source-process")
    proc.ExecutesProtocol <- Some proto

    let paramPV  = Annotation("temperature",  value = "25",      additionalType = "ParameterValue")
    let inputPV  = Annotation("organism",     value = "E. coli", additionalType = "CharacteristicValue")
    let outputPV = Annotation("growth_phase", value = "log",     additionalType = "FactorValue")

    proc.AddParameterValue(paramPV)
    inputNode.AddAdditionalProperty(inputPV)
    outputNode.AddAdditionalProperty(outputPV)
    proc.AddInputSample(inputNode)
    proc.AddOutputSample(outputNode)

    // downstream process
    let downstreamProc   = Process("fs-downstream-process")
    let downstreamOnlyPV = Annotation("downstream_param", value = "downstream_val", additionalType = "ParameterValue")
    downstreamProc.AddParameterValue(downstreamOnlyPV)
    downstreamProc.AddInputSample(outputNode)
    downstreamProc.AddOutputSample(downstreamNode)

    let ds = Dataset("DS-FourSources")
    ds.AddProcess(upstreamProc)
    ds.AddProcess(proc)
    ds.AddProcess(downstreamProc)

    {
      DS               = ds
      Process          = proc
      UpstreamProc     = upstreamProc
      DownstreamProc   = downstreamProc
      UpstreamNode     = upstreamNode
      InputNode        = inputNode
      OutputNode       = outputNode
      DownstreamNode   = downstreamNode
      ParamPV          = paramPV
      InputPV          = inputPV
      OutputPV         = outputPV
      ComponentPV      = compPV
      UpstreamOnlyPV   = upstreamOnlyPV
      DownstreamOnlyPV = downstreamOnlyPV }
