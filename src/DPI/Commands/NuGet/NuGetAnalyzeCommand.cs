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
            NuGetSettings settings
            )
        {
            var basePackageReference = new PackageReference(
                BuildProvider: settings.BuildSystem.Provider,
                BuildReference: settings.BuildSystem.Provider switch
                {
                    BuildProvider.AppVeyor => settings.BuildSystem.AppVeyor.Environment.Build.Number.ToString(CultureInfo.InvariantCulture),
                    BuildProvider.AzurePipelines => settings.BuildSystem.AzurePipelines.Environment.Build.Number,
                    BuildProvider.AzurePipelinesHosted => settings.BuildSystem.AzurePipelines.Environment.Build.Number,
                    BuildProvider.GitHubActions => settings.BuildSystem.GitHubActions.Environment.Workflow.RunNumber.ToString(CultureInfo.InvariantCulture),
                    _=> null
                },
                BuildSCM: settings.BuildSystem.Provider switch
                {
                    BuildProvider.AppVeyor => settings.BuildSystem.AppVeyor.Environment.Repository.Name,
                    BuildProvider.AzurePipelines => settings.BuildSystem.AzurePipelines.Environment.Repository.RepoName,
                    BuildProvider.AzurePipelinesHosted => settings.BuildSystem.AzurePipelines.Environment.Repository.RepoName,
                    BuildProvider.GitHubActions => settings.BuildSystem.GitHubActions.Environment.Workflow.Repository,
                    _ => null
                },
                SessionId: Guid.NewGuid()
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
            PackageReference basePackageReference
            )
        {
            using (settings.Logger.BeginScope(nameof(ParseCSProj)))
            {
                await using var file = settings.Context.FileSystem.GetFile(filePath).OpenRead();
                var xml = await XDocument.LoadAsync(file, LoadOptions.None, CancellationToken.None);
                foreach (var packageReference in xml.XPathSelectElements("/Project/ItemGroup/PackageReference"))
                {
                    yield return basePackageReference with
                    {
                        PackageId = packageReference.Attribute("Include")?.Value,
                        Version = packageReference.Attribute("Version")?.Value
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