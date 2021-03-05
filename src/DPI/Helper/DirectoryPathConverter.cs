using System;
using System.ComponentModel;
using Cake.Core.IO;

namespace DPI.Helper
{
    public class DirectoryPathConverter : TypeConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            => value switch {
                    string stringValue => DirectoryPath.FromString(stringValue),
                    DirectoryPath directoryPath => directoryPath,
                    _ => DirectoryPath.FromString(Convert.ToString(value, culture))
                };
    }
}