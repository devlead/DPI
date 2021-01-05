using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DPI;
using Spectre.Console.Cli;

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
    .AddHttpClient();

using var registrar = new DependencyInjectionRegistrar(serviceCollection);
var app = new CommandApp(registrar);
return await app.RunAsync(args);