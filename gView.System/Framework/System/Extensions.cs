using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.IO;
using gView.Framework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace gView.Framework.system
{
    static public class Extensions
    {
        //static public T NonblockingResult<T>(this Task task)
        //{
        //    var nonBlockingTask = Task.Run(() => task);
        //    nonBlockingTask.Wait();

        //    var property=task.GetType().GetProperty("Result");
        //    if (property != null)
        //        return (T)property.GetValue(task);

        //    return default(T);
        //}

        static public string ExtractConnectionStringParameter(this string connectionString, string parameterName)
        {
            parameterName = parameterName.ToLower();

            foreach (var p in connectionString.Split(';'))
            {
                if (p.ToLower().StartsWith(parameterName + "="))
                {
                    return p.Substring(parameterName.Length + 1);
                }
            }

            return String.Empty;
        }

        static public void AddConnectionStringParameter(this StringBuilder sb, string parameter, string value)
        {
            if (String.IsNullOrEmpty(parameter) || String.IsNullOrEmpty(value))
            {
                return;
            }

            if (sb.Length > 0)
            {
                sb.Append(";");
            }

            sb.Append($"{parameter.Trim()}={value?.Trim()}");
        }

        static public string OrTake(this string str, string orTake)
        {
            return String.IsNullOrEmpty(str) ? orTake : str;
        }

        static public string ToInvariantString(this object obj)
        {
            if (obj is float)
                return ((double)(float)obj).ToDoubleString();

            if (obj is double)
                return ((double)obj).ToDoubleString();

            return obj?.ToString() ?? "";
        }

        static public IEnumerable<T> OrEmpty<T>(this IEnumerable<T> list)
            => list ?? Array.Empty<T>();

        public static string ToSHA256Hash(this string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        #region Database

        static public bool IsValidFieldName(this string columnName)
        {
            string pattern = @"^[a-zA-Z][a-zA-Z0-9_]*$";

            return Regex.IsMatch(columnName, pattern);
        }

        #endregion

        #region Numbers

        //static private IFormatProvider _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

        //static public string ToDoubleString(this double d)
        //{
        //    return d.ToString(_nhi);
        //}

        #endregion

        #region Url

        static public string UrlRemoveEndingSlashes(this string url)
        {
            if (!String.IsNullOrEmpty(url))
            {
                url = url.Trim();

                while (url.EndsWith("/"))
                {
                    url = url.Substring(0, url.Length - 1);
                }
            }

            return url;
        }

        static public string UrlRemoveBeginningSlashes(this string url)
        {
            if (!String.IsNullOrEmpty(url))
            {
                url = url.Trim();

                while (url.StartsWith("/"))
                {
                    url = url.Substring(1);
                }
            }

            return url;
        }

        static public string UrlToConfigId(this string url)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    var uri = new Uri(url);
                    url = uri.Host;

                    if (uri.Port != 80 && uri.Port != 443)
                    {
                        url += "__" + uri.Port;
                    }
                }
            }
            catch
            {
                return "invalid_server_url";
            }

            return url;
        }

        static public string UrlAppendParameters(this string url, string parameters)
        {
            if (!String.IsNullOrEmpty(url))
            {
                while (url.EndsWith("&") || url.EndsWith("?"))
                {
                    url = url.Substring(0, url.Length - 1);
                }

                url += (url.Contains("?") ? "&" : "?") + parameters;
            }

            return url;
        }

        static public string UrlAppendPath(this string url, string path)
        {
            path = path?.UrlRemoveBeginningSlashes()?.UrlRemoveEndingSlashes();

            if (!String.IsNullOrEmpty(url) && !String.IsNullOrWhiteSpace(path))
            {
                url = $"{url.UrlRemoveEndingSlashes()}/{path}";
            }

            return url;
        }

        #endregion

        #region IMapDocument

        static public IMap MapFromDataset(this IMapDocument doc, IDataset dataset)
        {
            if (dataset == null)
            {
                return null;
            }

            return doc?.Maps?
                    .Where(m => m.Datasets != null && m.Datasets.Contains(dataset))
                    .FirstOrDefault();
        }

        static public IMap MapFromLayer(this IMapDocument doc, ILayer layer)
        {
            if (doc?.Maps == null)
            {
                return null;
            }

            foreach (var map in doc.Maps)
            {
                if (map?.TOC?.Elements == null)
                {
                    continue;
                }

                foreach (var element in map.TOC.Elements)
                {
                    if (element.Layers.Contains(layer))
                    {
                        return map;
                    }
                }
            }

            return null;
        }

        static public void TemporaryRestore(this IMapDocument doc)
        {
            if (doc is IPersistableTemporaryRestore)
            {
                ((IPersistableTemporaryRestore)doc).TemporaryRestore();
            }
        }

        #endregion

        #region DateTime

        static public DateTime ToUTC(this DateTime dt)
        {
            switch (dt.Kind)
            {
                case DateTimeKind.Unspecified:
                    dt = new DateTime(dt.Ticks, DateTimeKind.Local);
                    return dt.ToUniversalTime();
                case DateTimeKind.Local:
                    return dt.ToUniversalTime();
                default:
                    return dt;
            }
        }

        static public bool IsEqual(this DateTime date1, DateTime date2, int toleranceSeconds = 0, DateTimeKind defaultDateTimeKind = DateTimeKind.Local)
        {
            if (date1.Kind == DateTimeKind.Unspecified)
            {
                date1 = new DateTime(date1.Ticks, defaultDateTimeKind);
            }

            if (date2.Kind == DateTimeKind.Unspecified)
            {
                date2 = new DateTime(date2.Ticks, defaultDateTimeKind);
            }

            return Math.Abs((date1.ToUniversalTime() - date2.ToUniversalTime()).TotalSeconds) <= toleranceSeconds;
        }

        static public bool IsEqual2(this DateTime date1, DateTime date2, int toleranceSeconds = 0)
        {
            if (date1.Kind == DateTimeKind.Unspecified)
            {
                return date1.IsEqual(date2, toleranceSeconds, DateTimeKind.Local) ||
                       date1.IsEqual(date2, toleranceSeconds, DateTimeKind.Utc);
            }

            if (date2.Kind == DateTimeKind.Unspecified)
            {
                return date1.IsEqual(date2, toleranceSeconds, DateTimeKind.Local) ||
                       date1.IsEqual(date2, toleranceSeconds, DateTimeKind.Utc);
            }

            return Math.Abs((date1.ToUniversalTime() - date2.ToUniversalTime()).TotalSeconds) <= toleranceSeconds;
        }

        static public double SpanSeconds2(this DateTime date1, DateTime date2)
        {
            if (date1.IsEqual2(date2))
            {
                return 0;
            }

            return Math.Abs((date1 - date2).TotalSeconds);
        }

        #endregion

        #region Exceptions

        static public string AllMessages(this Exception ex, int maxDepth = 10)
        {
            StringBuilder sb = new StringBuilder();

            int depth = 0;
            while (ex != null)
            {
                if (sb.Length >= 0)
                {
                    sb.Append(Environment.NewLine);
                }

                sb.Append(ex.Message);

                ex = ex.InnerException;

                depth++;
                if (depth >= maxDepth)
                {
                    break;
                }
            }

            return sb.ToString();
        }

        #endregion
    }
}
