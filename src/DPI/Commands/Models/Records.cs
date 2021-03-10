using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Cake.Common.Build;
using Cake.Core;
using Cake.Core.IO;
using DPI.Helper;

namespace DPI.Commands.Models
{
    public record PackageReference(
        [property: JsonPropertyName("sessionId")]
        Guid SessionId,

        [property: JsonPropertyName("buildProvider")] [property: JsonConverter(typeof(JsonStringEnumConverter))]
        BuildProvider BuildProvider,

        [property: JsonPropertyName("platformFamily")] [property: JsonConverter(typeof(JsonStringEnumConverter))]
        PlatformFamily PlatformFamily,

        [property: JsonPropertyName("buildNo")]
        string? BuildNo,

        [property: JsonPropertyName("buildSCM")]
        // ReSharper disable once InconsistentNaming
        string? BuildSCM,

        [property: JsonPropertyName("buildVersion")]
        string? BuildVersion,

        [property: JsonPropertyName("source")] [property: JsonConverter(typeof(FilePathJsonConverter))]
        FilePath? Source = null,

        [property: JsonPropertyName("sourceType")] [property: JsonConverter(typeof(JsonStringEnumConverter))]
        NuGetSourceType SourceType = NuGetSourceType.Unknown,

        [property: JsonPropertyName("packageId")]
        string? PackageId = null,

        [property: JsonPropertyName("version")]
        string? Version = null
    )
    {
        [JsonPropertyName("timestamp")]
        [Browsable(false)]
        public DateTimeOffset TimeStamp { get; } = DateTimeOffset.UtcNow;

        [JsonPropertyName("Computer")]
        [Browsable(false)]
        public string Computer { get; } = Environment.MachineName;
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
