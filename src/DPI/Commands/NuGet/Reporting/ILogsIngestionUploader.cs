using Azure.Monitor.Ingestion;
using DPI.Commands.Settings.NuGet;

namespace DPI.Commands.NuGet.Reporting;

public interface ILogsIngestionUploader
{
    Task UploadAsync<T>(
        NuGetReportSettings settings,
        IReadOnlyList<T> records,
        LogsUploadOptions options,
        CancellationToken cancellationToken = default);
}
