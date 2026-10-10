namespace DPI.Parsers.NuGet;

public class NuGetParsers
{
    public INuGetPackageReferenceParser CsProjParser { get; }
    public INuGetPackageReferenceParser DotNetToolsManifestParser { get; }
    public INuGetPackageReferenceParser PackageConfigParser { get; }
    public INuGetPackageReferenceParser CakeParser { get; }
    public INuGetPackageReferenceParser FileBasedAppParser { get; }
    
    public NuGetParsers(
        CsProjParser csProjParser,
        DotNetToolsManifestParser dotNetToolsManifestParser,
        PackageConfigParser packageConfigParser,
        CakeParser cakeParser,
        FileBasedAppParser fileBasedAppParser
    )
    {
        CsProjParser = csProjParser;
        DotNetToolsManifestParser = dotNetToolsManifestParser;
        PackageConfigParser = packageConfigParser;
        CakeParser = cakeParser;
        FileBasedAppParser = fileBasedAppParser;
    }
}