using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VerifyTests;
using VerifyXunit;
using Xunit;

[UsesVerify]
public class IntegrationTest
{
    [Theory]
    [MemberData(nameof(GetArgs))]
    public Task Run(string args)
    {
        return RunDotnet(args);
    }

    public static IEnumerable<object[]> GetArgs()
    {
        yield return new object[] {""};
        yield return new object[] {"--version"};
        yield return new object[] {"--help"};
    }

    static Task RunDotnet(string? args, [CallerFilePath] string sourceFile = "")
    {
        var dotnetArgs = $"run --no-build --no-restore -- {args}";
        var solutionDirectory = AttributeReader.GetSolutionDirectory();
        ProcessStartInfo startInfo = new("dotnet", dotnetArgs)
        {
            WorkingDirectory = Path.Combine(solutionDirectory, @"DPI"),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false
        };
        using var process = Process.Start(startInfo)!;
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (error.Length > 0)
        {
            throw new(error);
        }

        return Verifier.Verify(output, sourceFile: sourceFile)
            .UseParameters(args);
    }
}