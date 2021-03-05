using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Cake.Core.IO;
using DPI.Commands.Models;
using DPI.Commands.Settings.NuGet;
using DPI.Helper;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DPI.Commands.NuGet
{
    public abstract class NuGetCommand<TCommandSettings> : AsyncCommand<TCommandSettings>
        where TCommandSettings : NuGetSettings
    {
        protected static async Task OutputResult<T>(TCommandSettings settings, IEnumerable<T> results)
        {
            await using var fileStream = (settings.OutputPath == null)
                ? null
                : settings.Context.FileSystem.GetFile(settings.OutputPath).OpenWrite();

            switch (settings.Output)
            {
                case OutputFormat.Json:
                {
                    await using var outputStream = fileStream ?? Console.OpenStandardOutput();
                    await JsonSerializer.SerializeAsync(
                        outputStream,
                        results,
                        new JsonSerializerOptions { WriteIndented = true }
                    );
                    break;
                }
                case OutputFormat.Table:
                {
                    
                    var table = results.AsTable();

                    AnsiConsole.Render(table);

                    if (fileStream != null)
                    { 
                        await using var writer = new StreamWriter(fileStream, Encoding.UTF8);
                        var console = AnsiConsole.Create(new AnsiConsoleSettings
                        {
                            Ansi = AnsiSupport.No,
                            ColorSystem = ColorSystemSupport.NoColors,
                            Out = writer,
                            Interactive = InteractionSupport.No
                        });

                        console.Render(table);
                    }
                    break;
                }

                case null:
                {
                    foreach (var result in results)
                    {
                        settings.Logger.LogInformation("{result}", result);
                    }

                    break;
                }

                default:
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(settings.Output),
                        settings.Output,
                        "Unknown output format."
                    );
                }
            }
        }
    }
}