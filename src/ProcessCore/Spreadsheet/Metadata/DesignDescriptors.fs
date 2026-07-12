namespace ProcessCore.Spreadsheet

open ProcessCore
open Comment
open Remark
open System.Collections.Generic

module DesignDescriptors = 

    let designTypeLabel = "Type"
    let designTypeTermAccessionNumberLabel = "Type Term Accession Number"
    let designTypeTermSourceREFLabel = "Type Term Source REF"

    let labels = [designTypeLabel;designTypeTermAccessionNumberLabel;designTypeTermSourceREFLabel]

    let fromSparseTable (matrix : SparseTable) =
        OntologyAnnotationSection.fromSparseTable designTypeLabel designTypeTermSourceREFLabel designTypeTermAccessionNumberLabel matrix

    let fromRows (prefix : string option) lineNumber (rows : IEnumerator<SparseRow>) =
        OntologyAnnotationSection.fromRows prefix designTypeLabel designTypeTermSourceREFLabel designTypeTermAccessionNumberLabel lineNumber rows

    let toSparseTable (designs: DefinedTerm seq) =
        OntologyAnnotationSection.toSparseTable designTypeLabel designTypeTermSourceREFLabel designTypeTermAccessionNumberLabel designs

    let toRows (prefix : string option) (designs : DefinedTerm seq) =
        OntologyAnnotationSection.toRows prefix designTypeLabel designTypeTermSourceREFLabel designTypeTermAccessionNumberLabel designs