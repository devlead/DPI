using System;
using System.ComponentModel;
using System.Net.Http;
using Cake.Core;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DPI.Commands.Settings.NuGet
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class NuGetReportSettings : NuGetAnalyzeSettings
    {
        private const string Prefix = nameof(NuGetReportSettings);
        private const string WorkspaceIdEnvKey = Prefix + "_" + nameof(WorkspaceId);
        private const string WorkspaceIdArgument = "--workspace <WORKSPACEID>";
        private const string SharedKeyEnvKey = Prefix + "_" + nameof(SharedKey);
        private const string SharedKeyArgument = "--sharedkey <SHAREDKEY>";

        public IHttpClientFactory HttpClientFactory { get; }
        public string ApiVersion { get; } = "2016-04-01";
        public string LogType { get; } = "NuGetReport";
        public string ContentType { get; } = "application/json";

#pragma warning disable 8601
        [CommandOption(WorkspaceIdArgument)]
        [Description("Specifies Azure Log Analytics workspace id to log to, defaults to environment variable " + WorkspaceIdEnvKey + ".")]
        public string WorkspaceId { get; init; } = Environment.GetEnvironmentVariable(WorkspaceIdEnvKey);

        [CommandOption(SharedKeyArgument)]
        [Description("Specifies Azure Log Analytics shared key, defaults to environment variable " + SharedKeyEnvKey + ".")]

        public string SharedKey { get; init; } = Environment.GetEnvironmentVariable(SharedKeyEnvKey);
#pragma warning restore 8601

        


        public override ValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(WorkspaceId))
            {
                return ValidationResult.Error(
                    $"{nameof(WorkspaceId)} not specified as argument {WorkspaceIdArgument}, nor environment variable {WorkspaceIdEnvKey}"
                );
            }

            if (string.IsNullOrWhiteSpace(SharedKey))
            {
                return ValidationResult.Error(
                    $"{nameof(SharedKey)} not specified as argument {SharedKeyArgument}, nor environment variable {SharedKeyEnvKey}"
                );
            }

            return base.Validate();
        }

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