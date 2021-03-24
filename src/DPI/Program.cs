using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Cake.Bridge.DependencyInjection;
using DPI.Commands.NuGet;
using DPI.Commands.Settings.NuGet;
using DPI.Models;
using DPI.OutputConverters;
using DPI.Parsers.NuGet;
using Spectre.Console.Cli;
using Spectre.Cli.Extensions.DependencyInjection;

var serviceCollection = new ServiceCollection()
    .AddLogging(configure =>
        configure
            .AddSimpleConsole(opts => {
                opts.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            })
            .AddConfiguration(
                new ConfigurationBuilder()
                    .Add(new MemoryConfigurationSource
                    {
                        InitialData = new Dictionary<string, string>
                        {
                            {"LogLevel:System.Net.Http.HttpClient", "Warning"}
                        }
                    })
                    .Build()
            ))
    .AddHttpClient()
    .AddSingleton<CsProjParser>()
    .AddSingleton<DotNetToolsManifestParser>()
    .AddSingleton<PackageConfigParser>()
    .AddSingleton<CakeParser>()
    .AddSingleton<NuGetParsers>()
    .AddSingleton<ProjectsAssetsParser>()
    .AddSingleton<IOutputConverter, JsonOutputConverter>()
    .AddSingleton<IOutputConverter, TableOutputConverter>()
    .AddSingleton<ILookup<OutputFormat, IOutputConverter>, OutputConverterLookup>()
    .AddCakeCore();

using var registrar = new DependencyInjectionRegistrar(serviceCollection);
var app = new CommandApp(registrar);

app.Configure(
    config =>
    {
        config.SetApplicationName("dpi");
        config.ValidateExamples();
        config.AddBranch<NuGetSettings>("nuget", nuGet => {
            nuGet.SetDescription("NuGet dependency commands.");

            nuGet.AddCommand<NuGetAnalyzeCommand<NuGetAnalyzeSettings>>("analyze")
                .WithDescription("Inventories NuGet packages")
                .WithExample(new[] { "nuget", "<SourcePath>", "analyze" });

            nuGet.AddCommand<NuGetReportCommand>("report")
                .WithDescription("Inventories NuGet packages and reports to Azure Log Analytics")
                .WithExample(new[] { "nuget", "<SourcePath>", "report" });
        });
    });

return await app.RunAsync(args);