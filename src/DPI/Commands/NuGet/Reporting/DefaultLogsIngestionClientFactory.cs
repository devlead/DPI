using Azure.Identity;
using Azure.Monitor.Ingestion;
using DPI.Commands.Settings.NuGet;

namespace DPI.Commands.NuGet.Reporting;

public sealed class DefaultLogsIngestionClientFactory : ILogsIngestionClientFactory
{
    public LogsIngestionClient Create(NuGetReportSettings settings)
        => new(new Uri(settings.Endpoint!), new DefaultAzureCredential());
}
