using System.Collections.Generic;
using System.Text.Json;
using Cake.Core.IO;
using DPI.Commands.Settings.NuGet;
using DPI.Models.NuGet;

namespace DPI.Parsers.NuGet
{
    public class ProjectsAssetsParser : INuGetPackageReferenceParser
    {
        public NuGetSourceType SourceType { get; } = NuGetSourceType.ProjectAssets;
        public async IAsyncEnumerable<PackageReference> Parse(
            NuGetSettings settings,
            FilePath filePath,
            DirectoryPath? repoRootPath,
            PackageReference basePackageReference
        )
        {
            using (settings.Logger.BeginScope(nameof(ProjectsAssetsParser)))
            {
                await using var file = settings.Context.FileSystem.GetFile(filePath).OpenRead();
                var projectAssets = await JsonSerializer.DeserializeAsync<ProjectAssets>(file);

                if (projectAssets == null)
                {
                    yield break;
                }

                foreach (var (targetFramework, targets) in projectAssets.Targets)
                {
                    foreach (var (package, _) in targets)
                    {
                        yield return basePackageReference with
                        {
                            SourceType = SourceType,
                            TargetFramework = targetFramework switch
                            {
                                ".NETCoreApp,Version=v2.0" => "netcoreapp2.0",
                                ".NETCoreApp,Version=v2.1" => "netcoreapp2.1",
                                ".NETCoreApp,Version=v2.2" => "netcoreapp2.2",
                                ".NETCoreApp,Version=v3.0" => "netcoreapp3.0",
                                ".NETCoreApp,Version=v3.1" => "netcoreapp3.1",
                                _ => targetFramework
                            },
                            PackageId = System.IO.Path.GetDirectoryName(package),
                            Version = System.IO.Path.GetFileName(package)
                        };
                    }
                }
            }
        }
    }
}