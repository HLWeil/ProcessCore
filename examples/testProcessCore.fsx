#r "nuget: Fable.Core, 4.3.0"
#r "nuget: DynamicObj"
#r "nuget: YAMLicious"
#r "nuget: FsSpreadsheet.Net"
#r @"..\src\ProcessCore\bin\Release\netstandard2.1\ProcessCore.dll"
#r @"..\tests\ProcessCore.Tests\bin\Release\net10.0\ProcessCore.Tests.dll"

open ProcessCore
open ProcessCore.Table
open ProcessCore.Yaml

open ProcessCore.Tests.Graph.DatasetQueries
open ProcessCore.Tests.Fixtures

let p = @"C:\Users\HLWei\source\repos\Ru_ChlamyHeatstress"
let p = @"C:\Users\HLWei\source\repos\Facultative-CAM-in-Talinum"

let arc = ARC.loadXLSX(p).WriteYML(p)

let arc' = ARC.loadYML(p)

arc'.AllAnnotations()