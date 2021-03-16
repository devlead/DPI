using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Text;
using DPI.Commands.Settings.NuGet;
using DPI.Models.NßuGet;
using DPI.Models.NuGet;

namespace DPI.Parsers.NuGet
{
    public class CakeParser : INuGetPackageReferenceParser
    {
        public NuGetSourceType SourceType { get; } = NuGetSourceType.Cake;

        public async IAsyncEnumerable<PackageReference> Parse(
            NuGetSettings settings,
            FilePath filePath,
            DirectoryPath? repoRootPath,
            PackageReference basePackageReference
            )
        {
            using (settings.Logger.BeginScope(nameof(CakeParser)))
            {
                await using var file = settings.Context.FileSystem.GetFile(filePath).OpenRead();
                using var reader = new StreamReader(file, Encoding.UTF8);
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    
                    if (
                        QuoteAwareStringSplitter.Split(
                            Environment.ExpandEnvironmentVariables(
                                line.Trim()
                            )
                        ).ToArray()
                        is not { Length: >= 2 } parts)
                    {
                        continue;
                    }

                    var preProcessorDirective = parts[0]
                            .ToLowerInvariant()
                        switch
                        {
                            "#addin" => CakePreProcessorDirective.Addin,
                            "#module" => CakePreProcessorDirective.Module,
                            "#load" => CakePreProcessorDirective.Load,
                            "#tool" => CakePreProcessorDirective.Tool,
                            _ => CakePreProcessorDirective.Unknown
                        };

                    if (preProcessorDirective == CakePreProcessorDirective.Unknown)
                    {
                        continue;
                    }

                    if (!Uri.TryCreate(parts[1].UnQuote(), UriKind.Absolute, out var uri))
                    {
                        continue;
                    }

                    var parameters = uri
                        .GetQueryString()
                        .SelectMany(key => key.Value.Select(value => (key.Key, value)))
                        .ToLookup(
                            key => key.Key,
                            value => value.value,
                            StringComparer.OrdinalIgnoreCase
                        );

                    var packageId = parameters["package"].FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(packageId))
                    {
                        continue;
                    }
                    
                    yield return basePackageReference with
                    {
                        SourceType = NuGetSourceType.Cake,
                        PackageId = packageId,
                        Version = parameters["version"].FirstOrDefault()
                    };
                }
            }
        }
    }
}