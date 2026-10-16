using DPI.Commands.NuGet.Reporting;
using DPI.Commands.Settings.NuGet;
using DPI.OutputConverters;
using DPI.Parsers.NuGet;

namespace DPI.Commands.NuGet
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class NuGetReportCommand : NuGetAnalyzeCommand<NuGetReportSettings>
    {
        private LogAnalyticsReporterFactory ReporterFactory { get; }

        protected override async Task OutputResult<T>(NuGetReportSettings settings, IEnumerable<T> results)
        {
            var enumeratedResult = results as T[] ?? results.ToArray();

            await Task.WhenAll(
                base.OutputResult(settings, enumeratedResult),
                ReportResult(settings, enumeratedResult)
            );
        }

        protected virtual Task ReportResult<T>(NuGetReportSettings settings, IEnumerable<T> results)
            => ReporterFactory.Create(settings).ReportAsync(settings, results as IReadOnlyList<T> ?? results.ToArray());

        public NuGetReportCommand(
            NuGetParsers nuGetParsers,
            ILookup<OutputFormat, IOutputConverter> outputConverterLookup,
            LogAnalyticsReporterFactory reporterFactory
        )
            : base(nuGetParsers, outputConverterLookup)
        {
            ReporterFactory = reporterFactory;
        }
    }
}
