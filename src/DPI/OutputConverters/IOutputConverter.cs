using DPI.Models;

namespace DPI.OutputConverters;

public interface IOutputConverter
{
    OutputFormat  OutputFormat { get; }
    Task OutputToStream<T>(IEnumerable<T> results, Stream? fileStream);
}