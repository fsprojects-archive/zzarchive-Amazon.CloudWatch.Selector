namespace Amazon.CloudWatch.Selector.Tests

open FsUnit
open NUnit.Framework
open Amazon.CloudWatch.Selector

[<TestFixture>]
type ``Given an internal DSL expression`` () = 
    [<Test>]
    member this.``namespaceIs creates a predicate for case-insensitive equality comparison`` () =
        let filter = namespaceIs "namespace"
        let (MetricFilter(term, pred)) = filter

        term |> should equal Namespace
        pred "namespace" |> should equal true
        pred "NameSpace" |> should equal true

    [<Test>]
    member this.``namespaceLike creates a predicate for case-insensitive regex match`` () =
        let filter = namespaceLike "selector"
        let (MetricFilter(term, pred)) = filter

        term |> should equal Namespace
        pred "selector"         |> should equal true
        pred "SelEctOr"         |> should equal true
        pred "my_selector"      |> should equal true
        pred "my_selector_2"    |> should equal true        

    [<Test>]
    member this.``+ operator work combines two filters into a composite filter`` () =
        let filter = namespaceIs "namespace" + nameIs "name"
        let (CompositeFilter(lf, rt)) = filter
        
        match lf with | MetricFilter(Namespace, _) -> true
        |> should equal true

        match rt with | MetricFilter(Name, _) -> true
        |> should equal true

    [<Test>]
    member this.``+ operator work combines multiple filters into a list`` () =
        let filter = namespaceIs "namespace" + nameIs "name" + unitIs "count"
        let (CompositeFilter(lf, rt)) = filter

        match lf with | CompositeFilter(_, _) -> true
        |> should equal true

        match rt with | UnitFilter(Unit, _) -> true
        |> should equal true

        let (CompositeFilter(lf', rt')) = lf
        match lf' with | MetricFilter(Namespace, _) -> true
        |> should equal true

        match rt' with | MetricFilter(Name, _) -> true
        |> should equal true