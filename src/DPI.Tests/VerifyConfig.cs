using System.Runtime.CompilerServices;

namespace DPI.Tests;

public static class VerifyConfig
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifierSettings.InitializePlugins();
        VerifierSettings.DontIgnoreEmptyCollections();
        VerifierSettings.IgnoreStackTrace();
        Verifier.DerivePathInfo((_, projectDirectory, type, method) =>
            new PathInfo(
                directory: System.IO.Path.Combine(projectDirectory, "Snapshots", TargetFrameworkMoniker, type.Name, method.Name),
                typeName: "_",
                methodName: "_"));
        VerifierSettings.AddExtraSettings(settings =>
        {
            settings.DefaultValueHandling = Argon.DefaultValueHandling.Include;
        });
        VerifierSettings.ScrubMember("timestamp");
        VerifierSettings.ScrubMember("Computer");
    }

#if NET11_0
    private const string TargetFrameworkMoniker = "net11.0";
#elif NET10_0
    private const string TargetFrameworkMoniker = "net10.0";
#elif NET9_0
    private const string TargetFrameworkMoniker = "net9.0";
#else
    private const string TargetFrameworkMoniker = "net8.0";
#endif
}
