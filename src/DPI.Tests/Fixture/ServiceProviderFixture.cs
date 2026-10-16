using Cake.Bridge.DependencyInjection.Testing;
using Devlead.Testing.MockHttp;
using DPI.Commands.NuGet.Reporting;
using DPI.Commands.Settings.NuGet;
using NSubstitute;

public static partial class ServiceProviderFixture
{
    static partial void InitServiceProvider(IServiceCollection services)
    {
        services
            .AddCakeCoreFakes()
            .AddMockHttpClient<Constants>()
            .AddLogging()
            .AddTransient<NuGetAnalyzeSettings>()
            .AddTransient<NuGetReportSettings>()
            .AddSingleton<ILogsIngestionUploader>(_ => Substitute.For<ILogsIngestionUploader>())
            .AddSingleton<LogsIngestionReporter>()
            .AddSingleton<DataCollectorReporter>()
            .AddSingleton<LogAnalyticsReporterFactory>();
    }
}
