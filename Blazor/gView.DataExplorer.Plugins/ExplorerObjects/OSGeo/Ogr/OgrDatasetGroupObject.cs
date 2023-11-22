using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.VectorData;
using gView.DataSources.OGR;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.IO;
using gView.Framework.system;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.OSGeo.Ogr;

[RegisterPlugIn("d621846b-959b-4285-8506-908b527f0722")]
public class OgrDatasetGroupObject : ExplorerParentObject,
                                     IPlugInDependencies,
                                     IVectorDataExplorerGroupObject
{
    public OgrDatasetGroupObject()
        : base()
    {
    }

    #region IPlugInDependencies Member

    public bool HasUnsolvedDependencies()
    {
        return Dataset.hasUnsolvedDependencies;
    }

    #endregion

    #region IExplorerObject Member

    public string Name => "OGR Simple Feature Library";

    public string FullName => @"VectorData\OGR";


    public string Type => "OGR Connections";

    public string Icon => "basic:package";

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    #endregion

    #region ISerializableExplorerObject Member

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache?.Contains(FullName) == true)
        {
            return Task.FromResult<IExplorerObject?>(cache[FullName]);
        }

        if (this.FullName == FullName)
        {
            OgrDatasetGroupObject exObject = new OgrDatasetGroupObject();
            cache?.Append(exObject);
            return Task.FromResult<IExplorerObject?>(exObject);
        }

        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion

    #region IExplorerParentObject Members

    async public override Task<bool> Refresh()
    {
        await base.Refresh();
        base.AddChildObject(new OgrNewConnectionObject(this));


        ConfigConnections conStream = new ConfigConnections("OGR", "ca7011b3-0812-47b6-a999-98a900c4087d");
        Dictionary<string, string> connStrings = conStream.Connections;
        foreach (string connString in connStrings.Keys)
        {
            base.AddChildObject(new OgrDatasetExplorerObject(this, connString, connStrings[connString]));
        }

        return true;
    }

    #endregion

    #region IExplorerGroupObject

    public void SetParentExplorerObject(IExplorerObject parentExplorerObject)
    {
        base.Parent = parentExplorerObject;
    }

    #endregion
}
