using DPI.Commands.Settings.NuGet;

namespace DPI.Commands.NuGet.Reporting;

public sealed class LogAnalyticsReporterFactory(
    LogsIngestionReporter logsIngestionReporter,
    DataCollectorReporter dataCollectorReporter)
{
    public ILogAnalyticsReporter Create(NuGetReportSettings settings)
        => settings.ReportMode switch
        {
            LogAnalyticsReportMode.LogsIngestion => logsIngestionReporter,
            LogAnalyticsReportMode.DataCollector => dataCollectorReporter,
            _ => throw new InvalidOperationException($"Unsupported report mode '{settings.ReportMode}'.")
        };
}
