using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Cake.Core;
using Spectre.Console.Cli;

namespace DPI.Commands.Settings.NuGet
{
    public class NuGetAnalyzeSettings : NuGetSettings
    {
        [CommandOption("-b|--buildversion <BUILDVERSION>")]
        [Description("Specifies optional version of current build.")]
        public string? BuildVersion { get; init; }

        public NuGetAnalyzeSettings(ICakeContext context, ILogger<NuGetAnalyzeSettings> logger)
            : base(context, logger)
         {
         }
    }
}