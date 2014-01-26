#r "bin/Debug/AWSSDK.dll"
#r "bin/Debug/Amazon.CloudWatch.Selector.dll"

open Amazon

open Amazon.CloudWatch.Selector

let awsKey     = "AKIAITHCDJ64PL5FZA7Q"
let awsSecret  = "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"
let region     = RegionEndpoint.USEast1
let cloudWatch = AWSClientFactory.CreateAmazonCloudWatchClient(awsKey, awsSecret, region)

// using the internal DSL
let res = cloudWatch.Select(namespaceLike "iwi" + nameLike "total" + sampleCount (>) 50.0 @ last 24 hours |> intervalOf 5 minutes) 
          |> Async.RunSynchronously

