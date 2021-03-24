using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cake.Core.IO;
using DPI.Models;
using DPI.Commands.Settings.NuGet;
using DPI.Helper;
using DPI.OutputConverters;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DPI.Commands.NuGet
{
    public abstract class NuGetCommand<TCommandSettings> : AsyncCommand<TCommandSettings>
        where TCommandSettings : NuGetSettings
    {
        private ILookup<OutputFormat, IOutputConverter> OutputConverterLookup { get; }

        protected NuGetCommand(ILookup<OutputFormat, IOutputConverter> outputConverterLookup)
        {
            OutputConverterLookup = outputConverterLookup;
        }
        
        protected virtual async Task OutputResult<T>(TCommandSettings settings, IEnumerable<T> results)
        {
            if (!settings.Output.HasValue)
            {
                foreach (var result in results)
                {
                    settings.Logger.LogInformation("{Result}", result);
                }

                return;
            }

            var converter = OutputConverterLookup[settings.Output.Value].FirstOrDefault()
                            ?? throw new ArgumentOutOfRangeException(
                                nameof(settings.Output),
                                settings.Output,
                                "Unknown output format."
                            );

            await using var fileStream = (settings.OutputPath == null)
                ? null
                : settings.Context.FileSystem.GetFile(settings.OutputPath).OpenWrite();

            await converter.OutputToStream(results, fileStream);
        }
    }
}