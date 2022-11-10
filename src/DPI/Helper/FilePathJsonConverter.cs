namespace DPI.Helper
{
    public class FilePathJsonConverter : PathJsonConverter<FilePath>
    {
        protected override FilePath ConvertFromString(string value) => FilePath.FromString(value);
    }
}