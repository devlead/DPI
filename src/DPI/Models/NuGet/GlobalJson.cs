namespace DPI.Models.NuGet;

public record GlobalJson(
    [property: JsonPropertyName("msbuild-sdks")] Dictionary<string, string>? MsBuildSdks
);
