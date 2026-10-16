using System.Text.Json;
using Cake.Common.Build;
using Cake.Core;
using Cake.Core.IO;
using DPI.Models.NuGet;

namespace DPI.Tests.Unit;

public sealed class PackageReferenceSerializationTests
{
    [Fact]
    public async Task SerializesExpectedLogAnalyticsFieldNames()
    {
        // Given
        var package = new PackageReference(
            SessionId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            BuildProvider: BuildProvider.GitHubActions,
            PlatformFamily: PlatformFamily.Windows,
            BuildNo: "42",
            BuildSCM: "abcdef",
            BuildVersion: "2026.10.16.1",
            SourceType: NuGetSourceType.CSProj,
            Source: FilePath.FromString("src/DPI/DPI.csproj"),
            TargetFramework: "net8.0",
            PackageId: "Azure.Identity",
            Version: "1.21.0"
        );

        // When
        var json = JsonSerializer.Serialize(package);

        // Then
        await VerifyJson(json);
    }
}
