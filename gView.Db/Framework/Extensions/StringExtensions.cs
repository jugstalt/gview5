using System;
using System.Linq;

namespace gView.Db.Framework.Extensions
{
    internal static class StringExtensions
    {
        public static string ToDbTableName(this string tableName, char prefix, char postfix, char schemaSeparator) 
        { 
            if(string.IsNullOrEmpty(tableName))
            {
                return tableName;
            }

            return String.Join(schemaSeparator.ToString(),
                               tableName.Split(schemaSeparator)
                                        .Select(s => !s.StartsWith(prefix.ToString()) ?
                                                     $"{prefix}{s}{postfix}" : s));
        }

        public static string ToMsSqlTableName(this string tableName)
            => tableName.ToDbTableName('[', ']', '.');

        public static string ToPostgreSqlTableName(this string tableName)
            => tableName.ToDbTableName('\"', '\"', '.');
    }
}
