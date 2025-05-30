# DPI

[![NuGet](https://img.shields.io/nuget/v/dpi.svg)](https://www.nuget.org/packages/dpi)

Dependency Inventory .NET Tool - Inventories dependencies and reports to Azure Log Analytics

You can get an introduction to the tool, in the blog post: [Introducing DPI - A DevOps tool to inspect dependencies and report to Azure Log Analytics](https://www.devlead.se/posts/2021/2021-03-20-introducing-dpi)

## Obtain

`dotnet tool install -g dpi`

## Commands

Use `-h` / `--help` to get the current list of available commands and options.

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
# Output as table (default)
dpi nuget --output table analyze

# Output as JSON
dpi nuget --output json analyze

# Output as Markdown
dpi nuget --output markdown analyze

# Save output to file
dpi nuget --output json --file dependencies.json analyze
```

#### report

**nuget report** analyzes and reports the result to Azure Log Analytics.

```bash
export NuGetReportSettings_WorkspaceId=<workspaceid>

export NuGetReportSettings_SharedKey=<sharedkey>

# Output as table (default)
dpi nuget --output table report

# Output as JSON
dpi nuget --output json report

# Output as Markdown
dpi nuget --output markdown report

# Save output to file
dpi nuget --output json --file report.json report
```
