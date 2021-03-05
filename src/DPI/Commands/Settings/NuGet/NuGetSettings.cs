using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Cake.Core;
using Cake.Core.IO;
using DPI.Helper;
using DPI.Commands.Settings.Validation;
using Spectre.Console.Cli;

namespace DPI.Commands.Settings.NuGet
{
    public abstract class NuGetSettings : CommandSettings
    {
        public ICakeContext Context { get; }
        public ILogger Logger { get; }

        [CommandArgument(0, "[SourcePath]")]
        [Description("Specifies source path to scan for NuGet dependencies, if not specified working direcory used.")]
        [TypeConverter(typeof(DirectoryPathConverter))]
        [ValidatePath()]
        public DirectoryPath SourcePath { get; set; }

        public NuGetSettings(ICakeContext context, ILogger logger)
        {
            Context = context;
            Logger = logger;
            SourcePath = context.Environment.WorkingDirectory;
        }
    }
}