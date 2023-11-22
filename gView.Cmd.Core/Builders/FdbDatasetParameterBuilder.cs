using gView.Cmd.Core.Abstraction;
using gView.Cmd.Core.Extensions;
using gView.DataSources.Fdb.MSSql;
using gView.DataSources.Fdb.PostgreSql;
using gView.DataSources.Fdb.SQLite;
using gView.Framework.Data;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Cmd.Core.Builders;
public class FdbDatasetParameterBuilder : ICommandPararmeterBuilder
{
    private readonly string _parameterPrefix;

    public FdbDatasetParameterBuilder(string parameterPrefix = "")
    {
        _parameterPrefix = parameterPrefix;
    }

    #region ICommandPararmeterBuilder

    public IEnumerable<ICommandParameterDescription> ParameterDescriptions
        => new ICommandParameterDescription[]
        {
            new RequiredCommandParameter<string>("connstr".PrependPrefix(_parameterPrefix))
            {
                Description="ConnectionString to dataset"
            },
            new CommandParameter<Guid>("fdb".PrependPrefix(_parameterPrefix))
            {
                Description="FDB Type: <sql|postgres|sqlite>"
            },
            new CommandParameter<Guid>("guid".PrependPrefix(_parameterPrefix))
            {
                Description="Or Plugin Guid of dataset plugin"
            }
        };

    async public Task<T> Build<T>(IDictionary<string, object> parameters)
    {
        return typeof(T) switch
        {
            Type t when t == typeof(IDataset) => (T)await BuildDataset(parameters),
            Type t when t == typeof(IFeatureDataset) => (T)await BuildDataset(parameters),
            _ => throw new ArgumentException($"Can't build type {typeof(T).Name}")
        };
    }

    #endregion

    async private Task<IDataset> BuildDataset(IDictionary<string, object> parameters)
    {
        if (parameters.ContainsKey(_parameterPrefix) &&
            parameters[_parameterPrefix] is IFeatureDataset)
        {
            return (IFeatureDataset)parameters[_parameterPrefix];
        }

        var fdbType = parameters.GetValue<string>("fdb".PrependPrefix(_parameterPrefix));

        IFeatureDataset? dataset = null;

        if (!String.IsNullOrEmpty(fdbType))
        {
            switch (fdbType!.ToLower())
            {
                case "sql":
                    dataset = new SqlFDBDataset();
                    break;
                case "pg":
                case "postgres":
                    dataset = new pgDataset();
                    break;
                case "sqlite":
                    dataset = new SQLiteFDBDataset();
                    break;
                default:
                    throw new Exception($"Unknown FDB Type {fdbType}");
            }
        }
        else
        {
            var guid = parameters.GetRequiredValue<Guid>("guid".PrependPrefix(_parameterPrefix));
            dataset = PlugInManager.Create(guid) as IFeatureDataset;

            if (dataset == null)
            {
                throw new Exception($"Plugin with guid {guid} is not a registeterd feature dataset");
            }
        }

        string connetionString = parameters.GetRequiredValue<string>("connstr".PrependPrefix(_parameterPrefix));
        await dataset.SetConnectionString(connetionString);

        if (!await dataset.Open())
        {
            throw new Exception("Error opening destination dataset:" + dataset.LastErrorMessage);
        }

        return dataset;
    }
}
