Amazon.CloudWatch.Query
=======================

This library provides both internal and external DSL for querying against metrics stored in `Amazon CloudWatch`.

As part of the repo, there is also a simple **CLI tool** which you can use to interactively query your `CloudWatch` metrics using the external DSL and plot the resulting metrics on a graph.

### Getting Started

To get started, download the library from Nuget.
[![Nuget](https://raw.github.com/theburningmonk/Amazon.CloudWatch.Selector/develop/nuget/banner.png)](https://www.nuget.org/packages/Amazon.CloudWatch.Selector)

You can then use both the internal and external DSL via extension methods on an `IAmazonCloudWatch` instance.

For example, to answer the question such as
>  which latency metrics' average over 5 minute intervals were above 1000ms in the last 12 hours 

you can write a query like this in F#:
```fsharp
open Amazon
open Amazon.CloudWatch.Selector

let awsKey     = "YOUR_AWS_KEY"
let awsSecret  = "YOUR_AWS_SECRET"
let region     = RegionEndpoint.USEast1
let cloudWatch = AWSClientFactory.CreateAmazonCloudWatchClient(awsKey, awsSecret, region)

// using the internal DSL
let internalDslRes = 
    cloudWatch.Select(unitIs "milliseconds" + average (>) 1000.0 @ last 12 hours |> intervalOf 5 minutes) 
    |> Async.RunSynchronously

// using the external DSL
let externalDslRes = 
    cloudWatch.Select("unitIs 'milliseconds' and average > 1000.0 duringLast 12 hours at intervalOf 5 minutes")
    |> Async.RunSynchronously
```





### Using the CLI

To start the CLI:

1. set the AWS key and secret, and region for your account in the **start_cli.cmd** script
2. run the **start_cli.cmd** script (this will build the solution and then start the CLI)

Suppose you want to find CPU metrics for your `Amazon ElastiCache` clusters whose max CPU went over 30% at any point over the last 24 hours, you could write a query like this:

`namespacelike 'elasticache' and namelike 'cpu' and max > 30.0 duringLast 24 hours at intervalOf 15 minutes`

You can then plot the resulting metrics (if any) onto a graph,  simply type **plot** and hit return, e.g.
![Demo Screenshot](https://raw.github.com/theburningmonk/Amazon.CloudWatch.Selector/develop/docs/files/img/CLI_demo_screenshot.png)

Finally, to quit the CLI tool, type **exit** and hit return.

Here's a quick demo video of the CLI tool in action.
[![YouTube](https://raw.github.com/theburningmonk/Amazon.CloudWatch.Selector/develop/docs/files/img/youtube_demo_screenshot.png)](http://www.youtube.com/watch?v=XRtHfH26QQg)

[![Bitdeli Badge](https://d2weczhvl823v0.cloudfront.net/theburningmonk/amazon.cloudwatch.selector/trend.png)](https://bitdeli.com/free "Bitdeli Badge")