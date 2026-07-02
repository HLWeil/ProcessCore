module ProcessCore.Helper.ResizeArray

let map (f : 'T -> 'U) (arr : ResizeArray<'T>) : ResizeArray<'U> =
    let result = ResizeArray<'U>(arr.Count)
    for item in arr do
        result.Add(f item)
    result
