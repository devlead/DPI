using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Cake.Common.Build;
using Cake.Core;
using DPI.Helper;
using DPI.Models;
using Spectre.Console;

namespace DPI.OutputConverters
{
    public class TableOutputConverter : IOutputConverter
    {
        public OutputFormat OutputFormat { get; } = OutputFormat.Table;
        private BuildSystem BuildSystem { get; }
        public async Task OutputToStream<T>(IEnumerable<T> results, Stream? fileStream)
        {
            var table = results.AsTable();

            if (!BuildSystem.IsLocalBuild)
            {
                AnsiConsole.Profile.Width = 240;
            }

            AnsiConsole.Render(table);

            if (fileStream != null)
            {
                await using var writer = new StreamWriter(fileStream, Encoding.UTF8);
                var console = AnsiConsole.Create(new AnsiConsoleSettings
                {
                    Ansi = AnsiSupport.No,
                    ColorSystem = ColorSystemSupport.NoColors,
                    Out = writer,
                    Interactive = InteractionSupport.No,
                });

                console.Profile.Width = 240;

                console.Render(table);
            }
        }

        public TableOutputConverter(ICakeContext context)
        {
            BuildSystem = context.BuildSystem();
        }
    }
}