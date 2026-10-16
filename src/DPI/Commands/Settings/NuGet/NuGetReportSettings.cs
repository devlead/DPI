using System.ComponentModel;
using Cake.Core;
using DPI.Commands.NuGet.Reporting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DPI.Commands.Settings.NuGet
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class NuGetReportSettings : NuGetAnalyzeSettings
    {
        private const string Prefix = nameof(NuGetReportSettings);
        internal const string WorkspaceIdEnvKey = Prefix + "_" + nameof(WorkspaceId);
        private const string WorkspaceIdArgument = "--workspace <WORKSPACEID>";
        internal const string SharedKeyEnvKey = Prefix + "_" + nameof(SharedKey);
        private const string SharedKeyArgument = "--sharedkey <SHAREDKEY>";
        internal const string EndpointEnvKey = Prefix + "_" + nameof(Endpoint);
        private const string EndpointArgument = "--endpoint <ENDPOINT>";
        internal const string RuleIdEnvKey = Prefix + "_" + nameof(RuleId);
        private const string RuleIdArgument = "--rule-id <RULEID>";
        internal const string StreamEnvKey = Prefix + "_" + nameof(Stream);
        private const string StreamArgument = "--stream <STREAM>";
        public const string DefaultStream = "Custom-NuGetReport";

        public IHttpClientFactory HttpClientFactory { get; }
        public string ApiVersion { get; } = "2016-04-01";
        public string LogType { get; } = "NuGetReport";
        public string ContentType { get; } = "application/json";

#pragma warning disable 8601
        [CommandOption(WorkspaceIdArgument)]
        [Description("Specifies Azure Log Analytics workspace id to log to, defaults to environment variable " + WorkspaceIdEnvKey + ". Deprecated; use --endpoint and --rule-id.")]
        public string? WorkspaceId { get; init; } = Environment.GetEnvironmentVariable(WorkspaceIdEnvKey);

        [CommandOption(SharedKeyArgument)]
        [Description("Specifies Azure Log Analytics shared key, defaults to environment variable " + SharedKeyEnvKey + ". Deprecated; use Azure identity instead.")]
        public string? SharedKey { get; init; } = Environment.GetEnvironmentVariable(SharedKeyEnvKey);

        [CommandOption(EndpointArgument)]
        [Description("Specifies the Logs Ingestion API endpoint, defaults to environment variable " + EndpointEnvKey + ".")]
        public string? Endpoint { get; init; } = Environment.GetEnvironmentVariable(EndpointEnvKey);

        [CommandOption(RuleIdArgument)]
        [Description("Specifies the data collection rule immutable ID, defaults to environment variable " + RuleIdEnvKey + ".")]
        public string? RuleId { get; init; } = Environment.GetEnvironmentVariable(RuleIdEnvKey);

        [CommandOption(StreamArgument)]
        [Description("Specifies the data collection rule input stream name, defaults to environment variable " + StreamEnvKey + " or " + DefaultStream + ".")]
        public string Stream
        {
            get => HasValue(_stream) ? _stream! : DefaultStream;
            init => _stream = value;
        }

        private readonly string? _stream = Environment.GetEnvironmentVariable(StreamEnvKey);
#pragma warning restore 8601

        public LogAnalyticsReportMode ReportMode { get; private set; } = LogAnalyticsReportMode.LogsIngestion;

        public override ValidationResult Validate()
        {
            var hasLogsIngestionSettings = HasValue(Endpoint) || HasValue(RuleId);
            var hasDataCollectorSettings = HasValue(WorkspaceId) || HasValue(SharedKey);

            if (hasLogsIngestionSettings && hasDataCollectorSettings)
            {
                return ValidationResult.Error(
                    "Specify either Logs Ingestion API settings (--endpoint and --rule-id) or deprecated Data Collector API settings (--workspace and --sharedkey), not both."
                );
            }

            if (hasLogsIngestionSettings)
            {
                if (!HasValue(Endpoint))
                {
                    return ValidationResult.Error(
                        $"{nameof(Endpoint)} not specified as argument {EndpointArgument}, nor environment variable {EndpointEnvKey}"
                    );
                }

                if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpointUri) ||
                    endpointUri.Scheme != Uri.UriSchemeHttps)
                {
                    return ValidationResult.Error(
                        $"{nameof(Endpoint)} must be an absolute HTTPS URI."
                    );
                }

                if (!HasValue(RuleId))
                {
                    return ValidationResult.Error(
                        $"{nameof(RuleId)} not specified as argument {RuleIdArgument}, nor environment variable {RuleIdEnvKey}"
                    );
                }

                ReportMode = LogAnalyticsReportMode.LogsIngestion;
                return base.Validate();
            }

            if (!HasValue(WorkspaceId))
            {
                return ValidationResult.Error(
                    $"{nameof(WorkspaceId)} not specified as argument {WorkspaceIdArgument}, nor environment variable {WorkspaceIdEnvKey}. Prefer Logs Ingestion API settings {EndpointArgument} and {RuleIdArgument}."
                );
            }

            if (!HasValue(SharedKey))
            {
                return ValidationResult.Error(
                    $"{nameof(SharedKey)} not specified as argument {SharedKeyArgument}, nor environment variable {SharedKeyEnvKey}. Prefer Logs Ingestion API settings {EndpointArgument} and {RuleIdArgument}."
                );
            }

            ReportMode = LogAnalyticsReportMode.DataCollector;
            return base.Validate();
        }

        private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

        public NuGetReportSettings(
            ICakeContext context,
            ILogger<NuGetReportSettings> logger,
            IHttpClientFactory httpClientFactory
            ) : base(context, logger)
        {
            HttpClientFactory = httpClientFactory;
        }
    }
}
