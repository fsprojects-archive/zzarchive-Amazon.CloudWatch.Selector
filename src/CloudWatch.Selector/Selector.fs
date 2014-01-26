namespace Amazon.CloudWatch.Selector

open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Text.RegularExpressions

open Amazon
open Amazon.CloudWatch
open Amazon.CloudWatch.Model

(*
    External DSL example:
        "namespaceLike 'count%' and unitIs 'milliseconds' and max >= 10000 duringLast 10 minutes"
*)

[<AutoOpen>]
module Model =
    type MetricTerm =
        | Namespace
        | Name

    type StatsTerm =
        | Average
        | Min
        | Max
        | Sum
        | SampleCount

    type Unit      = | Unit
    type Dimension = | Dimension

    type TimeFrame =
        | Last              of TimeSpan
        | Since             of DateTime
        | Between           of DateTime * DateTime

    type Filter =
        | MetricFilter      of MetricTerm * (string -> bool)
        | StatsFilter       of StatsTerm * (float -> bool)
        | UnitFilter        of Unit * (string -> bool)
        | DimensionFilter   of Dimension * (string * string)
        | CompositeFilter   of Filter * Filter

        static member (+) (lf : Filter, rt : Filter) = CompositeFilter (lf, rt)

    type Period = | Period  of TimeSpan

    type Query =
        {
            Filter      : Filter
            TimeFrame   : TimeFrame
            Period      : Period option
        }

    let (@) filter timeframe       = { Filter = filter; TimeFrame = timeframe; Period = None }

    let statistics = new List<string>([| "Average"; "Sum"; "SampleCount"; "Maximum"; "Minimum" |])
    let units      = Set [ "Seconds"; "Microseconds"; "Milliseconds"; 
                           "Bytes"; "Kilobytes"; "Megabytes"; "Gigabytes"; "Terabytes";
                           "Bits";  "Kilobits";  "Megabits";  "Gigabits";  "Terabits";
                           "Percent"; "Count";
                           "Bytes/Second"; "Kilobytes/Second"; "Megabytes/Second"; "Gigabytes/Second"; "Terabytes/Second";
                           "Bits/Second";  "Kilobits/Second";  "Megabits/Second";  "Gigabits/Second";  "Terabits/Second";
                           "Count/Second"; "None" ]

[<AutoOpen>]
module InternalDSL = 
    let private lowerCase (str : string) = str.ToLower()
    let private isRegexMatch pattern input = Regex.IsMatch(input, pattern)

    let inline private eqFilter filter term lf = filter <| (term, fun rt -> lowerCase lf = lowerCase rt)
    let inline private regexFilter filter term pattern = filter <| (term, fun input -> isRegexMatch (lowerCase pattern) (lowerCase input))

    let namespaceIs ns        = eqFilter MetricFilter Namespace ns
    let namespaceLike pattern = regexFilter MetricFilter Namespace pattern

    let nameIs name           = eqFilter MetricFilter Name name
    let nameLike pattern      = regexFilter MetricFilter Name pattern

    let unitIs unit           = eqFilter UnitFilter Unit unit

    let inline private statsFilter term op value = StatsFilter <| (term, fun x -> op x value)
    let average op value      = statsFilter Average op value
    let min op value          = statsFilter Min op value
    let max op value          = statsFilter Max op value
    let sum op value          = statsFilter Sum op value
    let sampleCount op value  = statsFilter SampleCount op value

    let dimensionContains dim = DimensionFilter(Dimension, dim)

    let inline minutes n      = n |> float |> TimeSpan.FromMinutes
    let inline hours n        = n |> float |> TimeSpan.FromHours
    let inline days n         = n |> float |> TimeSpan.FromDays

    let inline last n (unit : 'a -> TimeSpan) = unit n |> Last
    let since timestamp           = Since timestamp
    let between startTime endTime = Between(startTime, endTime)

    let inline intervalOf n (unit : 'a -> TimeSpan) query = { query with Period = Some (unit n |> Period) }

[<AutoOpen>]
module ExternalDSL =
    ()

[<AutoOpen>]
module Execution = 
    type AwsDimension = Amazon.CloudWatch.Model.Dimension

    let inline (<&&>) g f x = g x && f x
    let inline (<?&>) g f = match g with | Some g -> g <&&> f | _ -> f
    let inline (<&?>) g f = match f with | Some f -> g <&&> f | _ -> g

    let alwaysTrue = fun _ -> true

    let getMetricPred filter = 
        let rec loop filter (acc : (Metric -> bool) option) =
            match filter with
            | MetricFilter (term, pred) -> 
                match term with 
                | Namespace -> fun (m : Metric) -> pred m.Namespace
                | Name      -> fun (m : Metric) -> pred m.MetricName
                |> (<?&>) acc
                |> Some
            | CompositeFilter (lf, rt) ->
                // depth-first
                acc |> loop lf |> loop rt
            | _ -> acc
        
        loop filter None <?&> alwaysTrue

    let getDimensions filter = 
        let rec loop filter acc =
            match filter with
            | DimensionFilter (Dimension, dim) -> dim::acc
            | CompositeFilter (lf, rt) ->
                // depth-first
                acc |> loop lf |> loop rt
            | _ -> acc

        loop filter []

    let rec getUnit = function
        | UnitFilter (Unit, pred) -> units |> Seq.tryFind pred
        | CompositeFilter (lf, rt) ->
            // depth-first
            match getUnit lf with
            | None -> getUnit rt
            | x    -> x
        | _ -> None

    let getPeriod = function
        | Some (Period ts) -> 
            // NOTE : period must be at least 60 and a multiple of 60 
            // see http://docs.aws.amazon.com/AmazonCloudWatch/latest/APIReference/API_GetMetricStatistics.html
            Some <| (ceil ts.TotalMinutes) * 60.0
        | _ -> None

    let getDatapointPred filter =
        let rec loop filter (acc : (Datapoint -> bool) option) = 
            match filter with
            | StatsFilter (term, pred) ->
                match term with
                | Average     -> fun (dp : Datapoint) -> pred dp.Average
                | Min         -> fun (dp : Datapoint) -> pred dp.Minimum
                | Max         -> fun (dp : Datapoint) -> pred dp.Maximum
                | Sum         -> fun (dp : Datapoint) -> pred dp.Sum
                | SampleCount -> fun (dp : Datapoint) -> pred dp.SampleCount
                |> (<?&>) acc
                |> Some
            | CompositeFilter (lf, rt) ->
                // depth-first
                acc |> loop lf |> loop rt
            | _ -> acc

        loop filter None <?&> alwaysTrue

    let getMetrics (cloudWatch : IAmazonCloudWatch) filter =
        let dims = getDimensions filter |> Seq.map (fun (name, value) -> new DimensionFilter(Name = name, Value = value))

        let rec loop (acc : _ list) next = 
            async {
                let req  = new ListMetricsRequest()
                req.NextToken <- next
                req.Dimensions.AddRange dims

                let! res = cloudWatch.ListMetricsAsync(req) |> Async.AwaitTask

                match res.NextToken with
                | null  -> return (res.Metrics::acc) |> Seq.collect id
                | token -> return! loop (res.Metrics::acc) token
            }

        loop [] null

    let getMetricStats (cloudWatch : IAmazonCloudWatch) 
                       (metric : Metric) 
                       { Filter = filter; TimeFrame = timeframe; Period = period } =
        let pred = getDatapointPred filter

        async {
            let req = new GetMetricStatisticsRequest(MetricName = metric.MetricName, Namespace = metric.Namespace)

            let now = DateTime.UtcNow
            let startTime, endTime = 
                match timeframe with
                | Last ts       -> now.Add(-ts), now
                | Since dt      -> dt, now
                | Between(a, b) -> a, b
            req.StartTime <- startTime
            req.EndTime   <- endTime

            match getPeriod period with
            | Some seconds -> req.Period <- int seconds
            | _ -> ()
            
            req.Statistics <- statistics
            req.Dimensions <- metric.Dimensions

            match getUnit filter with
            | Some unit -> req.Unit <- new StandardUnit(unit)
            | _ -> ()

            let! res = cloudWatch.GetMetricStatisticsAsync(req) |> Async.AwaitTask

            if res.Datapoints |> Seq.exists pred
            then return Some (metric, res.Datapoints)
            else return None
        }

    let select (cloudWatch : IAmazonCloudWatch) ({ Filter = filter } as query) = 
        let metricPred = getMetricPred filter

        async {
            let! metrics = getMetrics cloudWatch filter
            let metrics  = metrics |> Seq.filter metricPred |> Seq.toArray

            let! results = 
                metrics 
                |> Array.map (fun m -> getMetricStats cloudWatch m query) 
                |> Async.Parallel
                
            return results |> Array.choose id
        }

    /// F#-friendly extension methods
    type IAmazonCloudWatch with
        member this.Select(query : Query)  = select this query
        member this.Select(query : string) = ()

    /// C#-friendely extension methods
    [<Extension>]
    [<AbstractClass>]
    [<Sealed>]
    type AmazonCloudWatchContextExt =
        [<Extension>]
        static member Select (cloudWatch : IAmazonCloudWatch, query : Query) = cloudWatch.Select query |> Async.StartAsTask

        [<Extension>]
        static member Select (cloudWatch : IAmazonCloudWatch, query : string) = ()