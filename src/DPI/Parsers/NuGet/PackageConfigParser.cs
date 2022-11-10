using System.Xml.Linq;
using System.Xml.XPath;
using DPI.Commands.Settings.NuGet;
using DPI.Models.NuGet;

namespace DPI.Parsers.NuGet;

public class PackageConfigParser : INuGetPackageReferenceParser
{
    public NuGetSourceType SourceType { get; } = NuGetSourceType.PackagesConfig;

    public async IAsyncEnumerable<PackageReference> Parse(
        NuGetSettings settings,
        FilePath filePath,
        DirectoryPath? repoRoot,
        PackageReference basePackageReference
    )
    {
        using (settings.Logger.BeginScope(nameof(PackageConfigParser)))
        {
            await using var file = settings.Context.FileSystem.GetFile(filePath).OpenRead();
            var xml = await XDocument.LoadAsync(file, LoadOptions.None, CancellationToken.None);
            foreach (var package in xml.XPathSelectElements("/packages/package"))
            {
                yield return basePackageReference with
                {
                    SourceType = NuGetSourceType.PackagesConfig,
                    TargetFramework = package.Attribute("targetFramework")?.Value,
                    PackageId = package.Attribute("id")?.Value,
                    Version = package.Attribute("version")?.Value
                };
            }
        }
    }
}