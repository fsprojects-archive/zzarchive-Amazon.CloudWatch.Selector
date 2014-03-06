namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("AWS CloudWatch Selector CLI")>]
[<assembly: AssemblyProductAttribute("cwcli")>]
[<assembly: AssemblyDescriptionAttribute("CLI for querying metrics in Amazon CloudWatch using a simple DSL.")>]
[<assembly: AssemblyVersionAttribute("0.2.0")>]
[<assembly: AssemblyFileVersionAttribute("0.2.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.2.0"
