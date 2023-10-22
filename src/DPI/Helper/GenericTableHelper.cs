using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DPI.Attributes;
using DPI.Models;
using Spectre.Console;

namespace DPI.Helper
{
    public static class GenericTableHelper
    {
        private const int MaxTableWidth = 120;

        public static Table AsTable<T>(this IEnumerable<T> result)
        {
            // ReSharper disable once ParameterTypeCanBeEnumerable.Local
            static string[] GetPropertyNames(ICollection<PropertyInfo> properties)
                => properties.Select(property => property.Name).ToArray();

            // ReSharper disable once ParameterTypeCanBeEnumerable.Local
            static string[] GetPropertyValues(ICollection<PropertyInfo> properties, T row)
                => properties
                    .Select(property => GetPropertyValue(property.GetValue, row))
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
                .Except(groupProperties.Select(pi=>pi.PropertyInfo))
                .Where(
                    property => property
                                    .GetCustomAttributes(typeof(TableHiddenAttribute))
                                    .OfType<TableHiddenAttribute>()
                                    .Any() == false
                    )
                .ToArray();

            var rowLookup = result.ToLookup(
                row => GetPropertyNameValues(groupProperties, row),
                row => row,
                PropertyValueGrouper.Default
            );

            var masterTable = new Table();

            masterTable
                .AddColumn("Table")
                .HideHeaders()
                .NoBorder();
            

            foreach (var rows in rowLookup)
            {
                var rowsKey = rows
                    .Key
                    .Where(key => !key.IsTitle)
                    .ToArray();

                var groupTitle = string.Join(
                    ", ",
                    rows
                        .Key
                        .Where(key => key.IsTitle)
                        .Select(key => string.Concat(key.Name,": ", key.Value))
                );

                var groupTable = new Table()
                    .Width(MaxTableWidth)
                    .AddColumns(
                        rowsKey
                            .Select(col => col.Name)
                            .ToArray()
                    ).AddRow(
                        rowsKey
                            .Select(col => col.Value)
                            .ToArray()
                    );

                if (!string.IsNullOrWhiteSpace(groupTitle))
                {
                    groupTable.Title(groupTitle);
                }

                var rowTable = new Table()
                    .Width(MaxTableWidth)
                    .AddColumns(GetPropertyNames(properties));


                foreach (var row in rows)
                {
                    if (row is null)
                    {
                        rowTable.AddEmptyRow();
                        continue;
                    }


                    rowTable.AddRow(
                        GetPropertyValues(properties, row)
                    );
                }

                

                masterTable.AddRow(
                    new Table()
                        .AddColumn("")
                        .HideHeaders()
                        .AddRow(groupTable)
                        .AddRow(rowTable)
                );
            }

            return masterTable;
        }
    }
}