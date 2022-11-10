using System.Text;
using Cake.Common.Build;
using DPI.Helper;
using DPI.Models;
using Spectre.Console;

namespace DPI.OutputConverters;

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

        AnsiConsole.Write(table);

        if (fileStream != null)
        {
            await using var writer = new StreamWriter(fileStream, Encoding.UTF8);
            var console = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.No,
                ColorSystem = ColorSystemSupport.NoColors,
                Out = new AnsiConsoleOutput(writer),
                Interactive = InteractionSupport.No,
            });

            console.Profile.Width = 240;

            console.Write(table);
        }
    }

    public TableOutputConverter(ICakeContext context)
    {
        BuildSystem = context.BuildSystem();
    }
}