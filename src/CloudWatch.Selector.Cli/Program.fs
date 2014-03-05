open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Reflection
open Microsoft.FSharp.Text

open Amazon
open Amazon.CloudWatch
open Amazon.CloudWatch.Model
open Amazon.CloudWatch.Selector

let stringCI text (input : string) = input.Equals(text, StringComparison.CurrentCultureIgnoreCase)

let (|Exit|_|) input = if stringCI "exit" input then Some () else None
let (|Plot|_|) input = if stringCI "plot" input then Some () else None
let (|Help|_|) input = if stringCI "help" input then Some () else None

let stringJoin separator (seq : string seq) = String.Join(separator, seq)
let csv             = stringJoin ","
let singleQuote     = sprintf "'%s'"
let bracket         = sprintf "[%s]"
let trailingComma   = sprintf "%s,"
let combinePath a b = Path.Combine(a, b)
let tempPath        = Path.GetTempPath()

let resultTemplate = 
    use stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Result.html")
    use reader = new StreamReader(stream)
    reader.ReadToEnd()

let draw (state : (Metric * List<Datapoint>)[]) =
    let data = new Dictionary<DateTime, Dictionary<string, Datapoint>>()
    
    let getMetricName (metric : Metric) =
        sprintf "%s-%s" metric.MetricName (metric.Dimensions |> Seq.map (fun dim -> sprintf "%s:%s" dim.Name dim.Value) |> csv)

    let metricNames = state |> Seq.map (fst >> getMetricName) |> Seq.toList

    for (metric, datapoints) in state do
        for datapoint in datapoints do
            match data.TryGetValue(datapoint.Timestamp) with
            | true, dict -> dict.Add(getMetricName metric, datapoint)
            | _ -> let dict = new Dictionary<string, Datapoint>()
                   dict.Add(getMetricName metric, datapoint)
                   data.Add(datapoint.Timestamp, dict)

    let timestamps = data.Keys |> Seq.sort |> Seq.toArray

    let getData (f : Datapoint -> float) =
        seq {
            yield "Timestamp"::metricNames |> List.map singleQuote |> csv |> bracket |> trailingComma
            for timestamp in timestamps do
                let dict      = data.[timestamp]
                let timestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss") |> singleQuote
                
                let line = metricNames 
                           |> List.map (fun m -> 
                                match dict.TryGetValue(m) with 
                                | true, dp -> f dp
                                | _        -> 0.0
                                |> string)
                yield timestamp::line |> csv |> bracket |> trailingComma
        }
        |> stringJoin "\n"

    let replaceToken (token : string) f (output : string) =
        let data = getData f
        output.Replace(token, data)

    let output = 
        resultTemplate
        |> replaceToken "@avg_data@" (fun dp -> dp.Average)
        |> replaceToken "@min_data@" (fun dp -> dp.Minimum)
        |> replaceToken "@max_data@" (fun dp -> dp.Maximum)
        |> replaceToken "@sum_data@" (fun dp -> dp.Sum)
        |> replaceToken "@sample_count_data@" (fun dp -> dp.SampleCount)

    let outputFile = sprintf "Result_%s.html" <| DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") |> combinePath tempPath
    File.WriteAllText(outputFile, output)

    Process.Start(outputFile) |> ignore

let rec loop (cloudWatch : IAmazonCloudWatch) (state : (Metric * List<Datapoint>)[] option) = 
    let prefix = "\nselector> "

    printf "%s" prefix
    match Console.ReadLine() with
    | Exit  -> printfn "%sGood bye!" prefix
    | Help  -> printfn """%sCommands:
    help        - display list of commands
    exit        - exit the CLI
    plot        - plots the current results onto a set of graphs

All other inputs are treated as a query, go to http://bit.ly/awscwcli to learn more about the DSL syntax and find examples.
    """
                       prefix
               loop cloudWatch state
    | Plot  -> 
        match state with
        | Some state' -> 
            printfn "%sPlotting..." prefix
            draw state'

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
            "-awsKey",    ArgType.String (fun s -> awsKey := s),    "AWS Access Key, e.g. AKIAITIDAJ88PL5FZ59Q. Optional, if not provided will use the credential specified in app.config or instance IAM role."
            "-awsSecret", ArgType.String (fun s -> awsSecret := s), "AWS Access Secret. Optional, if not provided will use the credential specified in app.config or instance IAM role."
            "-region",    ArgType.String (fun s -> region := s),    "AWS Region, e.g. us-east-1. Optional, if not provided defaults to us-east-1"
        ] 
        |> List.map (fun (sh, ty, desc) -> ArgInfo(sh, ty, desc))
 
    ArgParser.Parse(specs, compile)

    printfn "\n\n\n"
    printfn "=============================================================="
    printfn "======             Amazon.CloudWatch.Selector           ======"
    printfn "=============================================================="
    printfn "\nCloudWatch client initialized.\n"

    let cloudWatch =
        match !awsKey, !awsSecret, !region with
        | "", "", _                 -> AWSClientFactory.CreateAmazonCloudWatchClient()
        | awsKey, awsSecret, ""     -> AWSClientFactory.CreateAmazonCloudWatchClient(awsKey, awsSecret, RegionEndpoint.USEast1)
        | awsKey, awsSecret, region -> AWSClientFactory.CreateAmazonCloudWatchClient(awsKey, awsSecret, RegionEndpoint.GetBySystemName region)

    loop cloudWatch None

    0 // return an integer exit code