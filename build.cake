#load "build/records.cake"
#load "build/helpers.cake"

/*****************************
 * Setup
 *****************************/
Setup(
    static context => {
        var gh = context.GitHubActions();
        var version =
            gh.IsRunningOnGitHubActions
                ? FormattableString
                    .Invariant($"{DateTime.UtcNow:yyyy.MM.dd}.{gh.Environment.Workflow.RunNumber}")
                : "1.0.0.0";

        context.Information("Building version {0}", version);

        var artifactsPath = context
                            .MakeAbsolute(context.Directory("./artifacts"));

        return new BuildData(
            version,
            "src",
            "win-x64",
            new DotNetCoreMSBuildSettings()
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
        static (context, data) => context.DotNetCoreRestore(
            data.ProjectRoot.FullPath,
            new DotNetCoreRestoreSettings {
                Runtime = data.Runtime,
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("Build")
    .Default()
    .Does<BuildData>(
        static (context, data) => context.DotNetCoreBuild(
            data.ProjectRoot.FullPath,
            new DotNetCoreBuildSettings {
                NoRestore = true,
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("Pack")
    .Does<BuildData>(
        static (context, data) => context.DotNetCorePack(
            data.ProjectRoot.FullPath,
            new DotNetCorePackSettings {
                NoBuild = true,
                NoRestore = true,
                OutputDirectory = data.NuGetOutputPath,
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("Publish")
    .Does<BuildData>(
        static (context, data) => context.DotNetCorePublish(
            data.ProjectRoot.FullPath,
            new DotNetCorePublishSettings {
                NoBuild = true,
                NoRestore = true,
                PublishReadyToRun = true,
                SelfContained = true,
                PublishSingleFile = true,
                PublishTrimmed = true,
                OutputDirectory = data.BinaryOutputPath,
                Runtime = data.Runtime,
                ArgumentCustomization = arg => arg
                                                .Append("-p:TrimMode=Link")
                                                .Append("-p:IncludeNativeLibrariesInSingleFile=true")
                                                .Append("-p:IncludeNativeLibrariesForSelfExtract=true"),
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Run();