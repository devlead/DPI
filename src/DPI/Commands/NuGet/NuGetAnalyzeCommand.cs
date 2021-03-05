using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Extensions.Logging;
using Cake.Common.IO;
using Cake.Core.IO;
using DPI.Commands.Models;
using DPI.Commands.Settings.NuGet;
using Spectre.Console.Cli;

namespace DPI.Commands.NuGet
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class NuGetAnalyzeCommand : NuGetCommand<NuGetAnalyzeSettings>
    {
        private const string NuGetPackagesFilePattern = "{.csproj,dotnet-tools.json,packages.config}";

        public override async Task<int> ExecuteAsync(CommandContext context, NuGetAnalyzeSettings settings)
        {
            FilePathCollection  filePaths;
            using(settings.Logger.BeginScope("GetFiles"))
            {
                settings.Logger.LogInformation("Scanning {SourcePath} for {Pattern}...", settings.SourcePath, NuGetPackagesFilePattern);
                filePaths = settings.Context.GetFiles($"{settings.SourcePath}/**/*{NuGetPackagesFilePattern}");
                settings.Logger.LogInformation("Found {Count} files.", filePaths.Count);
            }

            PackageReference[] packages;
            using(settings.Logger.BeginScope("ParseFiles"))
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

        private async IAsyncEnumerable<PackageReference> ParseFiles(FilePathCollection filePaths, NuGetAnalyzeSettings settings)
        {
            foreach (var filePath in filePaths)
            {
                await foreach(var package in (
                        filePath.GetFilename().FullPath,
                        Extension: filePath.GetExtension()
                    ) switch
                {
                    ("dotnet-tools.json", _) => ParseToolManifest(settings, filePath),
                    ("packages.config", _) => ParsePackagesConfig(settings, filePath),
                    (_, ".csproj") => ParseCSProj(settings, filePath),
                    _ => AsyncEnumerable.Empty<PackageReference>()
                })
                {
                    yield return package;
                }
            }
        }

        private async IAsyncEnumerable<PackageReference> ParseCSProj(NuGetAnalyzeSettings settings, FilePath filePath)
        {
            using (settings.Logger.BeginScope(nameof(ParseCSProj)))
            {
                await using var file = settings.Context.FileSystem.GetFile(filePath).OpenRead();
                var xml = await XDocument.LoadAsync(file, LoadOptions.None, CancellationToken.None);
                foreach (var packageReference in xml.XPathSelectElements("/Project/ItemGroup/PackageReference"))
                {
                    yield return GetPackageReference(
                        settings,
                        filePath,
                        NuGetSourceType.CSProj,
                        packageReference.Attribute("Include")?.Value,
                        packageReference.Attribute("Version")?.Value
                    );
                }
            }
        }

        private static PackageReference GetPackageReference(
            NuGetSettings settings,
            FilePath filePath,
            NuGetSourceType nuGetSourceType, string? packageId, string? version)
        {
            return new (
                settings.Context.MakeRelative(filePath, settings.SourcePath),
                nuGetSourceType,
                packageId,
                version
            );
        }

        private async IAsyncEnumerable<PackageReference> ParsePackagesConfig(NuGetAnalyzeSettings settings, FilePath filePath)
        {
            using (settings.Logger.BeginScope(nameof(ParsePackagesConfig)))
            {
                await using var file = settings.Context.FileSystem.GetFile(filePath).OpenRead();
                var xml = await XDocument.LoadAsync(file, LoadOptions.None, CancellationToken.None);
                foreach (var package in xml.XPathSelectElements("/packages/package"))
                {
                    yield return GetPackageReference(
                        settings,
                        filePath,
                        NuGetSourceType.PackagesConfig,
                        package.Attribute("id")?.Value,
                        package.Attribute("version")?.Value
                    );
                }
            }
        }

        private async IAsyncEnumerable<PackageReference> ParseToolManifest(NuGetAnalyzeSettings settings, FilePath filePath)
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
                    yield return GetPackageReference(
                        settings,
                        filePath,
                        NuGetSourceType.DotNetToolsManifest,
                        key,
                        value.Version
                        );
                }
            }
        }
    }
}