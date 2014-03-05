#r "bin/Debug/AWSSDK.dll"
#r "bin/Debug/Amazon.CloudWatch.Selector.dll"

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

// using the internal DSL
//let res' = cloudWatch.Select(namespaceLike "elasticache" + nameLike "cpu" + dimensionContains ("CacheNodeId", "0001") + max (>) 30.0 @ last 24 hours |> intervalOf 15 minutes)
//          |> Async.RunSynchronously