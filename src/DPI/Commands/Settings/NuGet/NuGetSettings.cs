using System.ComponentModel;
using Cake.Common.Build;
using Cake.Common.IO;
using Microsoft.Extensions.Logging;
using Cake.Core;
using Cake.Core.IO;
using DPI.Models;
using DPI.Helper;
using DPI.Commands.Settings.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console.Cli;

namespace DPI.Commands.Settings.NuGet
{
    public abstract class NuGetSettings : CommandSettings
    {
        private static readonly ILogger SilentLogger = new NullLogger<NuGetSettings>();
        private readonly ILogger logger;
        private DirectoryPath sourcePath;

        public ICakeContext Context { get; }
        public BuildSystem BuildSystem { get; }

        public ILogger Logger => (
            Silent
            ||
            Output switch
            {
                OutputFormat.Table => false,
                {} => OutputPath == null,
                _ => false
            }
        )
            ? SilentLogger
            : logger;

        [CommandArgument(0, "[SourcePath]")]
        [Description("Specifies source path to scan for NuGet dependencies, if not specified working direcory used.")]
        [TypeConverter(typeof(DirectoryPathConverter))]
        [ValidatePath]
        public DirectoryPath SourcePath
        {
            get => sourcePath.IsRelative
                    ? sourcePath = Context.MakeAbsolute(sourcePath)
                    : sourcePath;
            init => sourcePath = value;
        }

        [CommandOption("-s|--silent")]
        [Description("Supresses console logging")]
        public bool Silent { get; init; }

        [CommandOption("-o|--output <FORMAT>")]
        [Description("Specifies optional result output format JSON,TABLE.")]
        public OutputFormat? Output { get; init; }

        [CommandOption("-f|--file <FILEPATH>")]
        [Description("Specifies optional result output file path.")]
        [TypeConverter(typeof(FilePathConverter))]
        public FilePath? OutputPath { get; init; }

        // ReSharper disable once PublicConstructorInAbstractClass
        public NuGetSettings(ICakeContext context, ILogger logger)
        {
            Context = context;
            BuildSystem = context.BuildSystem();
            this.logger = logger;
            sourcePath = context.Environment.WorkingDirectory;
        }
    }
}