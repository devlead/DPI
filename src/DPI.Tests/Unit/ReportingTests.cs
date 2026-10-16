using Azure;
using Azure.Monitor.Ingestion;
using Cake.Common.Build;
using Cake.Core;
using Cake.Core.IO;
using DPI.Commands.NuGet.Reporting;
using DPI.Commands.Settings.NuGet;
using DPI.Models.NuGet;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DPI.Tests.Unit;

public sealed class ReportingTests
{
    [Fact]
    public async Task Factory_ShouldReturnLogsIngestionReporter_WhenModeIsLogsIngestion()
    {
        // Given
        var (factory, logsIngestionReporter, context, httpClientFactory) = ServiceProviderFixture
            .GetRequiredService<LogAnalyticsReporterFactory, LogsIngestionReporter, ICakeContext, IHttpClientFactory>();
        var settings = ReportSettingsFactory.Create(
            context,
            httpClientFactory,
            endpoint: "https://example.ingest.monitor.azure.com",
            ruleId: "dcr-123");
        settings.Validate();

        // When
        var reporter = factory.Create(settings);

        // Then
        await Verify(new
        {
            Reporter = reporter.GetType().Name,
            SameAsLogsIngestion = ReferenceEquals(reporter, logsIngestionReporter)
        });
    }

    [Fact]
    public async Task Factory_ShouldReturnDataCollectorReporter_WhenModeIsDataCollector()
    {
        // Given
        var (factory, dataCollectorReporter, context, httpClientFactory) = ServiceProviderFixture
            .GetRequiredService<LogAnalyticsReporterFactory, DataCollectorReporter, ICakeContext, IHttpClientFactory>();
        var settings = ReportSettingsFactory.Create(
            context,
            httpClientFactory,
            workspaceId: "workspace-id",
            sharedKey: Convert.ToBase64String("shared-key"u8.ToArray()));
        settings.Validate();

        // When
        var reporter = factory.Create(settings);

        // Then
        await Verify(new
        {
            Reporter = reporter.GetType().Name,
            SameAsDataCollector = ReferenceEquals(reporter, dataCollectorReporter)
        });
    }

    [Fact]
    public async Task LogsIngestionReporter_ShouldUploadRecordsToConfiguredRule()
    {
        // Given
        var (reporter, uploader, context, httpClientFactory) = ServiceProviderFixture
            .GetRequiredService<LogsIngestionReporter, ILogsIngestionUploader, ICakeContext, IHttpClientFactory>();
        var settings = ReportSettingsFactory.Create(
            context,
            httpClientFactory,
            endpoint: "https://example.ingest.monitor.azure.com",
            ruleId: "dcr-123");
        settings.Validate();
        var records = CreateRecords();
        IReadOnlyList<PackageReference>? uploaded = null;
        uploader.UploadAsync(
            settings,
            Arg.Do<IReadOnlyList<PackageReference>>(value => uploaded = value),
            Arg.Any<LogsUploadOptions>(),
            Arg.Any<CancellationToken>()
        ).Returns(Task.CompletedTask);

        // When
        await reporter.ReportAsync(settings, records, TestContext.Current.CancellationToken);

        // Then
        await Verify(new
        {
            settings.RuleId,
            settings.Stream,
            UploadedCount = uploaded?.Count
        });
    }

    [Fact]
    public async Task LogsIngestionReporter_ShouldSkipUpload_WhenThereAreNoRecords()
    {
        // Given
        var (reporter, uploader, context, httpClientFactory) = ServiceProviderFixture
            .GetRequiredService<LogsIngestionReporter, ILogsIngestionUploader, ICakeContext, IHttpClientFactory>();
        var settings = ReportSettingsFactory.Create(
            context,
            httpClientFactory,
            endpoint: "https://example.ingest.monitor.azure.com",
            ruleId: "dcr-123");
        settings.Validate();
        var uploaded = false;
        uploader.UploadAsync(
            Arg.Any<NuGetReportSettings>(),
            Arg.Any<IReadOnlyList<PackageReference>>(),
            Arg.Any<LogsUploadOptions>(),
            Arg.Any<CancellationToken>()
        ).Returns(_ =>
        {
            uploaded = true;
            return Task.CompletedTask;
        });

        // When
        await reporter.ReportAsync(settings, Array.Empty<PackageReference>(), TestContext.Current.CancellationToken);

        // Then
        await Verify(new
        {
            uploaded,
            settings.Stream
        });
    }

    [Fact]
    public async Task LogsIngestionReporter_ShouldWrapFailedUploads()
    {
        // Given
        var (reporter, uploader, context, httpClientFactory) = ServiceProviderFixture
            .GetRequiredService<LogsIngestionReporter, ILogsIngestionUploader, ICakeContext, IHttpClientFactory>();
        var settings = ReportSettingsFactory.Create(
            context,
            httpClientFactory,
            endpoint: "https://example.ingest.monitor.azure.com",
            ruleId: "dcr-123");
        settings.Validate();
        uploader.UploadAsync(
            Arg.Any<NuGetReportSettings>(),
            Arg.Any<IReadOnlyList<PackageReference>>(),
            Arg.Any<LogsUploadOptions>(),
            Arg.Any<CancellationToken>()
        ).ThrowsAsync(new RequestFailedException(403, "Forbidden"));

        // When
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reporter.ReportAsync(settings, CreateRecords(), TestContext.Current.CancellationToken)
        );

        // Then
        await Verify(new
        {
            exception.Message,
            Inner = exception.InnerException?.GetType().Name
        });
    }

    [Fact]
    public async Task DataCollectorReporter_ShouldPostEachRecordAndWarn()
    {
        // Given
        var (reporter, context, httpClientFactory) = ServiceProviderFixture
            .GetRequiredService<DataCollectorReporter, ICakeContext, IHttpClientFactory>();
        var settings = ReportSettingsFactory.Create(
            context,
            httpClientFactory,
            workspaceId: "workspace-id",
            sharedKey: Convert.ToBase64String("shared-key"u8.ToArray()));
        settings.Validate();

        // When
        await reporter.ReportAsync(settings, CreateRecords(), TestContext.Current.CancellationToken);

        // Then
        await Verify(new
        {
            settings.WorkspaceId,
            settings.LogType
        });
    }

    [Fact]
    public async Task DataCollectorReporter_GetSignature_ShouldUseSharedKeyHeader()
    {
        // Given
        var (context, httpClientFactory) = ServiceProviderFixture.GetRequiredService<ICakeContext, IHttpClientFactory>();
        var settings = ReportSettingsFactory.Create(
            context,
            httpClientFactory,
            workspaceId: "workspace-id",
            sharedKey: Convert.ToBase64String("shared-key"u8.ToArray()));

        // When
        var signature = DataCollectorReporter.GetSignature(
            "POST",
            12,
            "Thu, 16 Oct 2026 12:00:00 GMT",
            "/api/logs",
            settings);

        // Then
        await Verify(new
        {
            Prefix = signature[..(signature.IndexOf(':', StringComparison.Ordinal) + 1)]
        });
    }

    private static PackageReference[] CreateRecords()
        =>
        [
            new(
                Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                BuildProvider.Local,
                PlatformFamily.Windows,
                "1",
                "scm",
                "1.0.0",
                NuGetSourceType.CSProj,
                FilePath.FromString("src/A.csproj"),
                "net8.0",
                "Package.A",
                "1.0.0"),
            new(
                Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
                BuildProvider.Local,
                PlatformFamily.Windows,
                "1",
                "scm",
                "1.0.0",
                NuGetSourceType.CSProj,
                FilePath.FromString("src/B.csproj"),
                "net8.0",
                "Package.B",
                "2.0.0")
        ];
}
