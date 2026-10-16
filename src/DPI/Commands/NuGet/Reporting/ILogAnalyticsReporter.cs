using DPI.Commands.Settings.NuGet;

namespace DPI.Commands.NuGet.Reporting;

public interface ILogAnalyticsReporter
{
    Task ReportAsync<T>(
        NuGetReportSettings settings,
        IReadOnlyList<T> results,
        CancellationToken cancellationToken = default);
}
