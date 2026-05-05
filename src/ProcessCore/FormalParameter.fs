namespace rec ProcessCore

open Fable.Core
open DynamicObj

/// Describes the shape and type of a protocol parameter slot.
/// bioschemas.org/FormalParameter
[<AttachMembers>]
type FormalParameter(name: string) =

    inherit DynamicObj()

    let mutable _name: string = name
    let mutable _nameTAN: string option = None
    let mutable _defaultValue: DefinedTerm option = None


    member _.Name
        with get() = _name
        and set(v) = _name <- v

    /// Key ontology reference (URL)
    member _.NameTAN
        with get() = _nameTAN
        and set(v) = _nameTAN <- v

    /// Default value for the parameter
    member _.DefaultValue
        with get() = _defaultValue
        and set(v) = _defaultValue <- v

    /// Two FormalParameters are identical if their names match (within the same LabProtocol).
    override this.Equals(obj) =
        match obj with
        | :? FormalParameter as other -> this.Name = other.Name
        | _ -> false

    override this.GetHashCode() = hash this.Name
