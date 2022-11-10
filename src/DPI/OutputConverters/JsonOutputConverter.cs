using System.Text.Json;
using DPI.Models;

namespace DPI.OutputConverters;

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