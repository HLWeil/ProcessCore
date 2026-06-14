namespace rec ProcessCore

open Fable.Core
open DynamicObj

/// Ontology annotation referencing a term in a controlled vocabulary or ontology.
/// schema.org/DefinedTerm
[<AttachMembers>]
type DefinedTerm(name: string, ?tan: string, ?inDefinedTermSet: string) =

    inherit DynamicObj()
    let mutable _name: string = name
    let mutable _tan: string option = tan
    let mutable _inDefinedTermSet: string option = inDefinedTermSet


    member _.Name
        with get() = _name
        and set(v) = _name <- v

    /// Term Accession Number – identifier within the ontology
    member _.TAN
        with get() = _tan
        and set(v) = _tan <- v

    /// URL or DefinedTermSet reference pointing to the ontology
    member _.InDefinedTermSet
        with get() = _inDefinedTermSet
        and set(v) = _inDefinedTermSet <- v

    override this.Equals(obj) =
        match obj with
        | :? DefinedTerm as other ->
            this.Name = other.Name &&
            this.TAN = other.TAN &&
            this.InDefinedTermSet = other.InDefinedTermSet
        | _ -> false

    override this.GetHashCode() =
        hash (this.Name, this.TAN, this.InDefinedTermSet)
