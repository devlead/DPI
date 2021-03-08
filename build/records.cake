#load "helpers.cake"
using System.Text.Json.Serialization;

/*****************************
 * Records
 *****************************/
public record BuildData(
    string Version,
    DirectoryPath ProjectRoot,
    DotNetCoreMSBuildSettings MSBuildSettings,
    DirectoryPath ArtifactsPath,
    DirectoryPath OutputPath
    )
{
    private const string IntegrationTest = "integrationtest";
    public DirectoryPath NuGetOutputPath { get; } = OutputPath.Combine("nuget");
    public DirectoryPath IntegrationTestPath { get; } = OutputPath.Combine(IntegrationTest);
    public ICollection<DirectoryPath> DirectoryPathsToClean = new []{
        ArtifactsPath,
        OutputPath,
        OutputPath.Combine(IntegrationTest)
    };


}

private record ExtensionHelper(Func<string, CakeTaskBuilder> TaskCreate, Func<CakeReport> Run);


public record DPIPackageReference(
    [property: JsonPropertyName("source")] [property: JsonConverter(typeof(FilePathJsonConverter))]
    FilePath Source,

    [property: JsonPropertyName("sourceType")]
    string SourceType,

    [property: JsonPropertyName("packageId")]
    string PackageId,

    [property: JsonPropertyName("version")]
    string Version
);