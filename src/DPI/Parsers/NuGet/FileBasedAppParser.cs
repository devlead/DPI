using System.Text.RegularExpressions;
using Cake.Common.IO;
using DPI.Commands.Settings.NuGet;
using DPI.Models.NuGet;

namespace DPI.Parsers.NuGet;

public partial class FileBasedAppParser : INuGetPackageReferenceParser
{
    public NuGetSourceType SourceType { get; } = NuGetSourceType.FileBasedApp;

    [GeneratedRegex(@"^#!/usr/bin/env dotnet(?:\s|$)", RegexOptions.CultureInvariant)]
    private static partial Regex Shebang();

    [GeneratedRegex(@"^#:(sdk|package)\s+([^@]+?)(?:@(.+))?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PackageOrSdkDirective();

    public async IAsyncEnumerable<PackageReference> Parse(
        NuGetSettings settings,
        FilePath filePath,
        DirectoryPath? repoRootPath,
        PackageReference basePackageReference
    )
    {
        using (settings.Logger.BeginScope(nameof(FileBasedAppParser)))
        {
            await using var file = settings.Context.FileSystem.GetFile(filePath).OpenRead();
            using var reader = new StreamReader(file, Encoding.UTF8);

            var firstLine = await reader.ReadLineAsync();
            if (firstLine is null || !Shebang().IsMatch(firstLine.TrimStart()))
            {
                yield break;
            }

            ILookup<string, string>? msBuildSdks = null;
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var trimmed = line.Trim();

                if (trimmed.Length == 0 || trimmed.StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!trimmed.StartsWith("#:", StringComparison.Ordinal))
                {
                    yield break;
                }

                var match = PackageOrSdkDirective().Match(trimmed);
                if (!match.Success)
                {
                    continue;
                }

                var kind = match.Groups[1].Value;
                var packageId = match.Groups[2].Value.Trim();
                var version = match.Groups[3].Success
                    ? match.Groups[3].Value.Trim()
                    : null;

                if (string.IsNullOrWhiteSpace(packageId))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(version)
                    && kind.Equals("sdk", StringComparison.OrdinalIgnoreCase))
                {
                    msBuildSdks ??= await TryFindMsBuildSdks(settings, filePath, repoRootPath);
                    version = msBuildSdks[packageId].FirstOrDefault();
                }

                yield return basePackageReference with
                {
                    SourceType = SourceType,
                    PackageId = packageId,
                    Version = string.IsNullOrWhiteSpace(version) ? null : version
                };
            }
        }
    }

    private static async Task<ILookup<string, string>> TryFindMsBuildSdks(
        NuGetSettings settings,
        FilePath filePath,
        DirectoryPath? gitRootPath
    )
    {
        var currentDirectory = settings.Context.MakeAbsolute(filePath.GetDirectory()).Collapse();
        var stopDir = gitRootPath is { }
            ? settings.Context.MakeAbsolute(gitRootPath.Combine("../")).Collapse()
            : null;

        for (
            var level = byte.MaxValue
            ; level > 0
              && currentDirectory.FullPath != string.Empty
              && currentDirectory.FullPath != stopDir?.FullPath
            ; level--
        )
        {
            var globalJsonPath = currentDirectory.CombineWithFilePath("global.json");

            if (settings.Context.FileExists(globalJsonPath))
            {
                await using var globalJsonFile = settings.Context.FileSystem.GetFile(globalJsonPath).OpenRead();
                var globalJson = await JsonSerializer.DeserializeAsync(
                    globalJsonFile,
                    GlobalJsonSerializerContext.Default.GlobalJson);

                return (globalJson?.MsBuildSdks ?? new Dictionary<string, string>())
                    .ToLookup(
                        pair => pair.Key,
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );
            }

            currentDirectory = currentDirectory.Combine("../").Collapse();
        }

        return Array.Empty<(string key, string value)>().ToLookup(pair => pair.key, pair => pair.value);
    }
}
