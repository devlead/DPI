using System;
using System.ComponentModel;
using Cake.Core.IO;

namespace DPI.Helper
{
    public class FilePathConverter : TypeConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            => value switch
            {
                string stringValue => FilePath.FromString(stringValue),
                FilePath filePath => filePath,
                _ => FilePath.FromString(Convert.ToString(value, culture))
            };
    }
}