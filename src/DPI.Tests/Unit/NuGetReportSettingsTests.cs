using Cake.Core;
using DPI.Commands.Settings.NuGet;

namespace DPI.Tests.Unit;

public sealed class NuGetReportSettingsTests
{
    [Fact]
    public async Task Validate_ShouldSelectLogsIngestion_WhenEndpointAndRuleIdAreSet()
    {
        // Given
        var (context, httpClientFactory) = ServiceProviderFixture.GetRequiredService<ICakeContext, IHttpClientFactory>();
        var settings = ReportSettingsFactory.Create(
            context,
            httpClientFactory,
            endpoint: "https://example.ingest.monitor.azure.com",
            ruleId: "dcr-123");

        // When
        var result = settings.Validate();

        // Then
        await Verify(new
        {
            result.Successful,
            result.Message,
            settings.ReportMode,
            settings.Stream
        });
    }

    [Fact]
    public async Task Validate_ShouldSelectDataCollector_WhenWorkspaceAndSharedKeyAreSet()
    {
        // Given
        var (context, httpClientFactory) = ServiceProviderFixture.GetRequiredService<ICakeContext, IHttpClientFactory>();
        var settings = ReportSettingsFactory.Create(
            context,
            httpClientFactory,
            workspaceId: "workspace-id",
            sharedKey: Convert.ToBase64String("shared-key"u8.ToArray()));

        // When
        var result = settings.Validate();

        // Then
        await Verify(new
        {
            result.Successful,
            result.Message,
            settings.ReportMode
        });
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenModesAreMixed()
    {
        // Given
        var (context, httpClientFactory) = ServiceProviderFixture.GetRequiredService<ICakeContext, IHttpClientFactory>();
        var settings = ReportSettingsFactory.Create(
            context,
            httpClientFactory,
            endpoint: "https://example.ingest.monitor.azure.com",
            ruleId: "dcr-123",
            workspaceId: "workspace-id",
            sharedKey: Convert.ToBase64String("shared-key"u8.ToArray()));

        // When
        var result = settings.Validate();

        // Then
        await Verify(new
        {
            result.Successful,
            result.Message
        });
    }

    [Fact]
    public async Task Validate_ShouldUseDefaultStream_WhenStreamIsWhitespace()
    {
        // Given
        var (context, httpClientFactory) = ServiceProviderFixture.GetRequiredService<ICakeContext, IHttpClientFactory>();
        var settings = ReportSettingsFactory.Create(
            context,
            httpClientFactory,
            endpoint: "https://example.ingest.monitor.azure.com",
            ruleId: "dcr-123",
            stream: "  ");

        // When
        var result = settings.Validate();

        // Then
        await Verify(new
        {
            result.Successful,
            result.Message,
            settings.ReportMode,
            settings.Stream
        });
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenLogsIngestionSettingsArePartial()
    {
        // Given
        var (context, httpClientFactory) = ServiceProviderFixture.GetRequiredService<ICakeContext, IHttpClientFactory>();
        var settings = ReportSettingsFactory.Create(
            context,
            httpClientFactory,
            endpoint: "https://example.ingest.monitor.azure.com");

        // When
        var result = settings.Validate();

        // Then
        await Verify(new
        {
            result.Successful,
            result.Message
        });
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenEndpointIsHttp()
    {
        // Given
        var (context, httpClientFactory) = ServiceProviderFixture.GetRequiredService<ICakeContext, IHttpClientFactory>();
        var settings = ReportSettingsFactory.Create(
            context,
            httpClientFactory,
            endpoint: "http://example.ingest.monitor.azure.com",
            ruleId: "dcr-123");

        // When
        var result = settings.Validate();

        // Then
        await Verify(new
        {
            result.Successful,
            result.Message
        });
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenEndpointIsNotAbsolute()
    {
        // Given
        var (context, httpClientFactory) = ServiceProviderFixture.GetRequiredService<ICakeContext, IHttpClientFactory>();
        var settings = ReportSettingsFactory.Create(
            context,
            httpClientFactory,
            endpoint: "example.ingest.monitor.azure.com",
            ruleId: "dcr-123");

        // When
        var result = settings.Validate();

        // Then
        await Verify(new
        {
            result.Successful,
            result.Message
        });
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenDataCollectorSettingsArePartial()
    {
        // Given
        var (context, httpClientFactory) = ServiceProviderFixture.GetRequiredService<ICakeContext, IHttpClientFactory>();
        var settings = ReportSettingsFactory.Create(
            context,
            httpClientFactory,
            workspaceId: "workspace-id");

        // When
        var result = settings.Validate();

        // Then
        await Verify(new
        {
            result.Successful,
            result.Message
        });
    }
}
