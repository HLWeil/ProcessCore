```fsharp
namespace ARCtrl

open Fable.Core
open Fable.Core.JsInterop
open ARCtrl.Helper

[<AttachMembers>]
[<RequireQualifiedAccess>]
type IOType =
    | Source
    | Sample
    | Data
    | Material
    | FreeText of string


[<AttachMembers>]
[<RequireQualifiedAccess>]
[<StructuralComparison; StructuralEquality>]
type CompositeHeader = 
    // term
    | Component         of OntologyAnnotation
    | Characteristic    of OntologyAnnotation
    | Factor            of OntologyAnnotation
    | Parameter         of OntologyAnnotation
    // featured
    | ProtocolType
    // single
    | ProtocolDescription
    | ProtocolUri
    | ProtocolVersion
    | ProtocolREF
    | Performer
    | Date
    // single - io type
    | Input of IOType
    | Output of IOType
    // single - fallback
    | FreeText of string
    | Comment of string


[<AttachMembers>]
[<RequireQualifiedAccess>]
type CompositeCell = 
    /// ISA-TAB term columns as ontology annotation.
    ///
    /// https://isa-specs.readthedocs.io/en/latest/isatab.html#ontology-annotations
    | Term of OntologyAnnotation
    /// Single columns like Input, Output, ProtocolREF, .. .
    | FreeText of string
    /// ISA-TAB unit columns, consisting of value and unit as ontology annotation.
    ///
    /// https://isa-specs.readthedocs.io/en/latest/isatab.html#unit
    | Unitized of string*OntologyAnnotation
    | Data of Data    

[<AttachMembers>]
type CompositeColumn = {
    Header: CompositeHeader
    Cells: ResizeArray<CompositeCell>
}    


[<AttachMembers>]
type ArcTable(name: string, ?headers: ResizeArray<CompositeHeader>, ?columns: ResizeArray<ResizeArray<CompositeCell>>) =


[<AttachMembers>]
type ArcTables(initTables:ResizeArray<ArcTable>) = 
```