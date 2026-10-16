using Azure.Monitor.Ingestion;
using DPI.Commands.Settings.NuGet;

namespace DPI.Commands.NuGet.Reporting;

public interface ILogsIngestionClientFactory
{
    LogsIngestionClient Create(NuGetReportSettings settings);
}
