using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cake.Core.IO;

namespace DPI.Helper
{
    public abstract class PathJsonConverter<TPath> : JsonConverter<TPath> where TPath : Path
    {
        public override TPath? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();

            return value is null ? null : ConvertFromString(value);
        }

        public override void Write(Utf8JsonWriter writer, TPath value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.FullPath);
        }

        protected abstract TPath ConvertFromString(string value);
    }
}