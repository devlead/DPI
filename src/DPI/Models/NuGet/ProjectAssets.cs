using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DPI.Models.NuGet
{
    public record ProjectAssets(
        [property: JsonPropertyName("version")] int Version,
        [property: JsonPropertyName("targets")] Dictionary<string, Dictionary<string, ProjectAsset>> Targets

    );

    public record ProjectAsset(
        [property: JsonPropertyName("type")] string Type
    );
}