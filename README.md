[![Issue Stats](http://issuestats.com/github/fsprojects/Amazon.CloudWatch.Selector/badge/issue)](http://issuestats.com/github/fsprojects/Amazon.CloudWatch.Selector)
[![Issue Stats](http://issuestats.com/github/fsprojects/Amazon.CloudWatch.Selector/badge/pr)](http://issuestats.com/github/fsprojects/Amazon.CloudWatch.Selector)

Amazon.CloudWatch.Query
=======================

This library provides both internal and external DSL for querying against metrics stored in `Amazon CloudWatch`.

As part of the repo, there is also a simple **CLI tool** which you can use to interactively query your `CloudWatch` metrics using the external DSL and plot the resulting metrics on a graph.


### Maintainer(s)

- [@theburningmonk](https://github.com/theburningmonk)

The default maintainer account for projects under "fsprojects" is [@fsprojectsgit](https://github.com/fsprojectsgit) - F# Community Project Incubation Space (repo management)


### Getting Started

To get started, download the library from Nuget.
[![NuGet Status](http://img.shields.io/nuget/v/Amazon.CloudWatch.Selector.svg?style=flat)](https://www.nuget.org/packages/Amazon.CloudWatch.Selector/)
[![Nuget](https://raw.github.com/theburningmonk/Amazon.CloudWatch.Selector/develop/nuget/banner.png)](https://www.nuget.org/packages/Amazon.CloudWatch.Selector)

You can then use both the internal and external DSL via extension methods on an `IAmazonCloudWatch` instance.

For example, to answer the question such as
>  which latency metrics' average over 5 minute intervals were above 1000ms in the last 12 hours?

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



### The DSL Syntax

Both DSLs support the same set of operators:
<table>
	<tr>
		<td><strong>NamespaceIs</strong></td>
		<td>Filters metrics by the specified namespace.</td>
	</tr>
	<tr>
		<td><strong>NamespaceLike</strong></td>
		<td>Fil­ters met­rics using a regex pat­tern against their namespaces.</td>
	</tr>
	<tr>
		<td><strong>NameIs</strong></td>
		<td>Fil­ters met­rics by the spec­i­fied name.</td>
	</tr>
	<tr>
		<td><strong>NameLike</strong></td>
		<td>Fil­ters met­rics using a regex pat­tern against their names.</td>
	</tr>
	<tr>
		<td><strong>UnitIs</strong></td>
		<td>Fil­ters met­rics against the unit they’re recorded in, e.g. Count, Bytes, etc.</td>
	</tr>
	<tr>
		<td><strong>Average</strong></td>
		<td>Fil­ters met­rics by the recorded aver­age data points, e.g. aver­age > 300 looks for met­rics whose aver­age in the spec­i­fied time­frame exceeded 300 at any time.</td>
	</tr>
	<tr>
		<td><strong>Min</strong></td>
		<td>Same as above but for the min­i­mum data points.</td>
	</tr>
	<tr>
		<td><strong>Max</strong></td>
		<td>Same as above but for the max­i­mum data points.</td>
	</tr>
	<tr>
		<td><strong>Sum</strong></td>
		<td>Same as above but for the sum data points.</td>
	</tr>
	<tr>
		<td><strong>SampleCount</strong></td>
		<td>Same as above but for the sam­ple count data points.</td>
	</tr>
	<tr>
		<td><strong>DimensionContains</strong></td>
		<td>Fil­ters met­rics by the dimen­sions they’re recorded with, please refer to the Cloud­Watch docs on how this works.</td>
	</tr>
	<tr>
		<td><strong>DuringLast</strong></td>
		<td>Spec­i­fies the time­frame of the query to be the last X minutes/hours/days. Note: Cloud­Watch only keeps up to 14 days worth of data so there’s no point going any fur­ther back then that.</td>
	</tr>
	<tr>
		<td><strong>Since</strong></td>
		<td>Spec­i­fies the time­frame of the query to be since the spec­i­fied time­stamp till now.</td>
	</tr>
	<tr>
		<td><strong>Between</strong></td>
		<td>Spec­i­fies the time­frame of the query to be between the spec­i­fied start and end timestamp.</td>
	</tr>
	<tr>
		<td><strong>IntervalOf</strong></td>
		<td>Spec­i­fies the ‘period’ in which the data points will be aggre­gated into, i.e. 5 min­utes, 15 min­utes, 1 hour, etc.</td>
	</tr>
</table>

The **internal DSL** uses the `+` operator to concatenate filter conditions against the metrics, then the `@` operator to apply a time frame to the query, before finally using `|>` to pipe the query so far to the `intervalOf` function to specify an interval to group the metrics' data points by.

For time frames and intervals, you can specify the time with the units `minutes`, `hours` and `days`, e.g. `... @ last 2 days |> intervalOf 15 minutes`.



### Examples

Coming soon.



### Using the CLI

You can get the CLI tool using [Chocolatey](https://chocolatey.org/):

[![Chocolatey](https://raw.github.com/theburningmonk/Amazon.CloudWatch.Selector/develop/chocolatey/banner.png)](https://chocolatey.org/packages/cwcli)


1. install the CLI using Chocolatey
2. enter `cwcli` in command line
3. follow instructions to enter credential for your AWS account
4. run queries! 

Suppose you want to find CPU metrics for your `Amazon ElastiCache` clusters whose max CPU went over 30% at any point over the last 24 hours, you could write a query like this:

`namespacelike 'elasticache' and namelike 'cpu' and max > 30.0 duringLast 24 hours at intervalOf 15 minutes`

You can then plot the resulting metrics (if any) onto a graph,  simply type **plot** and hit return, e.g.
![Demo Screenshot](https://raw.github.com/theburningmonk/Amazon.CloudWatch.Selector/develop/docs/files/img/CLI_demo_screenshot.png)

Finally, to quit the CLI tool, type **exit** and hit return.

Here's a quick demo video of the CLI tool in action.
[![YouTube](https://raw.github.com/theburningmonk/Amazon.CloudWatch.Selector/develop/docs/files/img/youtube_demo_screenshot.png)](http://www.youtube.com/watch?v=XRtHfH26QQg)
