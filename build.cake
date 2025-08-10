#tool "dotnet:https://api.nuget.org/v3/index.json?package=GitVersion.Tool&version=6.4.0"
#addin nuget:?package=System.Text.Json&version=9.0.8&loaddependencies=true
#load "build/records.cake"
#load "build/helpers.cake"

/*****************************
 * Setup
 *****************************/
Setup(
    static context => {
         var assertedVersions = context.GitVersion(new GitVersionSettings
            {
                OutputType = GitVersionOutput.Json
            });

        var branchName = assertedVersions.BranchName;
        var isMainBranch = StringComparer.OrdinalIgnoreCase.Equals("main", branchName);

        var gh = context.GitHubActions();
        var buildDate = DateTime.UtcNow;
        var runNumber = gh.IsRunningOnGitHubActions
                            ? gh.Environment.Workflow.RunNumber
                            : (short)((buildDate - buildDate.Date).TotalSeconds/3);

        var version = FormattableString
                    .Invariant($"{buildDate:yyyy.M.d}.{runNumber}");

        context.Information("Building version {0} (Branch: {1}, IsMain: {2})",
            version,
            branchName,
            isMainBranch);

        var artifactsPath = context
                            .MakeAbsolute(context.Directory("./artifacts"));

        return new BuildData(
            version,
            isMainBranch,
            !context.IsRunningOnWindows(),
            "src",
            new DotNetMSBuildSettings()
                .SetConfiguration("Release")
                .SetVersion(version)
                .WithProperty("PackAsTool", "true")
                .WithProperty("PackageId", "DPI")
                .WithProperty("Copyright", $"Mattias Karlsson © {DateTime.UtcNow.Year}")
                .WithProperty("ToolCommandName", "dpi")
                .WithProperty("Authors", "devlead")
                .WithProperty("Company", "devlead")
                .WithProperty("PackageLicenseExpression", "MIT")
                .WithProperty("PackageTags", "tool")
                .WithProperty("PackageDescription", "Dependency Inventory .NET Tool - Inventories dependencies to Azure Log Analytics")
                .WithProperty("RepositoryUrl", "https://github.com/devlead/DPI.git")
                .WithProperty("ContinuousIntegrationBuild", gh.IsRunningOnGitHubActions ? "true" : "false")
                .WithProperty("EmbedUntrackedSources", "true"),
            artifactsPath,
            artifactsPath.Combine(version)
            );
    }
);

/*****************************
 * Tasks
 *****************************/
Task("Clean")
    .Does<BuildData>(
        static (context, data) => context.CleanDirectories(data.DirectoryPathsToClean)
    )
.Then("Restore")
    .Does<BuildData>(
        static (context, data) => context.DotNetRestore(
            data.ProjectRoot.FullPath,
            new DotNetRestoreSettings {
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("Build")
    .Does<BuildData>(
        static (context, data) => context.DotNetBuild(
            data.ProjectRoot.FullPath,
            new DotNetBuildSettings {
                NoRestore = true,
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("Pack")
    .Does<BuildData>(
        static (context, data) => context.DotNetPack(
            data.ProjectRoot.FullPath,
            new DotNetPackSettings {
                NoBuild = true,
                NoRestore = true,
                OutputDirectory = data.NuGetOutputPath,
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("Upload-Artifacts")
    .WithCriteria(BuildSystem.IsRunningOnGitHubActions, nameof(BuildSystem.IsRunningOnGitHubActions))
    .Does<BuildData>(
        static (context, data) => context
            .GitHubActions() is var gh && gh != null
                ?   gh.Commands
                    .UploadArtifact(data.ArtifactsPath,  $"Artifact_{gh.Environment.Runner.ImageOS ?? gh.Environment.Runner.OS}_{context.Environment.Runtime.BuiltFramework.Identifier}_{context.Environment.Runtime.BuiltFramework.Version}")
                : throw new Exception("GitHubActions not available")
    )
.Then("Integration-Tests-Restore-MultiTarget")
    .Does<BuildData>(
        static (context, data) => context.DotNetRestore(
            "./resources/src/MultiTarget/MultiTarget.csproj",
            new DotNetRestoreSettings {
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("Integration-Tests-Tool-Manifest")
    .Does<BuildData>(
        static (context, data) => context.DotNetTool(
                "new",
                new DotNetToolSettings {
                    ArgumentCustomization = args => args
                                                        .Append("tool-manifest"),
                    WorkingDirectory = data.IntegrationTestPath
                }
            )
    )
.Then("Integration-Tests-Tool-Install")
    .Does<BuildData>(
        static (context, data) =>  context.DotNetTool(
                "tool",
                new DotNetToolSettings {
                    ArgumentCustomization = args => args
                                                        .Append("install")
                                                        .AppendSwitchQuoted("--add-source", data.NuGetOutputPath.FullPath)
                                                        .AppendSwitchQuoted("--version", data.Version)
                                                        .Append("dpi"),
                    WorkingDirectory = data.IntegrationTestPath
                }
            )
    )
.Then("Integration-Tests-Tool-ReportOrAnalyze")
    .Does<BuildData>(
        static (context, data) => context.DotNetTool(
                "tool",
                new DotNetToolSettings {
                    ArgumentCustomization = args => args
                                                        .Append("run")
                                                        .Append("dpi")
                                                        .Append("nuget")
                                                        .Append("../../../")
                                                        .AppendSwitchQuoted("--output", "table")
                                                        .Append(
                                                            (
                                                                !string.IsNullOrWhiteSpace(context.EnvironmentVariable("NuGetReportSettings_SharedKey"))
                                                                &&
                                                                !string.IsNullOrWhiteSpace(context.EnvironmentVariable("NuGetReportSettings_WorkspaceId"))
                                                            )
                                                                ? "report"
                                                                : "analyze"
                                                            )
                                                        .AppendSwitchQuoted("--buildversion", data.Version),
                    WorkingDirectory = data.IntegrationTestPath
                }
            )
    )
.Then("Integration-Tests-Tool-Validate-Manifest")
    .Does<BuildData>(
        static (context, data) => {

            IEnumerable<string> json;
            var result = context.StartProcess(
                "dotnet",
                new ProcessSettings {
                    Arguments = new ProcessArgumentBuilder()
                                                        .Append("tool")
                                                        .Append("run")
                                                        .Append("dpi")
                                                        .Append("nuget")
                                                        .AppendSwitchQuoted("--output", "json")
                                                        .Append("analyze")
                                                        .AppendSwitchQuoted("--buildversion", data.Version),
                    WorkingDirectory = data.IntegrationTestPath,
                    RedirectStandardOutput = true
                },
                out json
            );

            var packageReferences = System.Text.Json.JsonSerializer.Deserialize<DPIPackageReference[]>(
                string.Concat(json)
            );

            Array.ForEach(
                packageReferences,
                packageReference => Assert.Equal(
                    packageReference.Version,
                    data.Version
                    )
            );

            context.Information("Validated version {0}", data.Version);
        }
    )
.Then("Integration-Tests-Tool-Validate-Markdown")
    .Does<BuildData>(
        static (context, data) => {

            context.DotNetTool(
                "tool",
                new DotNetToolSettings {
                    ArgumentCustomization = args => args
                                                        .Append("run")
                                                        .Append("dpi")
                                                        .Append("nuget")
                                                        .AppendSwitchQuoted("--output", "markdown")
                                                        .AppendSwitchQuoted("--file", data.MarkdownIndexPath.FullPath)
                                                        .Append("../../../")
                                                        .Append("analyze")
                                                        .AppendSwitchQuoted("--buildversion", data.Version),
                    WorkingDirectory = data.IntegrationTestPath,
                }
            );
        }
    )
.Then("Integration-Tests-Upload-Results")
    .WithCriteria(BuildSystem.IsRunningOnGitHubActions, nameof(BuildSystem.IsRunningOnGitHubActions))
    .Does<BuildData>(
         async (context, data) => {
            await GitHubActions.Commands.UploadArtifact(
                data.MarkdownPath,
                $"Markdown_{GitHubActions.Environment.Runner.ImageOS ?? GitHubActions.Environment.Runner.OS}_{context.Environment.Runtime.BuiltFramework.Identifier}_{context.Environment.Runtime.BuiltFramework.Version}"
            );
            GitHubActions.Commands.SetStepSummary(
                string.Join(
                    System.Environment.NewLine,
                    context.FileSystem.GetFile(data.MarkdownIndexPath)
                        .ReadLines(Encoding.UTF8)
                )
            );
         }
    )
.Then("Integration-Tests")
    .Default()
.Then("Push-GitHub-Packages")
    .WithCriteria<BuildData>( (context, data) => data.ShouldPushGitHubPackages())
    .DoesForEach<BuildData, FilePath>(
        static (data, context)
            => context.GetFiles(data.NuGetOutputPath.FullPath + "/*.nupkg"),
        static (data, item, context)
            => context.DotNetNuGetPush(
                item.FullPath,
            new DotNetNuGetPushSettings
            {
                Source = data.GitHubNuGetSource,
                ApiKey = data.GitHubNuGetApiKey
            }
        )
    )
.Then("Push-NuGet-Packages")
    .WithCriteria<BuildData>( (context, data) => data.ShouldPushNuGetPackages())
    .DoesForEach<BuildData, FilePath>(
        static (data, context)
            => context.GetFiles(data.NuGetOutputPath.FullPath + "/*.nupkg"),
        static (data, item, context)
            => context.DotNetNuGetPush(
                item.FullPath,
                new DotNetNuGetPushSettings
                {
                    Source = data.NuGetSource,
                    ApiKey = data.NuGetApiKey
                }
        )
    )
.Then("Create-GitHub-Release")
    .WithCriteria<BuildData>( (context, data) => data.ShouldPushNuGetPackages())
    .Does<BuildData>(
        static (context, data) => context
            .Command(
                new CommandSettings {
                    ToolName = "GitHub CLI",
                    ToolExecutableNames = new []{ "gh.exe", "gh" },
                    EnvironmentVariables = { { "GH_TOKEN", data.GitHubNuGetApiKey } }
                },
                new ProcessArgumentBuilder()
                    .Append("release")
                    .Append("create")
                    .Append(data.Version)
                    .AppendSwitchQuoted("--title", data.Version)
                    .Append("--generate-notes")
                    .Append(string.Join(
                        ' ',
                        context
                            .GetFiles(data.NuGetOutputPath.FullPath + "/*.nupkg")
                            .Select(path => path.FullPath.Quote())
                        ))

            )
    )
.Then("GitHub-Actions")
.Run();
