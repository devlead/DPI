using Microsoft.Extensions.Logging;
using Cake.Core;

namespace DPI.Commands.Settings.NuGet
{
    public class NuGetAnalyzeSettings : NuGetSettings
    {
         public NuGetAnalyzeSettings(ICakeContext context, ILogger<NuGetAnalyzeSettings> logger)
            : base(context, logger)
         {

         }

    }
}