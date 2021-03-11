﻿using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Cake.Common.Build;
using Cake.Core;
using Cake.Core.IO;
using DPI.Commands.Attributes;
using DPI.Helper;

namespace DPI.Commands.Models
{
    public record PackageReference(
        [property: TableGroup]
        [property: JsonPropertyName("sessionId")]
        Guid SessionId,

        [property: TableGroup]
        [property: JsonPropertyName("buildProvider")] [property: JsonConverter(typeof(JsonStringEnumConverter))]
        BuildProvider BuildProvider,

        [property: TableGroup]
        [property: JsonPropertyName("platformFamily")] [property: JsonConverter(typeof(JsonStringEnumConverter))]
        PlatformFamily PlatformFamily,

        [property: TableGroup]
        [property: JsonPropertyName("buildNo")]
        string? BuildNo,

        [property: TableGroup]
        [property: JsonPropertyName("buildSCM")]
        // ReSharper disable once InconsistentNaming
        string? BuildSCM,

        [property: TableGroup]
        [property: JsonPropertyName("buildVersion")]
        string? BuildVersion,

        [property: TableGroupTitle]
        [property: JsonPropertyName("sourceType")] [property: JsonConverter(typeof(JsonStringEnumConverter))]
        NuGetSourceType SourceType = NuGetSourceType.Unknown,

        [property: TableGroupTitle]
        [property: JsonPropertyName("source")] [property: JsonConverter(typeof(FilePathJsonConverter))]
        FilePath? Source = null,

        [property: JsonPropertyName("targetFramework")]
        string? TargetFramework = null,

        [property: JsonPropertyName("packageId")]
        string? PackageId = null,

        [property: JsonPropertyName("version")]
        string? Version = null
    )
    {
        [TableHidden]
        [JsonPropertyName("timestamp")]
        public DateTimeOffset TimeStamp { get; } = DateTimeOffset.UtcNow;

        [TableHidden]
        [JsonPropertyName("Computer")]
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

    public record ProjectAssets(
        [property: JsonPropertyName("version")] int Version,
        [property: JsonPropertyName("targets")] Dictionary<string, Dictionary<string, ProjectAsset>> Targets

    );

    public record ProjectAsset(
        [property: JsonPropertyName("type")] string Type
    );
}
