using Cake.Core.IO;
using Spectre.Console.Cli;
using Spectre.Console;

namespace DPI.Commands.Settings.Validation
{
    public class ValidatePathAttribute : ParameterValidationAttribute
    {
        public ValidatePathAttribute() : base(null!)
        {
        }

        public override ValidationResult Validate(CommandParameterContext context)
        {
             return context.Value switch {
                FilePath filePath when System.IO.File.Exists(filePath.FullPath)
                    => ValidationResult.Success(),

                DirectoryPath  directoryPath when System.IO.Directory.Exists(directoryPath.FullPath)
                    => ValidationResult.Success(),

                _ => ValidationResult.Error($"Invalid {context.Parameter?.PropertyName} ({context.Value}) specified.")
            };
        }
    }
}