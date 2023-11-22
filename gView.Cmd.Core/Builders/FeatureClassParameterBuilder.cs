using gView.Cmd.Core.Abstraction;
using gView.Cmd.Core.Extensions;
using gView.Framework.Data;
using gView.Framework.FDB;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Cmd.Core.Builders;
public class FeatureClassParameterBuilder : ICommandPararmeterBuilder
{
    private readonly string _parameterPrefix;

    public FeatureClassParameterBuilder(string parameterPrefix = "")
    {
        _parameterPrefix = parameterPrefix;
    }

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
            },
            new RequiredCommandParameter<string>("fc".PrependPrefix(_parameterPrefix))
            {
                Description="Name of the FeatureClass"
            }
        };

    async public Task<T> Build<T>(IDictionary<string, object> parameters)
    {
        return typeof(T) switch
        {
            Type t when t == typeof(IFeatureClass) => (T)await BuildFeatureClass(parameters),
            Type t when t == typeof(IFeatureDataset) => (T)await BuildFeatureDataset(parameters),
            _ => throw new ArgumentException($"Can't build type {typeof(T).Name}")
        };
    }

    async private Task<IFeatureClass> BuildFeatureClass(IDictionary<string, object> parameters)
    {
        if (parameters.ContainsKey(_parameterPrefix) &&
            parameters[_parameterPrefix] is IFeatureClass)
        {
            return (IFeatureClass)parameters[_parameterPrefix];
        }

        var dataset = await BuildFeatureDataset(parameters);
        var fcName = parameters.GetRequiredValue<string>("fc".PrependPrefix(_parameterPrefix));
        var datasetElement = await dataset.Element(fcName);

        if (datasetElement == null)
        {
            throw new ArgumentException($"Unknown dataset element {fcName}");
        }
        if (datasetElement.Class is not IFeatureClass)
        {
            throw new ArgumentException($"{fcName} is not a FeatureClass");
        }

        return (IFeatureClass)datasetElement.Class;
    }

    async private Task<IFeatureDataset> BuildFeatureDataset(IDictionary<string, object> parameters)
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

    async public Task<IFeatureClass?> BuildFeatureClass(IFeatureDataset ds, IDictionary<string, object> parameters)
    {
        string fcName = parameters.GetRequiredValue<string>("fc".PrependPrefix(_parameterPrefix));

        IDatasetElement? element = await ds.Element(fcName);
        if (element != null && element.Class is IFeatureClass)
        {
            return (IFeatureClass)element.Class;
        }

        foreach (IDatasetElement element2 in await ds.Elements())
        {
            if (element2.Class is IFeatureClass)
            {
                if (element2.Class.Name == fcName)
                {
                    return (IFeatureClass)element2.Class;
                }
            }
        }

        return null;
    }
}
