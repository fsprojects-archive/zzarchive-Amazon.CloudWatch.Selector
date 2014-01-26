namespace Amazon.CloudWatch.Selector

open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open System.Threading.Tasks

open Amazon.CloudWatch
open Amazon.CloudWatch.Model

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
        with
            static member (+) : Filter * Filter -> Filter
        end

    type Period = | Period  of TimeSpan

    type Query =
        {
            Filter      : Filter
            TimeFrame   : TimeFrame
            Period      : Period option
        }

    val (@) : filter:Filter -> timeframe:TimeFrame -> Query

[<AutoOpen>]
module InternalDSL =
    val namespaceIs       : ns:string       -> Filter
    val namespaceLike     : pattern:string  -> Filter

    val nameIs            : name:string     -> Filter
    val nameLike          : pattern:string  -> Filter

    val unitIs            : unit:string     -> Filter

    val average           : op:(float -> 'a -> bool) -> value:'a -> Filter
    val min               : op:(float -> 'a -> bool) -> value:'a -> Filter
    val max               : op:(float -> 'a -> bool) -> value:'a -> Filter
    val sum               : op:(float -> 'a -> bool) -> value:'a -> Filter
    val sampleCount       : op:(float -> 'a -> bool) -> value:'a -> Filter

    val dimensionContains : string * string -> Filter

    val inline minutes    : n:^a -> TimeSpan when ^a : (static member op_Explicit : ^a -> float)
    val inline hours      : n:^a -> TimeSpan when ^a : (static member op_Explicit : ^a -> float)
    val inline days       : n:^a -> TimeSpan when ^a : (static member op_Explicit : ^a -> float)
    val inline last       : n:'a -> ('a -> TimeSpan) -> TimeFrame
    val since             : timestamp:DateTime -> TimeFrame
    val between           : startTime:DateTime -> endTime:DateTime -> TimeFrame

    val inline intervalOf : n:'a -> unit:('a -> TimeSpan) -> query:Query -> Query

[<AutoOpen>]
module Execution = 
    type IAmazonCloudWatch with
        member Select : Query -> Async<(Metric * List<Datapoint>)[]>

    [<Extension>]
    [<AbstractClass>]
    [<Sealed>]
    type AmazonCloudWatchContextExt =
        [<Extension>]
        static member Select : IAmazonCloudWatch * Query -> Task<(Metric * List<Datapoint>) []>