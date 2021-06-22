#load "helpers.cake"
using System.Text.Json.Serialization;

/*****************************
 * Records
 *****************************/
public record BuildData(
    string Version,
    bool IsMainBranch,
    bool ShouldNotPublish,
    DirectoryPath ProjectRoot,
    DotNetCoreMSBuildSettings MSBuildSettings,
    DirectoryPath ArtifactsPath,
    DirectoryPath OutputPath
    )
{
    private const string IntegrationTest = "integrationtest";
    public DirectoryPath NuGetOutputPath { get; } = OutputPath.Combine("nuget");
    public DirectoryPath BinaryOutputPath { get; } = OutputPath.Combine("bin");
    public DirectoryPath IntegrationTestPath { get; } = OutputPath.Combine(IntegrationTest);

    public string GitHubNuGetSource { get; } = System.Environment.GetEnvironmentVariable("GH_PACKAGES_NUGET_SOURCE");
    public string GitHubNuGetApiKey { get; } = System.Environment.GetEnvironmentVariable("GH_PACKAGES_NUGET_APIKEY");

    public bool ShouldPushGitHubPackages() =>   !ShouldNotPublish
                                                && !string.IsNullOrWhiteSpace(GitHubNuGetSource)
                                                && !string.IsNullOrWhiteSpace(GitHubNuGetApiKey);

    public string NuGetSource { get; } = System.Environment.GetEnvironmentVariable("NUGET_SOURCE");
    public string NuGetApiKey { get; } = System.Environment.GetEnvironmentVariable("NUGET_APIKEY");
    public bool ShouldPushNuGetPackages() =>    IsMainBranch &&
                                                !ShouldNotPublish &&
                                                !string.IsNullOrWhiteSpace(NuGetSource) &&
                                                !string.IsNullOrWhiteSpace(NuGetApiKey);

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