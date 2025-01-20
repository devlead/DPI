using DPI.Commands.NuGet;
using DPI.Commands.Settings.NuGet;
using DPI.OutputConverters;
using DPI.Parsers.NuGet;
public partial class Program
{
    static partial void AddServices(IServiceCollection services)
    {
        services.AddHttpClient()
        .AddTransient<NuGetAnalyzeSettings>()
        .AddTransient<NuGetReportSettings>()
        .AddSingleton<CsProjParser>()
        .AddSingleton<DotNetToolsManifestParser>()
        .AddSingleton<PackageConfigParser>()
        .AddSingleton<CakeParser>()
        .AddSingleton<NuGetParsers>()
        .AddSingleton<ProjectsAssetsParser>()
        .AddSingleton<IOutputConverter, JsonOutputConverter>()
        .AddSingleton<IOutputConverter, TableOutputConverter>()
        .AddSingleton<IOutputConverter, MarkdownOutputConverter>()
        .AddSingleton<ILookup<OutputFormat, IOutputConverter>, OutputConverterLookup>()
        .AddCakeCore();
    }

    static partial void ConfigureApp(AppServiceConfig appServiceConfig)
    {
        appServiceConfig.SetApplicationName("dpi");
        appServiceConfig.AddBranch<NuGetSettings>("nuget", nuGet => {
            nuGet.SetDescription("NuGet dependency commands.");

            nuGet.AddCommand<NuGetAnalyzeCommand<NuGetAnalyzeSettings>>("analyze")
                .WithDescription("Inventories NuGet packages")
                .WithExample(["nuget", "<SourcePath>", "analyze"]);

            nuGet.AddCommand<NuGetReportCommand>("report")
                .WithDescription("Inventories NuGet packages and reports to Azure Log Analytics")
                .WithExample(["nuget", "<SourcePath>", "report"]);
        });
    }
}