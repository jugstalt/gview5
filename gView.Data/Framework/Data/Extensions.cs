using gView.Framework.Carto;
using gView.Framework.Data.Filters;
using System;

namespace gView.Framework.Data
{
    public static class Extensions
    {
        static public bool RenderInScale(this ILayer layer, IDisplay display)
        {
            if (layer.MinimumScale > 1 && layer.MinimumScale > display.MapScale)
            {
                return false;
            }

            if (layer.MaximumScale > 1 && layer.MaximumScale < display.MapScale)
            {
                return false;
            }

            return true;
        }

        static public bool LabelInScale(this ILayer layer, IDisplay display)
        {
            if (layer.MinimumLabelScale <= 1 && layer.MaximumLabelScale <= 1)
            {
                return layer.RenderInScale(display);
            }

            if (layer.MinimumLabelScale > 1 && layer.MinimumLabelScale > display.MapScale)
            {
                return false;
            }

            if (layer.MaximumLabelScale > 1 && layer.MaximumLabelScale < display.MapScale)
            {
                return false;
            }

            return true;
        }
    }

    public static class ExtensionsSql
    {
        static public string AppendWhereClause(this string filter, string appendFilter)
        {
            if (String.IsNullOrWhiteSpace(appendFilter))
            {
                return filter;
            }
            if (String.IsNullOrWhiteSpace(filter))
            {
                return appendFilter;
            }

            if (filter.ToLower().Contains(" or "))
            {
                filter = $"({filter})";
            }

            if (appendFilter.ToLower().Contains(" or "))
            {
                appendFilter = $"({appendFilter})";
            }

            return $"{filter} and {appendFilter}";
        }

        static public IQueryFilter AppendWhereClause(this IQueryFilter queryFilter, string appendFilter)
        {
            if (String.IsNullOrWhiteSpace(appendFilter))
            {
                return queryFilter;
            }

            if (queryFilter == null)
            {
                return new QueryFilter() { WhereClause = appendFilter };
            }

            var result = queryFilter.Clone() as IQueryFilter;
            if (result != null)
            {
                result.WhereClause = queryFilter.WhereClause.AppendWhereClause(appendFilter);
            }

            return result;
        }
    }
}
