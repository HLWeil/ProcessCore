namespace rec ProcessCore

open Fable.Core
open DynamicObj

/// Extensible key-value-unit triple. Primary extension mechanism of ProcessCore.
/// schema.org/PropertyValue
[<AttachMembers>]
type Annotation(name: string, ?value: string, ?unit: string, ?nameTAN: string, ?valueTAN: string, ?unitTAN: string, ?additionalType: string, ?instanceOf: FormalParameter) =

    inherit DynamicObj()

    let mutable _name: string = name
    let mutable _value: string option = value
    let mutable _unit: string option = unit
    let mutable _nameTAN: string option = nameTAN
    let mutable _valueTAN: string option = valueTAN
    let mutable _unitTAN: string option = unitTAN
    let mutable _additionalType: string option = additionalType
    let mutable _instanceOf: FormalParameter option = instanceOf

    member _.Name
        with get() = _name
        and set(v) = _name <- v

    member _.Value
        with get() = _value
        and set(v) = _value <- v

    member _.Unit
        with get() = _unit
        and set(v) = _unit <- v

    /// Key ontology reference (URL)
    member _.NameTAN
        with get() = _nameTAN
        and set(v) = _nameTAN <- v

    /// Value term annotation (URL)
    member _.ValueTAN
        with get() = _valueTAN
        and set(v) = _valueTAN <- v

    /// Unit term annotation (URL)
    member _.UnitTAN
        with get() = _unitTAN
        and set(v) = _unitTAN <- v

    /// Subtype discriminator (e.g. ParameterValue, CharacteristicValue, FactorValue)
    member _.AdditionalType
        with get() = _additionalType
        and set(v) = _additionalType <- v

    /// Links a parameter value to its formal parameter definition
    member _.InstanceOf
        with get() = _instanceOf
        and set(v) = _instanceOf <- v

    member _.NameText = name

    member _.ValueText = 
        match _value with
        | Some v -> v
        | None -> ""

    member _.UnitText = 
        match _unit with
        | Some v -> v
        | None -> ""

    member _.ValueWithUnitText = 
        match _value, _unit with
        | Some v, Some u -> sprintf "%s %s" v u
        | Some v, None   -> v
        | None, Some u   -> u
        | None, None     -> ""

    member this.NameEquals(term: DefinedTerm) =
        match this.NameTAN, term.TAN with
        | Some left, Some right -> left = right
        | _ -> this.Name = term.Name

    /// Two Annotations are identical if name, value, unit, and nameTAN all match.
    override this.Equals(obj) =
        match obj with
        | :? Annotation as other ->
            this.Name = other.Name &&
            this.Value = other.Value &&
            this.Unit = other.Unit &&
            this.NameTAN = other.NameTAN
        | _ -> false

    override this.GetHashCode() =
        hash (this.Name, this.Value, this.Unit, this.NameTAN)
