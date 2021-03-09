using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Cake.Core.IO;
using DPI.Helper;

namespace DPI.Commands.Models
{
    public record PackageReference(
        [property: JsonPropertyName("source")] [property: JsonConverter(typeof(FilePathJsonConverter))]
        FilePath Source,

        [property: JsonPropertyName("sourceType")] [property: JsonConverter(typeof(JsonStringEnumConverter))]
        NuGetSourceType SourceType,

        [property: JsonPropertyName("packageId")]
        string? PackageId,

        [property: JsonPropertyName("version")]
        string? Version
    )
    {
        private static readonly Guid CurrentSessionId = Guid.NewGuid();

        [property: JsonPropertyName("sessionId")]
        public Guid SessionId { get; init; } = CurrentSessionId;
    }


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
