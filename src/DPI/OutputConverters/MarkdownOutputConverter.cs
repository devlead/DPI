using DPI.Attributes;
using DPI.Helper;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace DPI.OutputConverters;
public class MarkdownOutputConverter : IOutputConverter
{
    public OutputFormat OutputFormat => OutputFormat.Markdown;

    public async Task OutputToStream<T>(IEnumerable<T> results, Stream? fileStream)
    {

        await using var outputStream = fileStream ?? Console.OpenStandardOutput();

        await using var streamWriter = new StreamWriter(outputStream);

        streamWriter.WriteLine(
            FormattableString.Invariant(
                    $$"""
                    ---
                    modifiedby: DPI
                    modified: {{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm}}
                    ---

                    # Dependency Inventory
                    """
                )
            );


        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        static string[] GetPropertyNames(ICollection<PropertyInfo> properties)
            => properties.Select(property => property.Name).ToArray();

        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        static (string Name, string Value)[] GetPropertyValues(ICollection<PropertyInfo> properties, T row)
            => properties
                .Select(property => (property.Name, GetPropertyValue(property.GetValue, row)))
                .ToArray();

        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        // ReSharper disable once ReturnTypeCanBeEnumerable.Local
        static GroupProperty[] GetPropertyNameValues(
            ICollection<(PropertyInfo PropertyInfo, bool IsTitle, bool IsSource)> properties, T row)
            => properties
                .Select(property => new GroupProperty(
                        property.PropertyInfo.Name,
                        GetPropertyValue(property.PropertyInfo.GetValue, row),
                        property.IsTitle,
                        property.IsSource
                    )
                )
                .ToArray();

        static string GetPropertyValue(Func<object?, object?> getValue, T row)
            => row is { }
                ? Convert.ToString(getValue(row), CultureInfo.InvariantCulture) ?? "NULL"
                : "NULL";

        var propertyInfos = typeof(T)
            .GetProperties(
                BindingFlags.GetProperty
                | BindingFlags.Public
                | BindingFlags.Instance
            ).ToArray();

        (PropertyInfo PropertyInfo, bool IsTitle, bool IsSource)[] groupProperties = (
            from propertyInfo in propertyInfos
            let tableGroupAttributes = propertyInfo
                .GetCustomAttributes(typeof(TableGroupAttribute))
                .OfType<TableGroupAttribute>()
                .ToArray()

            where tableGroupAttributes.Any()

            select (
                propertyInfo,
                tableGroupAttributes.OfType<TableGroupTitleAttribute>().Any(),
                tableGroupAttributes.OfType<TableSourceAttribute>().Any()
            )
        ).ToArray();

        var properties = propertyInfos
            .Except(groupProperties.Select(pi => pi.PropertyInfo))
            .Where(
                property => property
                                .GetCustomAttributes(typeof(TableHiddenAttribute))
                                .OfType<TableHiddenAttribute>()
                                .Any() == false
                )
            .ToArray();

        var rowLookup = results.ToLookup(
            row => GetPropertyNameValues(groupProperties, row),
            row => row,
            PropertyValueGrouper.Default
        );

        foreach (var rows in rowLookup)
        {
            var rowsKey = rows
                .Key
                .Where(key => !key.IsSource)
                .ToArray();

            var groupTitle = string.Join(
                ", ",
                rows
                    .Key
                    .Where(key => key.IsSource)
                    .Select(key => key.Value)
            );

            streamWriter.WriteLine();
            streamWriter.WriteLine($"## {groupTitle}");
            streamWriter.WriteLine();

            var groupTableColumnWidth= rowsKey
                    .Max(
                        value => (
                                    value.Value.Length >= value.Name.Length
                                        ? value.Value.Length
                                        : value.Name.Length
                                ) 
                    );
            var groupTableHeaderWidth = groupTableColumnWidth + 2;

            streamWriter.WriteLine($"|{string.Empty.PadRight(groupTableHeaderWidth)}|{string.Empty.PadRight(groupTableHeaderWidth)}|");
            
            streamWriter.WriteLine($"|{string.Empty.PadRight(groupTableHeaderWidth, '-')}|{string.Empty.PadRight(groupTableHeaderWidth, '-')}|");


            foreach ( var rowKey in rowsKey)
            {
                streamWriter.WriteLine($"| {rowKey.Name.PadRight(groupTableColumnWidth)} | {rowKey.Value.PadRight(groupTableColumnWidth)} |");
            }

            streamWriter.WriteLine();

            var rowColumns = GetPropertyNames(properties);
            var rowData = rows
                            .Where(row => row is not null)
                            .Select(row => GetPropertyValues(properties, row))
                            .ToArray();


            var rowWidthLookup = rowData
                                .Aggregate(
                                    new ConcurrentDictionary<string, int>(
                                        rowColumns
                                            .Select(col => new KeyValuePair<string, int>(col, col.Length))
                                    ),
                                    (dictionary, row) =>
                                    {
                                        Array.ForEach(
                                            row,
                                            col => dictionary.AddOrUpdate(
                                                    col.Name,
                                                    key => col.Value.Length,
                                                    (key, value) => (value < col.Value.Length
                                                                                            ? col.Value.Length
                                                                                            : value) 
                                                    )
                                            );
                                        return dictionary;
                                    }
                                );

            streamWriter.WriteLine($"|{string.Join('|', rowColumns.Select(col => $" {col.PadRight(rowWidthLookup[col])} "))}|");
            streamWriter.WriteLine($"|{string.Join('|', rowColumns.Select(col => $"{string.Empty.PadRight(rowWidthLookup[col]+2, '-')}"))}|");
            foreach(var row in rowData)
            {
                streamWriter.WriteLine($"|{string.Join('|', row.Select(col => $" {col.Value.PadRight(rowWidthLookup[col.Name])} "))}|");
            }
        }
    }

}
