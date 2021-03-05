using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Spectre.Console;

namespace DPI.Helper
{
    public static class GenericTableHelper
    {
        public static Table AsTable<T>(this IEnumerable<T> result)
        {
            var properties = typeof(T)
                .GetProperties(
                    BindingFlags.GetProperty
                    | BindingFlags.Public
                    | BindingFlags.Instance
                );

            var table = new Table()
                            .AddColumns(properties.Select(property => property.Name).ToArray());

            foreach (var row in result)
            {
                if (row is null)
                {
                    table.AddEmptyRow();
                    continue;
                }

                table.AddRow(
                    properties.Select(
                            property => Convert.ToString(
                                property.GetValue(row),
                                CultureInfo.InvariantCulture) ?? "NULL"
                        )
                        .ToArray()
                );
            }

            return table;
        }
    }
}