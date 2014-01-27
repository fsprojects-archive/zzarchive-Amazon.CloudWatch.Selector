#r "bin/Debug/AWSSDK.dll"
#r "bin/Debug/Amazon.CloudWatch.Selector.dll"

open Amazon

open Amazon.CloudWatch.Selector

let awsKey     = "AKIAITHCDJ64PL5FZA7Q"
let awsSecret  = "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"
let region     = RegionEndpoint.USEast1
let cloudWatch = AWSClientFactory.CreateAmazonCloudWatchClient(awsKey, awsSecret, region)

// using the internal DSL
//let res = cloudWatch.Select(namespaceLike "iwi" + nameLike "total" + sampleCount (>) 50.0 @ last 24 hours |> intervalOf 5 minutes) 
//          |> Async.RunSynchronously

// using the internal DSL
//let res' = cloudWatch.Select(namespaceLike "elasticache" + nameLike "cpu" + dimensionContains ("CacheNodeId", "0001") + max (>) 30.0 @ last 24 hours |> intervalOf 15 minutes)
//          |> Async.RunSynchronously

// using the external DSL
//let res'' = cloudWatch.Select("namespacelike 'elasticache' and namelike 'cpu' and dimensionContains 'CacheNodeId' '0001' and max > 30.0 duringLast 24 hours at intervalOf 15 minutes")
//            |> Async.RunSynchronously