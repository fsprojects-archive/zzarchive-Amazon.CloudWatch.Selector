namespace System
open System.Reflection
open System.Runtime.CompilerServices

[<assembly: AssemblyTitleAttribute("CloudWatch.Selector")>]
[<assembly: AssemblyProductAttribute("CloudWatch.Selector")>]
[<assembly: AssemblyDescriptionAttribute("Extension library for AWSSDK that allows you to select CloudWatch metrics with simple queries")>]
[<assembly: AssemblyVersionAttribute("0.1.0")>]
[<assembly: AssemblyFileVersionAttribute("0.1.0")>]
[<assembly: InternalsVisibleToAttribute("CloudWatch.Selector.Tests")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.1.0"
