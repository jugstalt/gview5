using gView.Cmd.Core.Abstraction;
using gView.Cmd.Core.Extensions;
using gView.Framework.Data;
using gView.Framework.FDB;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Cmd.Core.Builders;

public class DatasetParameterBuilder : ICommandPararmeterBuilder
{
    private readonly string _parameterPrefix;

    public DatasetParameterBuilder(string parameterPrefix = "")
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
            new RequiredCommandParameter<Guid>("guid".PrependPrefix(_parameterPrefix))
            {
                Description="Plugin Guid of dataset plugin"
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

        var guid = parameters.GetRequiredValue<Guid>("guid".PrependPrefix(_parameterPrefix));
        var plugin = PlugInManager.Create(guid);
        IFeatureDataset? dataset;

        if (plugin == null)
        {
            throw new Exception($"Plugin with guid {guid} is not a registeterd feature dataset");
        }

        string connetionString = parameters.GetRequiredValue<string>("connstr".PrependPrefix(_parameterPrefix));

        if (plugin is IFileFeatureDatabase)
        {
            IFileFeatureDatabase fileDB = (IFileFeatureDatabase)plugin;
            if (!await fileDB.Open(connetionString))
            {
                throw new Exception("Error opening destination database:" + fileDB.LastErrorMessage);
            }

            dataset = await fileDB.GetDataset(connetionString);
        }
        else if (plugin is IFeatureDataset)
        {
            dataset = (IFeatureDataset)plugin;
            await dataset.SetConnectionString(connetionString);

            if (!await dataset.Open())
            {
                throw new Exception("Error opening destination dataset:" + dataset.LastErrorMessage);
            }
        }
        else
        {
            throw new Exception($"Component with GUID '{guid}' is not a feature dataset...");
        }

        return dataset;
    }
}
