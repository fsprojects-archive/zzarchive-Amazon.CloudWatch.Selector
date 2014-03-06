@echo off

cls
if not exist packages\FAKE\tools\Fake.exe (
  .nuget\nuget.exe install FAKE -OutputDirectory packages -ExcludeVersion
)
packages\FAKE\tools\FAKE.exe build.fsx CLI

cd "cli"

Amazon.CloudWatch.Selector.Cli.exe -awsKey YOUR_AWS_KEY -awsSecret YOUR_AWS_SECRET -region YOUR_AWS_REGION
pause
