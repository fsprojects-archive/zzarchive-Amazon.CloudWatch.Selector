namespace Amazon.CloudWatch.Selector.Tests

open System
open System.Globalization
open FsUnit
open NUnit.Framework
open Amazon.CloudWatch.Selector

[<TestFixture>]
type ``Given an external DSL expression`` () = 
    let dtFormat = "yyyy-MM-dd HH:mm:ss"

    let testStatsFilter stat term = 
        let testCase op eq gt lt =
            let query = sprintf "%s %s 42.0" stat op
            let (StatsFilter(term', pred)), tokens = tokenize query |> parseFilter
        
            term'       |> should equal term
            tokens      |> should haveLength 0
            pred 42.0   |> should equal eq
            pred 41.0   |> should equal lt
            pred 43.0   |> should equal gt

        testCase "="  true  false false
        testCase ">"  false true  false
        testCase ">=" true  true  false
        testCase "<"  false false true
        testCase "<=" true  false true
    
    [<Test>]
    member test.``tokenize should split tokens by space`` () =
        tokenize "Mary had a little lamb"
        |> should equal [ "Mary"; "had"; "a"; "little"; "lamb" ]

    [<Test>]
    member test.``tokenize should handle leading whitespaces`` () =
        tokenize "  Mary had a little lamb"
        |> should equal [ "Mary"; "had"; "a"; "little"; "lamb" ]

    [<Test>]
    member test.``tokenize should handle trailing whitespaces`` () =
        tokenize "Mary had a little lamb   "
        |> should equal [ "Mary"; "had"; "a"; "little"; "lamb" ]

    [<Test>]
    member test.``tokenize should handle duplicated whitespaces`` () =
        tokenize "Mary   had   a   little   lamb"
        |> should equal [ "Mary"; "had"; "a"; "little"; "lamb" ]

    [<Test>]
    member test.``tokenize should handle spaces between single quotes`` () =
        tokenize "the time was '1997-09-01 12:00:04' "
        |> should equal [ "the"; "time"; "was"; "'1997-09-01 12:00:04'" ]

    [<Test>]
    member test.``namespaceIs should create a predicate for case-insensitive equality comparison`` () =
        let (MetricFilter(term, pred)), tokens = tokenize "namespaceIs 'namespace'" |> parseFilter

        term    |> should equal Namespace
        tokens  |> should haveLength 0
        pred "namespace" |> should equal true
        pred "NameSpace" |> should equal true

    [<Test>]
    member test.``namespaceLike should create a predicate for case-insensitive regex match`` () =
        let (MetricFilter(term, pred)), tokens = tokenize "namespaceLike 'selector'" |> parseFilter

        term    |> should equal Namespace
        tokens  |> should haveLength 0
        pred "selector"         |> should equal true
        pred "SelEctOr"         |> should equal true
        pred "my_selector"      |> should equal true
        pred "my_selector_2"    |> should equal true
    
    [<Test>]
    member test.``nameIs should create a predicate for case-insensitive equality comparison`` () =
        let (MetricFilter(term, pred)), tokens = tokenize "nameIs 'name'" |> parseFilter

        term    |> should equal Name
        tokens  |> should haveLength 0
        pred "name" |> should equal true
        pred "NaMe" |> should equal true

    [<Test>]
    member test.``nameLike should create a predicate for case-insensitive regex match`` () =
        let (MetricFilter(term, pred)), tokens = tokenize "nameLike 'selector$'" |> parseFilter

        term    |> should equal Name
        tokens  |> should haveLength 0
        pred "selector"         |> should equal true
        pred "SelEctOr"         |> should equal true
        pred "my_selector"      |> should equal true
        pred "my_selector_2"    |> should equal false

    [<Test>]
    member test.``unitIs ahould create a predicate for case-insensitive equality comparison`` () =
        let (UnitFilter(term, pred)), tokens = tokenize "unitIs 'count'" |> parseFilter

        term    |> should equal Unit
        tokens  |> should haveLength 0
        pred "count" |> should equal true
        pred "CouNt" |> should equal true
    
    [<Test>]
    member test.``average should create a predicate for numeric comparison`` () = testStatsFilter "average" Average

    [<Test>]
    member test.``min should create a predicate for numeric comparison`` () = testStatsFilter "min" Min

    [<Test>]
    member test.``max should create a predicate for numeric comparison`` () = testStatsFilter "max" Max

    [<Test>]
    member test.``sum should create a predicate for numeric comparison`` () = testStatsFilter "sum" Sum

    [<Test>]
    member test.``sampleCount should create a predicate for numeric comparison`` () = testStatsFilter "sampleCount" SampleCount
    
    [<Test>]
    member test.``dimensionContains should create a dimension filter`` () =
        let (DimensionFilter(term, dim)), tokens = tokenize "dimensionContains 'name' 'value'" |> parseFilter

        term    |> should equal Dimension
        tokens  |> should haveLength 0
        dim     |> should equal ("name", "value")
    
    [<Test>]
    member test.``duringLast should create a timeframe for last x minutes`` () =
        let testQuery queryStr = 
            let query, tokens = tokenize queryStr |> parseFilter |> parseTimeFrame
        
            query.Period |> should equal None
            tokens       |> should haveLength 0
        
            match query.Filter with | MetricFilter (Name, _) -> true
            |> should equal true
        
            query.TimeFrame |> should equal <| Last(TimeSpan.FromMinutes 5.0)

        testQuery "nameIs 'name' duringLast 5 minutes"
        testQuery "nameIs 'name' duringLast 5.0 minutes"
        
    [<Test>]
    member test.``since should create a timeframe with the specified start time`` () =
        let now    = DateTime.UtcNow.ToString(dtFormat)
        let query, tokens = sprintf "nameIs 'name' since '%s'" now |> tokenize |> parseFilter |> parseTimeFrame
        
        query.Period |> should equal None
        tokens       |> should haveLength 0
        
        match query.Filter with | MetricFilter (Name, _) -> true
        |> should equal true
        
        let now' = DateTime.ParseExact(now, dtFormat, CultureInfo.CurrentCulture)
        query.TimeFrame |> should equal <| Since now'
    
    [<Test>]
    member test.``between should create a timeframe with the specified start and end time`` () =
        let endTime   = DateTime.UtcNow
        let startTime = endTime.AddMinutes -5.0
        let startTime, endTime = startTime.ToString(dtFormat), endTime.ToString(dtFormat)
        let query, tokens = sprintf "nameIs 'name' between '%s' '%s'" startTime endTime
                            |> tokenize 
                            |> parseFilter 
                            |> parseTimeFrame
        
        query.Period |> should equal None
        tokens       |> should haveLength 0
        
        match query.Filter with | MetricFilter (Name, _) -> true
        |> should equal true
        
        let startTime' = DateTime.ParseExact(startTime, dtFormat, CultureInfo.CurrentCulture)
        let endTime'   = DateTime.ParseExact(endTime, dtFormat, CultureInfo.CurrentCulture)
        query.TimeFrame |> should equal <| Between(startTime', endTime')
        
    [<Test>]
    member test.``multiple filters should be combined with 'and'`` () =
        let filter, tokens = tokenize "namespaceIs 'namespace' and nameLike 'iwi'" |> parseFilter

        tokens  |> should haveLength 0

        match filter with | CompositeFilter(lf, rt) -> true
        |> should equal true

        let (CompositeFilter(lf, rt))  = filter
        let (MetricFilter(term, pred)) = lf

        term |> should equal Namespace        
        pred "namespace" |> should equal true
        pred "NameSpace" |> should equal true

        let (MetricFilter(term, pred)) = rt

        term |> should equal Name        
        pred "iwi" |> should equal true
        pred "IwI" |> should equal true

    [<Test>]
    member test.``intervalOf should create a period with the specified unit of time`` () =
        let query = parse "nameIs 'name' duringLast 5 minutes at intervalOf 1 minutes"
        let { Filter = filter; TimeFrame = timeframe; Period = period } = query

        match filter with | MetricFilter(Name, _) -> true
        |> should equal true

        timeframe |> should equal <| Last(TimeSpan.FromMinutes 5.0)