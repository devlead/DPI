using System;
using System.Collections.Generic;
using System.Linq;
using DPI.Models;

namespace DPI.Helper
{
    public class PropertyValueGrouper : IEqualityComparer<GroupProperty[]>,
        IEqualityComparer<GroupProperty>
    {
        public static PropertyValueGrouper Default { get; }= new();

        bool IEqualityComparer<GroupProperty[]>.Equals(GroupProperty[]? x, GroupProperty[]? y)
        {
            if (x is null && y is null)
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (ReferenceEquals(x, y))
            {
                return true;
            }


            return x.SequenceEqual(
                y,
                this
            );
        }

        int IEqualityComparer<GroupProperty[]>.GetHashCode(GroupProperty[] obj)
        {
            return obj.Aggregate(
                0,
                (current, item) => HashCode.Combine(
                    current,
                    StringComparer.OrdinalIgnoreCase.GetHashCode(item.Name),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(item.Value)
                )
            );
        }

        bool IEqualityComparer<GroupProperty>.Equals(GroupProperty? x, GroupProperty? y)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(x?.Name, y?.Name) &&
                   StringComparer.OrdinalIgnoreCase.Equals(x?.Value, y?.Value);
        }

        int IEqualityComparer<GroupProperty>.GetHashCode(GroupProperty obj)
        {
            return HashCode.Combine(
                obj.Name.GetHashCode(),
                obj.Value.GetHashCode()
            );
        }
    }
}