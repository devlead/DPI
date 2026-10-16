using Azure;
using Azure.Monitor.Ingestion;
using DPI.Commands.Settings.NuGet;
using Microsoft.Extensions.Logging;

namespace DPI.Commands.NuGet.Reporting;

public sealed class LogsIngestionReporter(ILogsIngestionUploader uploader) : ILogAnalyticsReporter
{
    public async Task ReportAsync<T>(
        NuGetReportSettings settings,
        IReadOnlyList<T> results,
        CancellationToken cancellationToken = default)
    {
        var options = new LogsUploadOptions
        {
            MaxConcurrency = 4
        };

        if (results.Count == 0)
        {
            settings.Logger.LogInformation(
                "Reported {Count} records to Azure Monitor Logs Ingestion API",
                results.Count
            );
            return;
        }

        try
        {
            await uploader.UploadAsync(
                settings,
                results,
                options,
                cancellationToken
            ).ConfigureAwait(false);
        }
        catch (RequestFailedException exception)
        {
            throw new InvalidOperationException(
                $"Failed to upload {results.Count} records to Azure Monitor Logs Ingestion API (status {exception.Status}).",
                exception
            );
        }

        settings.Logger.LogInformation(
            "Reported {Count} records to Azure Monitor Logs Ingestion API",
            results.Count
        );
    }
}
