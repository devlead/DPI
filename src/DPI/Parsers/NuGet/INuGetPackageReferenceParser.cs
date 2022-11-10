using DPI.Commands.Settings.NuGet;
using DPI.Models.NuGet;

namespace DPI.Parsers.NuGet;

public interface INuGetPackageReferenceParser
{
    NuGetSourceType SourceType { get; }

    IAsyncEnumerable<PackageReference> Parse(
        NuGetSettings settings,
        FilePath filePath,
        DirectoryPath? repoRootPath,
        PackageReference basePackageReference
    );
}