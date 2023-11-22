using gView.Cmd.Core.Abstraction;
using gView.Framework.system;
using System;
using System.Collections.Generic;

namespace gView.Cmd.Core.Extensions;
static public class DictionaryExtensions
{
    static public bool HasKey(this IDictionary<string, object> parameters, string key)
        => parameters != null && parameters.ContainsKey(key);

    static public T? GetValue<T>(this IDictionary<string, object> parameters, string key, bool isRequired = false)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        if (!parameters.ContainsKey(key))
        {
            if (isRequired == true)
            {
                throw new ArgumentException($"parameter {key} is required");
            }

            return default(T);
        }

        if (typeof(T) == typeof(Guid))
        {
            return (T)(object)new Guid(parameters[key].ToString());
        }
        else if (typeof(T) == typeof(float))
        {
            return (T)(object)parameters[key].ToString().ToFloat();
        }
        else if (typeof(T) == typeof(double))
        {
            return (T)(object)parameters[key].ToString().ToDouble();
        }
        else if (typeof(T).IsEnum)
        {
            return (T)Enum.Parse(typeof(T), parameters[key].ToString());
        }

        return (T)Convert.ChangeType(parameters[key], typeof(T));
    }

    static public T? GetValueOrDefault<T>(this IDictionary<string, object> parameters, string key, T? defaultValue)
    {
        if (parameters == null || !parameters.ContainsKey(key))
        {
            return defaultValue;
        }

        return parameters.GetValue<T>(key) ?? defaultValue;
    }

    static public T GetRequiredValue<T>(this IDictionary<string, object> parameters, string key)
        => parameters.GetValue<T>(key, true) ??
                throw new Exception("Required parameter {key} can't be null");

    static public IEnumerable<T> GetArray<T>(this IDictionary<string, object> parameters, string key, bool isRequired = false)
    {
        var arrayString = parameters.GetValue<string>(key, isRequired);

        if (String.IsNullOrEmpty(arrayString))
        {
            yield break;
        }

        foreach (var val in arrayString!.Split(';'))
        {
            yield return (T)Convert.ChangeType(val, typeof(T));
        }
    }

    static public IDictionary<string, object> ParseCommandLineArguments(this string[] args)
    {
        Dictionary<string, object> parameters = new Dictionary<string, object>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--"))
            {
                var val = i < args.Length - 1 ? args[i + 1] : string.Empty;
                if (!val.StartsWith("--"))
                {
                    parameters[args[i].Substring(2)] = val;
                    i++;
                }
                else
                {
                    parameters[args[i].Substring(2)] = string.Empty;
                }
            }
            else if (args[i].StartsWith("-"))
            {
                var val = i < args.Length - 1 ? args[i + 1] : string.Empty;
                if (!val.StartsWith("-"))
                {
                    parameters[args[i].Substring(1)] = val;
                    i++;
                }
                else
                {
                    parameters[args[i].Substring(1)] = string.Empty;
                }
            }
        }

        return parameters;
    }

    static public bool VerifyParameters(this IDictionary<string, object> parameters, IEnumerable<ICommandParameterDescription> parameterDescriptions, ICommandLogger? logger = null)
    {
        foreach (var parameterDescription in parameterDescriptions)
        {
            if (parameterDescription.IsRequired == false)
            {
                continue;
            }

            try
            {
                if (parameterDescription.ParameterType.IsValueType == true || parameterDescription.ParameterType == typeof(string))
                {

                    if (!parameters.ContainsKey(parameterDescription.Name) ||
                        string.IsNullOrEmpty(parameters[parameterDescription.Name]?.ToString()))
                    {
                        throw new Exception("Value is required");
                    }

                    var val = parameterDescription.ParameterType switch
                    {
                        Type t when t == typeof(Guid) => new Guid(parameters[parameterDescription.Name].ToString()),
                        _ => Convert.ChangeType(parameters[parameterDescription.Name], parameterDescription.ParameterType)
                    };
                }
                else
                {
                    ICommandPararmeterBuilder parameterBuilder = parameterDescription.GetBuilder();

                    if(!parameters.VerifyParameters(parameterBuilder.ParameterDescriptions, logger))
                    {
                        return false;
                    }

                }
            }
            catch (Exception ex)
            {
                logger?.LogLine($"Exception: Parameter {parameterDescription.Name}");
                logger?.LogLine(ex.Message);

                return false;
            }
        }

        return true;
    }
}