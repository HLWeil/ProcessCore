// For more information see https://aka.ms/fsharp-console-apps
open ProcessCore

let arabidopsis = Annotation(name = "Organism", value = "Arabidopsis thaliana")
let tenDays = Annotation(name = "Time", value = "10", unit = "day")
let normalTemp = Annotation(name = "Temperature", value = "22", unit = "degree Celsius")

let createBySize (size: int) =
    let dataset = Dataset("Dataset")
    for i in 1 .. size do
        let p = Process(sprintf "Process%d" i)
        let inp = Sample(sprintf "InputSample%d" i, additionalProperty = [arabidopsis])
        let out = Sample(sprintf "OutputSample%d" i, additionalProperty = [arabidopsis])
        p.AddParameterValue(tenDays)
        p.AddParameterValue(normalTemp)
        dataset.AddProcess(p)
        p.SetInputSample(inp)
        p.SetOutputSample(out)

[<EntryPoint>]
let main argv =
    printfn "Start Speedtest"
    createBySize 1000000
    0 // return an integer exit code