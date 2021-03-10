using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Cake.Common.Build;
using Cake.Core.IO;
using DPI.Helper;

namespace DPI.Commands.Models
{
    public record PackageReference(
        [property: JsonPropertyName("sessionId")]
        Guid SessionId,

        [property: JsonPropertyName("buildProvider")] [property: JsonConverter(typeof(JsonStringEnumConverter))]
        BuildProvider BuildProvider,

        [property: JsonPropertyName("buildReference")]
        string? BuildReference,

        [property: JsonPropertyName("buildSCM")]
        // ReSharper disable once InconsistentNaming
        string? BuildSCM,

        [property: JsonPropertyName("source")] [property: JsonConverter(typeof(FilePathJsonConverter))]
        FilePath? Source = null,

        [property: JsonPropertyName("sourceType")] [property: JsonConverter(typeof(JsonStringEnumConverter))]
        NuGetSourceType SourceType = NuGetSourceType.Unknown,

        [property: JsonPropertyName("packageId")]
        string? PackageId = null,

        [property: JsonPropertyName("version")]
        string? Version = null
    );


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
