using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DPI.Commands.Settings.NuGet;
using Microsoft.Extensions.Logging;
using static System.FormattableString;

namespace DPI.Commands.NuGet
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class NuGetReportCommand : NuGetAnalyzeCommand<NuGetReportSettings>
    {
        protected override async Task OutputResult<T>(NuGetReportSettings settings, IEnumerable<T> results)
        {
            var enumeratedResult = results as T[] ?? results.ToArray();

            await Task.WhenAll(
                base.OutputResult(settings, enumeratedResult),
                ReportResult(settings, enumeratedResult)
            );
        }

        protected virtual async Task ReportResult<T>(NuGetReportSettings settings, IEnumerable<T> results)
        {
            var dateString = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture);
            var uri = Invariant(
                $"https://{Uri.EscapeDataString(settings.WorkspaceId)}.ods.opinsights.azure.com/api/logs?api-version={Uri.EscapeDataString(settings.ApiVersion)}"
            );

            using var client = settings.HttpClientFactory.CreateClient(nameof(NuGetReportCommand));
            client.DefaultRequestHeaders.Add("Log-Type", settings.LogType);
            client.DefaultRequestHeaders.Add("x-ms-date", dateString);
            
            foreach (var logValue in results)
            {
                await PostLog(settings, logValue, dateString, uri, client);
            }

            settings.Logger.LogInformation("Reported to Azure Log Analytics Workspace");
        }

        private static async Task PostLog<T>(NuGetReportSettings settings, T logValue, string dateString, string uri,
            HttpClient client)
        {
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(logValue);
            var signature = GetSignature(
                "POST",
                jsonBytes.Length,
                dateString,
                "/api/logs",
                settings
            );

            using var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new ByteArrayContent(jsonBytes)
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue(settings.ContentType),
                        ContentLength = jsonBytes.Length
                    }
                },
                Headers = {{"Authorization", signature}}
            };

            var response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();
        }

        private static string GetSignature(
            string method,
            int contentLength,
            string date,
            string resource,
            NuGetReportSettings settings
            )
        {
            var message = Invariant($"{method}\n{contentLength}\n{settings.ContentType}\nx-ms-date:{date}\n{resource}");
            var bytes = Encoding.UTF8.GetBytes(message);
            using var hmacSha256 = new HMACSHA256(Convert.FromBase64String(settings.SharedKey));
            return Invariant($"SharedKey {settings.WorkspaceId}:{Convert.ToBase64String(hmacSha256.ComputeHash(bytes))}");
        }
    }
}