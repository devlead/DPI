using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Cake.Common.Build;
using Microsoft.Extensions.Logging;
using Cake.Common.IO;
using Cake.Core.IO;
using DPI.Commands.Models;
using DPI.Commands.Settings.NuGet;
using Spectre.Console.Cli;

namespace DPI.Commands.NuGet
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class NuGetAnalyzeCommand<TSettings> : NuGetCommand<TSettings> where TSettings : NuGetAnalyzeSettings
    {
        private const string NuGetPackagesFilePattern = "{.csproj,dotnet-tools.json,packages.config}";

        public override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
        {
            FilePathCollection filePaths;
            using (settings.Logger.BeginScope("GetFiles"))
            {
                settings.Logger.LogInformation("Scanning {SourcePath} for {Pattern}...", settings.SourcePath,
                    NuGetPackagesFilePattern);
                filePaths = settings.Context.GetFiles($"{settings.SourcePath}/**/*{NuGetPackagesFilePattern}");
                settings.Logger.LogInformation("Found {Count} files.", filePaths.Count);
            }

            PackageReference[] packages;
            using (settings.Logger.BeginScope("ParseFiles"))
            {
                settings.Logger.LogInformation("Parsing files...");
                packages = await ParseFiles(filePaths, settings)
                    .ToArrayAsync();
                settings.Logger.LogInformation("Found {LongLength}", packages.LongLength);
            }

            using (settings.Logger.BeginScope("ReportPackages"))
            {
                await OutputResult(settings, packages);
            }

            return 0;
        }

        private async IAsyncEnumerable<PackageReference> ParseFiles(
            FilePathCollection filePaths,
            NuGetAnalyzeSettings settings
            )
        {
            DirectoryPath? TryFindGitRoot()
            {
                var gitDir = settings.Context.FileSystem.GetDirectory(settings.SourcePath.Combine(".git"));
                for (var level = byte.MaxValue; level > 0; level--)
                {
                    if (gitDir.Exists)
                    {
                        return gitDir
                            .Path.Combine("../")
                            .Collapse();
                    }

                    gitDir = settings.Context.FileSystem.GetDirectory(gitDir.Path.Combine("../../.git"));
                }

                return null;
            }

            var gitFolder = TryFindGitRoot();

            var basePackageReference = new PackageReference(
                BuildProvider: settings.BuildSystem.Provider,
                BuildNo: settings.BuildSystem.Provider switch
                {
                    BuildProvider.AppVeyor => settings.BuildSystem.AppVeyor.Environment.Build.Number.ToString(CultureInfo.InvariantCulture),
                    BuildProvider.AzurePipelines => settings.BuildSystem.AzurePipelines.Environment.Build.Number,
                    BuildProvider.AzurePipelinesHosted => settings.BuildSystem.AzurePipelines.Environment.Build.Number,
                    BuildProvider.GitHubActions => FormattableString.Invariant($"{settings.BuildSystem.GitHubActions.Environment.Workflow.RunId}-{settings.BuildSystem.GitHubActions.Environment.Workflow.RunNumber}"),
                    _=> DateTime.UtcNow.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture)
                },
                BuildSCM: settings.BuildSystem.Provider switch
                {
                    BuildProvider.AppVeyor => settings.BuildSystem.AppVeyor.Environment.Repository.Name,
                    BuildProvider.AzurePipelines => settings.BuildSystem.AzurePipelines.Environment.Repository.RepoName,
                    BuildProvider.AzurePipelinesHosted => settings.BuildSystem.AzurePipelines.Environment.Repository.RepoName,
                    BuildProvider.GitHubActions => settings.BuildSystem.GitHubActions.Environment.Workflow.Repository,
                    _ => gitFolder?.GetDirectoryName() ?? string.Empty
                },
                BuildVersion: settings.BuildVersion,
                SessionId: Guid.NewGuid(),
                PlatformFamily: settings.Context.Environment.Platform.Family
            );
            
            foreach (var filePath in filePaths)
            {
                var filePackageReference = basePackageReference with
                {
                    Source = settings.SourcePath.GetRelativePath(filePath)
                };
                await foreach (var package in (
                        filePath.GetFilename().FullPath,
                        Extension: filePath.GetExtension()
                    ) switch
                    {
                        ("dotnet-tools.json", _) => ParseToolManifest(
                            settings,
                            filePath,
                            filePackageReference with { SourceType = NuGetSourceType.DotNetToolsManifest }
                            ),
                        ("packages.config", _) => ParsePackagesConfig(
                            settings,
                            filePath,
                            filePackageReference with { SourceType = NuGetSourceType.PackagesConfig }
                        ),
                        (_, ".csproj") => ParseCSProj(
                            settings,
                            filePath,
                            gitFolder,
                            filePackageReference with { SourceType = NuGetSourceType.CSProj }
                        ),
                        _ => AsyncEnumerable.Empty<PackageReference>()
                    })
                {
                    yield return package;
                }
            }
        }

        private async IAsyncEnumerable<PackageReference> ParseCSProj(
            NuGetSettings settings,
            FilePath filePath,
            DirectoryPath? gitRootPath,
            PackageReference basePackageReference
            )
        {
            static async Task<ILookup<string, string>> TryFindDirectoryProps(
                NuGetSettings settings,
                FilePath csprojPath,
                DirectoryPath? gitRootPath
                )
            {
                static async Task<(string key, string value)[]> ParseMsBuildProperties(NuGetSettings settings, FilePath? msBuildXmlPath)
                {
                    if (msBuildXmlPath == null || !settings.Context.FileExists(msBuildXmlPath))
                    {
                        return Array.Empty<(string key, string value)>();
                    }

                    await using var directoryBuildPropsFile = settings.Context.FileSystem.GetFile(msBuildXmlPath).OpenRead();
                    var directoryBuildPropsXml =
                        await XDocument.LoadAsync(directoryBuildPropsFile, LoadOptions.None, CancellationToken.None);

                    var properties = directoryBuildPropsXml
                        .XPathSelectElements("/Project/PropertyGroup/*")
                        .Select(element => (key: element.Name.LocalName, value: element.Value))
                        .ToArray();
                    return properties;
                }

                var csprojProperties = await ParseMsBuildProperties(settings, csprojPath);
                var currentDirectory = settings.Context.MakeAbsolute(csprojPath.GetDirectory()).Collapse();
                var stopDir = gitRootPath is {}
                    ? settings.Context.MakeAbsolute(gitRootPath.Combine("../")).Collapse()
                    : null;
                FilePath? directoryBuildPropsPath = null;

                for (
                    var level = byte.MaxValue
                    ; level > 0
                      && currentDirectory.FullPath!=string.Empty
                      && currentDirectory.FullPath != stopDir?.FullPath
                    ; level--
                    )
                {
                    directoryBuildPropsPath = currentDirectory.CombineWithFilePath("Directory.Build.props");

                    if (settings.Context.FileExists(directoryBuildPropsPath))
                    {
                        break;
                    }

                    currentDirectory = currentDirectory.Combine("../").Collapse();
                }
               
                var propsProperties = await ParseMsBuildProperties(settings, directoryBuildPropsPath);
                var csprojPropertiesLookup = csprojProperties.ToLookup(
                    key => key.key,
                    value => value.value,
                    StringComparer.OrdinalIgnoreCase
                );

                return csprojProperties.
                    Union(
                        propsProperties
                    .Where(key => !csprojPropertiesLookup[key.key].Any())
                    )
                    .ToLookup(
                    key => string.Concat("$(", key.key,")"),
                    value => value.value,
                    StringComparer.OrdinalIgnoreCase
                );
            }

            using (settings.Logger.BeginScope(nameof(ParseCSProj)))
            {
                var propertiesLookup = await TryFindDirectoryProps(settings, filePath, gitRootPath);
        
                await using var file = settings.Context.FileSystem.GetFile(filePath).OpenRead();
                var xml = await XDocument.LoadAsync(file, LoadOptions.None, CancellationToken.None);
                foreach (var packageReference in xml.XPathSelectElements("/Project/ItemGroup/PackageReference"))
                {
                    var packageId = packageReference.Attribute("Include")?.Value;
                    var version = packageReference.Attribute("Version")?.Value ?? string.Empty;
                    yield return basePackageReference with
                    {
                        PackageId = packageId,
                        Version = propertiesLookup[version].FirstOrDefault() ?? version
                    };
                }
            }
        }

        private async IAsyncEnumerable<PackageReference> ParsePackagesConfig(
            NuGetSettings settings,
            FilePath filePath,
            PackageReference basePackageReference
            )
        {
            using (settings.Logger.BeginScope(nameof(ParsePackagesConfig)))
            {
                await using var file = settings.Context.FileSystem.GetFile(filePath).OpenRead();
                var xml = await XDocument.LoadAsync(file, LoadOptions.None, CancellationToken.None);
                foreach (var package in xml.XPathSelectElements("/packages/package"))
                {
                    yield return basePackageReference with
                    {
                        PackageId = package.Attribute("id")?.Value,
                        Version = package.Attribute("version")?.Value
                    };
                }
            }
        }

        private async IAsyncEnumerable<PackageReference> ParseToolManifest(
            NuGetSettings settings,
            FilePath filePath,
            PackageReference basePackageReference
            )
        {
            using (settings.Logger.BeginScope(nameof(ParseToolManifest)))
            {
                await using var file = settings.Context.FileSystem.GetFile(filePath).OpenRead();
                var dotNetToolsManifest = await JsonSerializer.DeserializeAsync<DotNetToolsManifest>(file);

                if (dotNetToolsManifest?.Tools?.Any() != true)
                {
                    yield break;
                }

                foreach (var (key, value) in dotNetToolsManifest.Tools)
                {
                    yield return basePackageReference with
                    {
                        PackageId = key,
                        Version = value.Version
                    };
                }
            }
        }
    }
}