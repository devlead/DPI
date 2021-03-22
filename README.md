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
