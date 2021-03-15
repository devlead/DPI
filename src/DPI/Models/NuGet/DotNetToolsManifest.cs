using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DPI.Models.NuGet
{
    public record DotNetToolsManifest(
        [property: JsonPropertyName("version")] int Version,
        [property: JsonPropertyName("isRoot")] bool IsRoot,
        [property: JsonPropertyName("tools")] Dictionary<string, DotNetTool> Tools);

    public record DotNetTool(
        [property: JsonPropertyName("version")] string Version,
        // ReSharper disable once SuggestBaseTypeForParameter
        [property: JsonPropertyName("commands")] string[] Commands
    );
}