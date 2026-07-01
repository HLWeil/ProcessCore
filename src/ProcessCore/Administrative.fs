namespace rec ProcessCore

open Fable.Core
open DynamicObj

/// Entity representing an organization involved in creating, curating, or hosting a dataset.
/// schema.org/Organization
[<AttachMembers>]
type Organization(name: string, ?id: string, ?url: string) =

    inherit DynamicObj()

    let mutable _id: string option = id
    let mutable _name: string = name
    let mutable _url: string option = url

    member _.Id
        with get() = _id
        and set v = _id <- v

    member _.Name
        with get() = _name
        and set v = _name <- v

    member _.Url
        with get() = _url
        and set v = _url <- v

    override this.Equals(obj) =
        match obj with
        | :? Organization as other ->
            match this.Id, other.Id with
            | Some a, Some b -> a = b
            | _ -> this.Name = other.Name
        | _ -> false

    override this.GetHashCode() =
        match this.Id with
        | Some id -> hash id
        | None -> hash this.Name

/// Individual contributor or contact associated with a dataset or article.
/// schema.org/Agent
[<AttachMembers>]
type Agent(givenName: string, ?id: string, ?familyName: string, ?email: string, ?affiliation: Organization, ?identifier: string, ?jobTitle: DefinedTerm, ?additionalName: string, ?address: string, ?telephone: string, ?additionalProperty: seq<Annotation>) as this =

    inherit DynamicObj()

    let mutable _id: string option = id
    let mutable _givenName: string = givenName
    let mutable _familyName: string option = familyName
    let mutable _email: string option = email
    let mutable _affiliation: Organization option = affiliation
    let mutable _identifier: string option = identifier
    let mutable _jobTitle: DefinedTerm option = jobTitle
    let mutable _additionalName: string option = additionalName
    let mutable _address: string option = address
    let mutable _telephone: string option = telephone
    let _additionalProperty: ResizeArray<Annotation> = ResizeArray()

    do
        additionalProperty |> Option.iter (fun pvs -> for pv in pvs do this.AddAdditionalProperty(pv))

    member _.Id
        with get() = _id
        and set v = _id <- v

    member _.GivenName
        with get() = _givenName
        and set v = _givenName <- v

    member _.FamilyName
        with get() = _familyName
        and set v = _familyName <- v

    member _.Email
        with get() = _email
        and set v = _email <- v

    member _.Affiliation
        with get() = _affiliation
        and set v = _affiliation <- v

    member _.Identifier
        with get() = _identifier
        and set v = _identifier <- v

    member _.JobTitle
        with get() = _jobTitle
        and set v = _jobTitle <- v

    member _.AdditionalName
        with get() = _additionalName
        and set v = _additionalName <- v

    member _.Address
        with get() = _address
        and set v = _address <- v

    member _.Telephone
        with get() = _telephone
        and set v = _telephone <- v

    member _.AdditionalProperty = _additionalProperty

    member this.AddAdditionalProperty(pv: Annotation) =
        if not (_additionalProperty |> Seq.exists (fun x -> x = pv)) then
            _additionalProperty.Add(pv)

    member _.RemoveAdditionalProperty(pv: Annotation) =
        _additionalProperty.Remove(pv) |> ignore

    override this.Equals(obj) =
        match obj with
        | :? Agent as other ->
            match this.Id, other.Id with
            | Some a, Some b -> a = b
            | _ -> this.GivenName = other.GivenName && this.FamilyName = other.FamilyName && this.Email = other.Email
        | _ -> false

    override this.GetHashCode() =
        match this.Id with
        | Some id -> hash id
        | None -> hash (this.GivenName, this.FamilyName, this.Email)

/// Scholarly publication associated with a dataset.
/// schema.org/ScholarlyArticle
[<AttachMembers>]
type ScholarlyArticle(headline: string, ?id: string, ?identifier: string, ?creativeWorkStatus: DefinedTerm, ?authors: seq<Agent>, ?additionalProperty: seq<Annotation>) as this =

    inherit DynamicObj()

    let mutable _id: string option = id
    let mutable _headline: string = headline
    let mutable _identifier: string option = identifier
    let mutable _creativeWorkStatus: DefinedTerm option = creativeWorkStatus
    let _authors: ResizeArray<Agent> = ResizeArray()
    let _additionalProperty: ResizeArray<Annotation> = ResizeArray()

    do
        authors |> Option.iter (fun ps -> for p in ps do this.AddAuthor(p))
        additionalProperty |> Option.iter (fun pvs -> for pv in pvs do this.AddAdditionalProperty(pv))

    member _.Id
        with get() = _id
        and set v = _id <- v

    member _.Headline
        with get() = _headline
        and set v = _headline <- v

    member _.Identifier
        with get() = _identifier
        and set v = _identifier <- v

    member _.CreativeWorkStatus
        with get() = _creativeWorkStatus
        and set v = _creativeWorkStatus <- v

    member _.Authors = _authors
    member _.AdditionalProperty = _additionalProperty

    member this.AddAuthor(agent: Agent) =
        if not (_authors |> Seq.exists (fun x -> x = agent)) then
            _authors.Add(agent)

    member _.RemoveAuthor(agent: Agent) =
        _authors.Remove(agent) |> ignore

    member this.AddAdditionalProperty(pv: Annotation) =
        if not (_additionalProperty |> Seq.exists (fun x -> x = pv)) then
            _additionalProperty.Add(pv)

    member _.RemoveAdditionalProperty(pv: Annotation) =
        _additionalProperty.Remove(pv) |> ignore

    override this.Equals(obj) =
        match obj with
        | :? ScholarlyArticle as other ->
            match this.Id, other.Id with
            | Some a, Some b -> a = b
            | _ -> this.Headline = other.Headline && this.Identifier = other.Identifier
        | _ -> false

    override this.GetHashCode() =
        match this.Id with
        | Some id -> hash id
        | None -> hash (this.Headline, this.Identifier)

