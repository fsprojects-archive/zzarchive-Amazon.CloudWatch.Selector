namespace Amazon.CloudWatch.Selector.Tests

open System
open FsUnit
open NUnit.Framework
open Amazon.CloudWatch.Selector

[<TestFixture>]
type ``Given an internal DSL expression`` () = 
    let testStatsFilter f term = 
        let testCase op eq gt lt =
            let (StatsFilter(term', pred)) = f op 42.0
        
            term' |> should equal term
            pred 42.0       |> should equal eq
            pred 41.0       |> should equal lt
            pred 43.0       |> should equal gt

        testCase (=)  true  false false
        testCase (>)  false true  false
        testCase (>=) true  true  false
        testCase (<)  false false true
        testCase (<=) true  false true

    [<Test>]
    member test.``namespaceIs should create a predicate for case-insensitive equality comparison`` () =
        let (MetricFilter(term, pred)) = namespaceIs "namespace"

        term |> should equal Namespace
        pred "namespace" |> should equal true
        pred "NameSpace" |> should equal true

    [<Test>]
    member test.``namespaceLike should create a predicate for case-insensitive regex match`` () =
        let (MetricFilter(term, pred)) = namespaceLike "selector"

        term |> should equal Namespace
        pred "selector"         |> should equal true
        pred "SelEctOr"         |> should equal true
        pred "my_selector"      |> should equal true
        pred "my_selector_2"    |> should equal true

    [<Test>]
    member test.``nameIs should create a predicate for case-insensitive equality comparison`` () =
        let (MetricFilter(term, pred)) = nameIs "name"

        term |> should equal Name
        pred "name" |> should equal true
        pred "NaMe" |> should equal true

    [<Test>]
    member test.``nameLike should create a predicate for case-insensitive regex match`` () =
        let (MetricFilter(term, pred)) = nameLike "selector$"

        term |> should equal Name
        pred "selector"         |> should equal true
        pred "SelEctOr"         |> should equal true
        pred "my_selector"      |> should equal true
        pred "my_selector_2"    |> should equal false

    [<Test>]
    member test.``unitIs ahould create a predicate for case-insensitive equality comparison`` () =
        let (UnitFilter(term, pred)) = unitIs "count"

        term |> should equal Unit
        pred "count" |> should equal true
        pred "CouNt" |> should equal true

    [<Test>]
    member test.``average should create a predicate for numeric comparison`` () = testStatsFilter average Average

    [<Test>]
    member test.``min should create a predicate for numeric comparison`` () = testStatsFilter min Min

    [<Test>]
    member test.``max should create a predicate for numeric comparison`` () = testStatsFilter max Max

    [<Test>]
    member test.``sum should create a predicate for numeric comparison`` () = testStatsFilter sum Sum

    [<Test>]
    member test.``sampleCount should create a predicate for numeric comparison`` () = testStatsFilter sampleCount SampleCount

    [<Test>]
    member test.``last should create a timeframe for last x minutes`` () =
        let (Last ts) = last 5 minutes
        ts |> should equal <| TimeSpan.FromMinutes 5.0

        let (Last ts) = last 5.0 minutes
        ts |> should equal <| TimeSpan.FromMinutes 5.0

    [<Test>]
    member test.``since should create a timeframe with the specified start time`` () =
        let now = DateTime.UtcNow
        let (Since ts) = since now

        ts |> should equal now

    [<Test>]
    member test.``between should create a timeframe with the specified start and end time`` () =
        let now = DateTime.UtcNow
        let (Between(startTime, endTime)) = between (now.AddMinutes -5.0) now

        startTime |> should equal (now.AddMinutes -5.0)
        endTime   |> should equal now

    [<Test>]
    member test.``+ operator should combine two filters into a composite filter`` () =
        let (CompositeFilter(lf, rt)) = namespaceIs "namespace" + nameIs "name"
        
        match lf with | MetricFilter(Namespace, _) -> true
        |> should equal true

        match rt with | MetricFilter(Name, _) -> true
        |> should equal true

    [<Test>]
    member test.``+ operator should combine multiple filters into a list`` () =
        let (CompositeFilter(lf, rt)) = namespaceIs "namespace" + nameIs "name" + unitIs "count"

        match lf with | CompositeFilter(_, _) -> true
        |> should equal true

        match rt with | UnitFilter(Unit, _) -> true
        |> should equal true

        let (CompositeFilter(lf', rt')) = lf
        match lf' with | MetricFilter(Namespace, _) -> true
        |> should equal true

        match rt' with | MetricFilter(Name, _) -> true
        |> should equal true

    [<Test>]
    member test.``@ operator should combine a filter with a time frame`` () =
        let { Filter = filter; TimeFrame = timeframe } = namespaceIs "namespace" @ last 3 minutes       

        match filter with | MetricFilter(Namespace, _) -> true
        |> should equal true

        match timeframe with | Last _ -> true
        |> should equal true

        match timeframe with 
        | Last ts -> ts |> should equal <| TimeSpan.FromMinutes 3.0

    [<Test>]
    member test.``@ and + operators should combine multiple filter with a time frame`` () =
        let { Filter = filter; TimeFrame = timeframe } = namespaceIs "namespace" + nameIs "name" @ last 3 minutes       

        match filter with 
        | CompositeFilter(MetricFilter(Namespace, _), MetricFilter(Name, _)) -> true
        |> should equal true

        match timeframe with | Last _ -> true
        |> should equal true

        match timeframe with 
        | Last ts -> ts |> should equal <| TimeSpan.FromMinutes 3.0