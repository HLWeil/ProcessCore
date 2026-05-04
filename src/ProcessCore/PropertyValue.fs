namespace rec ProcessCore

open Fable.Core
open DynamicObj

/// Extensible key-value-unit triple. Primary extension mechanism of ProcessCore.
/// schema.org/PropertyValue
[<AttachMembers>]
type PropertyValue(name: string) =

    inherit DynamicObj()

    let mutable _name: string = name
    let mutable _value: string option = None
    let mutable _unit: string option = None
    let mutable _nameTAN: string option = None
    let mutable _valueTAN: string option = None
    let mutable _unitTAN: string option = None
    let mutable _additionalType: string option = None
    let mutable _instanceOf: FormalParameter option = None

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

    /// Two PropertyValues are identical if name, value, unit, and nameTAN all match.
    override this.Equals(obj) =
        match obj with
        | :? PropertyValue as other ->
            this.Name = other.Name &&
            this.Value = other.Value &&
            this.Unit = other.Unit &&
            this.NameTAN = other.NameTAN
        | _ -> false

    override this.GetHashCode() =
        hash (this.Name, this.Value, this.Unit, this.NameTAN)
