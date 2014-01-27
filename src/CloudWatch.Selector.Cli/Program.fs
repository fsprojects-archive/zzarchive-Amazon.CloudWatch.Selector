open System
open System.Collections.Generic
open Microsoft.FSharp.Text

open FSharp.Charting
open FSharp.Charting.ChartTypes

open Amazon
open Amazon.CloudWatch
open Amazon.CloudWatch.Model
open Amazon.CloudWatch.Selector

let stringCI text (input : string) = input.Equals(text, StringComparison.CurrentCultureIgnoreCase)

let (|Exit|_|) input = if stringCI "exit" input then Some () else None
let (|Plot|_|) input = if stringCI "plot" input then Some () else None

let csv (seq : string seq) = String.Join(", ", seq)

let rec loop (cloudWatch : IAmazonCloudWatch) (state : (Metric * List<Datapoint>)[] option) = 
    let prefix = "\nselector> "

    printf "%s" prefix
    match Console.ReadLine() with
    | Exit  -> printfn "%sGood bye!" prefix
    | Plot  -> 
        match state with
        | Some res -> 
            printfn "%sPlotting..." prefix
            loop cloudWatch state
        | _ -> 
            printfn "%sPlease run a query first" prefix
            loop cloudWatch state   
    | query -> 
        printfn "%sRunning... (might take a minute)" prefix
        let state = cloudWatch.Select query |> Async.RunSynchronously

        printfn "%sFound %d metrics" prefix state.Length
        for (metric, _) in state do
            printfn "Namespace : %s | Name : %s | Dimensions : [ %s ]" 
                    metric.Namespace 
                    metric.MetricName 
                    (metric.Dimensions |> Seq.map (fun dim -> sprintf "%s:%s" dim.Name dim.Value) |> csv)
        
        loop cloudWatch (Some state)

[<EntryPoint>]
let main argv = 
    let compile s = ()

    let awsKey      = ref ""
    let awsSecret   = ref ""
    let region      = ref ""
    let specs =
        [   
            "-awsKey",    ArgType.String (fun s -> awsKey := s),    "AWS Access Key, e.g. AKIAITIDAJ88PL5FZ59Q"
            "-awsSecret", ArgType.String (fun s -> awsSecret := s), "AWS Access Secret"
            "-region",    ArgType.String (fun s -> region := s),    "AWS Region, e.g. USEast1"
        ] 
        |> List.map (fun (sh, ty, desc) -> ArgInfo(sh, ty, desc))
 
    ArgParser.Parse(specs, compile)

    printfn "\n\n\n"
    printfn "=============================================================="
    printfn "======             Amazon.CloudWatch.Selector           ======"
    printfn "=============================================================="
    printfn "\nCloudWatch client initialized.\n"

    let region = RegionEndpoint.USEast1
    let cloudWatch = AWSClientFactory.CreateAmazonCloudWatchClient(!awsKey, !awsSecret, region)

    loop cloudWatch None

    0 // return an integer exit code