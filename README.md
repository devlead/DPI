# DPI

Dependency Inventory .NET Tool - Inventories dependencies and reports to Azure Log Analytics

You can get introduction to the tool, in the blog post: [Introducing DPI - A DevOps tool to inspect dependencies and report to Azure Log Analytics](https://www.devlead.se/posts/2021/2021-03-20-introducing-dpi)

## Obtain

`dotnet tool install -g dpi`

## Commands

Use `-h` / `--help` to get current list of available commands and options.

```bash
dpi --help
```

Result:

<!-- snippet: IntegrationTest.Run_args=--help.verified.txt -->
<a id='snippet-IntegrationTest.Run_args=--help.verified.txt'></a>
```txt
USAGE:
    dpi [OPTIONS] <COMMAND>

EXAMPLES:
    dpi nuget <SourcePath> analyze
    dpi nuget <SourcePath> report

OPTIONS:
    -h, --help       Prints help information   
    -v, --version    Prints version information
                                               
COMMANDS:
    nuget    NuGet dependency commands
```
<sup><a href='/src/Tests/IntegrationTest.Run_args=--help.verified.txt#L1-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-IntegrationTest.Run_args=--help.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### nuget

The NuGet branch of commands have recursively from given path inventories packages present in

* csproj (project assets if restored)
* packages.config
* .NET Tool manifests
* Cake build script files

#### analyze

**nuget analyze** command inventories and outputs result to console or file.

```bash
dpi nuget --output table analyze
```

#### report

**nuget report** analyzes and reports result to Azure Log Analytics.

```bash
export NuGetReportSettings_WorkspaceId=<workspaceid>

export NuGetReportSettings_SharedKey=<sharedkey>

dpi nuget --output table report
```
