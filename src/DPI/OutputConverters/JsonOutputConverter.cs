using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DPI.Models;

namespace DPI.OutputConverters
{
    public class JsonOutputConverter : IOutputConverter
    {
        public OutputFormat OutputFormat { get; } = OutputFormat.Json;

        public async Task OutputToStream<T>(IEnumerable<T> results, Stream? fileStream)
        {
            await using var outputStream = fileStream ?? Console.OpenStandardOutput();
            await JsonSerializer.SerializeAsync(
                outputStream,
                results,
                new JsonSerializerOptions {WriteIndented = true}
            );
        }
    }
}