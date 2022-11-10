namespace DPI.Helper
{
    public class DirectoryPathJsonConverter : PathJsonConverter<DirectoryPath>
    {
        protected override DirectoryPath ConvertFromString(string value) => DirectoryPath.FromString(value);
    }
}