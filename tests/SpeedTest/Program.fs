// For more information see https://aka.ms/fsharp-console-apps
open ProcessCore

let arabidopsis = PropertyValue(name = "Organism", value = "Arabidopsis thaliana")
let tenDays = PropertyValue(name = "Time", value = "10", unit = "day")
let normalTemp = PropertyValue(name = "Temperature", value = "22", unit = "degree Celsius")

let createBySize (size: int) =
    let dataset = Dataset("Dataset")
    for i in 1 .. size do
        let p = LabProcess(sprintf "Process%d" i)
        let inp = Material(sprintf "InputMaterial%d" i, additionalProperty = [arabidopsis])
        let out = Material(sprintf "OutputMaterial%d" i, additionalProperty = [arabidopsis])
        p.AddParameterValue(tenDays)
        p.AddParameterValue(normalTemp)
        dataset.AddProcess(p)
        p.AddInputMaterial(inp)
        p.AddOutputMaterial(out)

[<EntryPoint>]
let main argv = 
    printfn "Start Speedtest"
    createBySize 1000000
    0 // return an integer exit code