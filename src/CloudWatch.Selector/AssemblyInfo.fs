namespace System
open System.Reflection
open System.Runtime.CompilerServices

[<assembly: AssemblyTitleAttribute("Amazon.CloudWatch.Selector")>]
[<assembly: AssemblyProductAttribute("Amazon.CloudWatch.Selector")>]
[<assembly: AssemblyDescriptionAttribute("Extension library for AWSSDK that allows you to select CloudWatch metrics with simple queries")>]
[<assembly: AssemblyVersionAttribute("0.2.0")>]
[<assembly: AssemblyFileVersionAttribute("0.2.0")>]
[<assembly: InternalsVisibleToAttribute("Amazon.CloudWatch.Selector.Tests")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.2.0"
