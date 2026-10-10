using System.Text.Json.Serialization;

namespace DPI.Models.NuGet;

[JsonSerializable(typeof(GlobalJson))]
internal partial class GlobalJsonSerializerContext : JsonSerializerContext;
