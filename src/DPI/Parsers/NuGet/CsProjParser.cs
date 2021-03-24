using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Cake.Common.IO;
using Cake.Core.IO;
using DPI.Commands.Settings.NuGet;
using DPI.Models.NuGet;

namespace DPI.Parsers.NuGet
{
    public class CsProjParser : INuGetPackageReferenceParser
    {
        private static readonly string[] NoFrameWorks = new[] { string.Empty };
        public NuGetSourceType SourceType { get; } = NuGetSourceType.CSProj;
        private ProjectsAssetsParser ProjectsAssetsParser { get; }

        public CsProjParser(ProjectsAssetsParser projectsAssetsParser)
        {
            ProjectsAssetsParser = projectsAssetsParser;
        }

        public async IAsyncEnumerable<PackageReference> Parse(
            NuGetSettings settings,
            FilePath filePath,
            DirectoryPath? repoRootPath,
            PackageReference basePackageReference
        )
        {
            using (settings.Logger.BeginScope(nameof(CsProjParser)))
            {
                var projectAssetsPath = filePath
                    .GetDirectory()
                    .Combine("obj")
                    .CombineWithFilePath("project.assets.json");

                if (settings.Context.FileExists(projectAssetsPath))
                {
                    var packageReferences = ProjectsAssetsParser.Parse(
                        settings,
                        projectAssetsPath,
                        repoRootPath,
                        basePackageReference
                    );

                    var found = false;
                    await foreach (var packageReference in packageReferences)
                    {
                        found = true;
                        yield return packageReference;
                    }

                    if (found)
                    {
                        yield break;
                    }
                }

                var propertiesLookup = await TryFindDirectoryProps(settings, filePath, repoRootPath);
                var targetFrameWorks = (
                    propertiesLookup["$(TargetFramework)"].FirstOrDefault()
                    ??
                    propertiesLookup["$(TargetFrameworks)"].FirstOrDefault()
                    ??
                    string.Empty
                ).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (targetFrameWorks.Length < 1)
                {
                    targetFrameWorks = NoFrameWorks;
                }

                await using var file = settings.Context.FileSystem.GetFile(filePath).OpenRead();
                var xml = await XDocument.LoadAsync(file, LoadOptions.None, CancellationToken.None);
                foreach (var packageReference in xml.XPathSelectElements("/Project/ItemGroup/PackageReference"))
                {
                    var packageId = packageReference.Attribute("Include")?.Value;
                    var version = packageReference.Attribute("Version")?.Value
                                  ?? packageReference.Element("Version")?.Value
                                  ?? string.Empty;

                    foreach (var targetFrameWork in targetFrameWorks)
                    {
                        yield return basePackageReference with
                        {
                            SourceType = SourceType,
                            TargetFramework = targetFrameWork,
                            PackageId = packageId,
                            Version = propertiesLookup[version].FirstOrDefault() ?? version
                        };
                    }
                }
            }
        }

        private static async Task<ILookup<string, string>> TryFindDirectoryProps(
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
            var stopDir = gitRootPath is { }
                ? settings.Context.MakeAbsolute(gitRootPath.Combine("../")).Collapse()
                : null;
            FilePath? directoryBuildPropsPath = null;

            for (
                var level = byte.MaxValue
                ; level > 0
                  && currentDirectory.FullPath != string.Empty
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
                    key => string.Concat("$(", key.key, ")"),
                    value => value.value,
                    StringComparer.OrdinalIgnoreCase
                );
        }
    }
}