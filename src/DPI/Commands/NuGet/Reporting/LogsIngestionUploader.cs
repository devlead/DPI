using Azure.Monitor.Ingestion;
using DPI.Commands.Settings.NuGet;

namespace DPI.Commands.NuGet.Reporting;

public sealed class LogsIngestionUploader(ILogsIngestionClientFactory clientFactory) : ILogsIngestionUploader
{
    public Task UploadAsync<T>(
        NuGetReportSettings settings,
        IReadOnlyList<T> records,
        LogsUploadOptions options,
        CancellationToken cancellationToken = default)
        => clientFactory.Create(settings).UploadAsync(
            settings.RuleId!,
            settings.Stream,
            records,
            options,
            cancellationToken);
}
