using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Cake.Core.IO;
using DPI.Commands.Settings.NuGet;
using DPI.Models.NuGet;

namespace DPI.Parsers.NuGet
{
    public class DotNetToolsManifestParser : INuGetPackageReferenceParser
    {
        public NuGetSourceType SourceType { get; } = NuGetSourceType.DotNetToolsManifest;

        public async IAsyncEnumerable<PackageReference> Parse(
            NuGetSettings settings,
            FilePath filePath,
            DirectoryPath? repoRootPath,
            PackageReference basePackageReference
            )
        {
            using (settings.Logger.BeginScope(nameof(DotNetToolsManifestParser)))
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
                        SourceType = SourceType,
                        PackageId = key,
                        Version = value.Version
                    };
                }
            }
        }
    }
}