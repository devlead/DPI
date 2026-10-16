using Cake.Core;
using DPI.Commands.Settings.NuGet;
using Microsoft.Extensions.Logging.Abstractions;

namespace DPI.Tests;

internal static class ReportSettingsFactory
{
    public static NuGetReportSettings Create(
        ICakeContext context,
        IHttpClientFactory httpClientFactory,
        string? endpoint = null,
        string? ruleId = null,
        string? stream = null,
        string? workspaceId = null,
        string? sharedKey = null)
        => new(
            context,
            NullLogger<NuGetReportSettings>.Instance,
            httpClientFactory)
        {
            Endpoint = endpoint,
            RuleId = ruleId,
            Stream = stream ?? NuGetReportSettings.DefaultStream,
            WorkspaceId = workspaceId,
            SharedKey = sharedKey
        };
}
