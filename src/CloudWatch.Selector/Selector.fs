module Amazon.CloudWatch.Selector

open System.Runtime.CompilerServices
open System.Text.RegularExpressions

open Amazon
open Amazon.CloudWatch
open Amazon.CloudWatch.Model

(*
    External DSL example:
        "namespaceLike 'count%' and unitIs 'milliseconds' and maxGreaterThan 10000 duringLast 10 minutes"

    internal DSL example:
        (fun m -> m.namespace like "count%" and m.unit = "milliseconds" and m.max >= 10000 during(10<min>))
*)

let cloudWatch = Amazon.AWSClientFactory.CreateAmazonCloudWatchClient()

let res = cloudWatch.ListMetrics(new ListMetricsRequest())
let m = res.Metrics.[0]
let req' = new GetMetricStatisticsRequest()
let res' = cloudWatch.GetMetricStatistics(req')
let dp = res'.Datapoints.[0]

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

    type Unit = 
        | Unit

    type Filter =
        | MetricFilter      of MetricTerm * (string -> bool)
        | StatsFilter       of StatsTerm * (float -> bool)
        | UnitFilter        of Unit * (string -> bool)
        | CompositeFilter   of Filter * Filter

        static member (+) (lf : Filter, rt : Filter) = CompositeFilter (lf, rt)

[<AutoOpen>]
module InternalDSL = 
    let private lowerCase (str : string) = str.ToLower()
    let private isRegexMatch pattern input = Regex.IsMatch(input, pattern)

    let inline private eqFilter filter term lf = filter <| (term, fun rt -> lowerCase lf = lowerCase rt)
    let inline private regexFilter filter term pattern = filter <| (term, fun input -> isRegexMatch (lowerCase pattern) (lowerCase input))

    let namespaceIs ns        = eqFilter MetricFilter Namespace ns
    let namespaceLike pattern = regexFilter MetricFilter Namespace pattern

    let nameIs name       = eqFilter MetricFilter Name name
    let nameLike pattern  = regexFilter MetricFilter Name pattern

    let unitIs unit       = eqFilter UnitFilter Unit unit
    let unitLike pattern  = regexFilter UnitFilter Unit pattern

    let inline private statsFilter term op value = StatsFilter <| (term, fun x -> op x value)

    let average op value     = statsFilter Average op value
    let min op value         = statsFilter Min op value
    let max op value         = statsFilter Max op value
    let sum op value         = statsFilter Sum op value
    let sampleCount op value = statsFilter SampleCount op value

[<AutoOpen>]
module ExternalDSL =
    ()

[<AutoOpen>]
module Execution =
    /// F#-friendly extension method for using the internal DSL
    type IAmazonCloudWatch with
        member this.Select(filter : Filter) = ()

    /// C#-friendely extension method for using the external DSL
    [<Extension>]
    [<AbstractClass>]
    [<Sealed>]
    type AmazonCloudWatchContextExt =
        [<Extension>]
        static member Select (cw : IAmazonCloudWatch, selector : string) = ()