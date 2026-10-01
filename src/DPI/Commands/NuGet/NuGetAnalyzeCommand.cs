using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Cake.Common.Build;
using Microsoft.Extensions.Logging;
using Cake.Common.IO;
using Cake.Core.IO;
using DPI.Models.NuGet;
using DPI.Commands.Settings.NuGet;
using DPI.Models;
using DPI.OutputConverters;
using DPI.Parsers.NuGet;
using Spectre.Console.Cli;

namespace DPI.Commands.NuGet
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class NuGetAnalyzeCommand<TSettings> : NuGetCommand<TSettings> where TSettings : NuGetAnalyzeSettings
    {
        private const string NuGetPackagesFilePattern = "{.csproj,dotnet-tools.json,packages.config,.cake}";

        private NuGetParsers NuGetParsers { get; }

        public override async Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken)
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

        public NuGetAnalyzeCommand(
            NuGetParsers nuGetParsers,
            ILookup<OutputFormat, IOutputConverter> outputConverterLookup
            ) : base(outputConverterLookup)
        {
            NuGetParsers = nuGetParsers;
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
                    BuildProvider.GitHubActions => FormattableString.Invariant($"{settings.BuildSystem.GitHubActions.Environment.Workflow.RunId}-{settings.BuildSystem.GitHubActions.Environment.Workflow.RunNumber}"),
                    _=> DateTime.UtcNow.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture)
                },
                BuildSCM: settings.BuildSystem.Provider switch
                {
                    BuildProvider.AppVeyor => settings.BuildSystem.AppVeyor.Environment.Repository.Name,
                    BuildProvider.AzurePipelines => settings.BuildSystem.AzurePipelines.Environment.Repository.RepoName,
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
                        ("dotnet-tools.json", _) => NuGetParsers.DotNetToolsManifestParser.Parse(
                            settings,
                            filePath,
                            gitFolder,
                            filePackageReference
                            ),
                        ("packages.config", _) => NuGetParsers.PackageConfigParser.Parse(
                            settings,
                            filePath,
                            gitFolder,
                            filePackageReference
                        ),
                        (_, ".csproj") => NuGetParsers.CsProjParser.Parse(
                            settings,
                            filePath,
                            gitFolder,
                            filePackageReference
                        ),
                        (_, ".cake") => NuGetParsers.CakeParser.Parse(
                            settings,
                            filePath,
                            gitFolder,
                            filePackageReference
                        ),
                        _ => AsyncEnumerable.Empty<PackageReference>()
                    })
                {
                    yield return package;
                }
            }
        }
    }
}