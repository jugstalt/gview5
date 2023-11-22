using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.OSGeo.Ogr.ContextTools;
using gView.DataSources.OGR;
using gView.DataSources.OSGeo;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.IO;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.OSGeo.Ogr;

[RegisterPlugIn("cae5aa56-0e41-4566-8031-6f18713d865d")]
public class OgrDatasetExplorerObject : ExplorerParentObject<OgrDatasetGroupObject>,
                                        IExplorerSimpleObject,
                                        IExplorerObjectDeletable,
                                        IExplorerObjectRenamable,
                                        ISerializableExplorerObject,
                                        IExplorerObjectContextTools
{
    private string _name = String.Empty, _connectionString = String.Empty;
    private Dataset? _dataset;
    private IEnumerable<IExplorerObjectContextTool>? _contextTools = null;

    public OgrDatasetExplorerObject()
        : base() { }
    public OgrDatasetExplorerObject(OgrDatasetGroupObject parent, string name, string connectionString)
        : base(parent, 1)
    {
        _name = name;
        _connectionString = connectionString;

        _contextTools = new IExplorerObjectContextTool[]
        {
            new UpdateConnectionString()
        };
    }

    internal bool UpdateConnectionString(string connectionString)
    {
        ConfigConnections connStream = new ConfigConnections("OGR", "ca7011b3-0812-47b6-a999-98a900c4087d");
        connStream.Add(_name, this.ConnectionString = connectionString);

        return true;
    }

    internal string ConnectionString
    {
        get
        {
            return _connectionString;
        }
        set
        {
            _connectionString = value;
            _dataset = null;
        }
    }

    #region IExplorerObjectContextTools

    public IEnumerable<IExplorerObjectContextTool> ContextTools => _contextTools ?? Array.Empty<IExplorerObjectContextTool>();

    #endregion

    #region IExplorerObject Members

    public string Name => _name;

    public string FullName => @$"VectorData\OGR\{_name}";

    public string Type => "Ogr Dataset";

    public string Icon => "basic:cloud-connect";

    new public void Dispose()
    {
        base.Dispose();
    }
    async public Task<object?> GetInstanceAsync()
    {
        if (_dataset == null)
        {
            _dataset = new Dataset();
            await _dataset.SetConnectionString(_connectionString);
            if (await _dataset.Open())
            {
                return _dataset;
            }
        }
        return null;
    }

    #endregion

    private string[] LayerNames
    {
        get
        {
            Initializer.RegisterAll();

            if (Initializer.InstalledVersion == GdalVersion.V1)
            {
                OSGeo_v1.OGR.DataSource dataSource = OSGeo_v1.OGR.Ogr.Open(this.ConnectionString, 0);
                if (dataSource != null)
                {
                    List<string> layers = new List<string>();
                    for (int i = 0; i < Math.Min(dataSource.GetLayerCount(), 20); i++)
                    {
                        OSGeo_v1.OGR.Layer ogrLayer = dataSource.GetLayerByIndex(i);
                        if (ogrLayer == null)
                        {
                            continue;
                        }

                        layers.Add(ogrLayer.GetName());
                    }
                    return layers.ToArray();
                }
            }
            else if (Initializer.InstalledVersion == GdalVersion.V3)
            {
                OSGeo_v3.OGR.DataSource dataSource = OSGeo_v3.OGR.Ogr.Open(this.ConnectionString, 0);
                if (dataSource != null)
                {
                    List<string> layers = new List<string>();
                    for (int i = 0; i < Math.Min(dataSource.GetLayerCount(), 20); i++)
                    {
                        OSGeo_v3.OGR.Layer ogrLayer = dataSource.GetLayerByIndex(i);
                        if (ogrLayer == null)
                        {
                            continue;
                        }

                        layers.Add(ogrLayer.GetName());
                    }
                    return layers.ToArray();
                }
            }

            return Array.Empty<string>();
        }
    }


    #region IExplorerParentObject Members

    async public override Task<bool> Refresh()
    {
        await base.Refresh();

        Dataset dataset = new Dataset();
        await dataset.SetConnectionString(_connectionString);
        await dataset.Open();

        foreach (IDatasetElement element in await dataset.Elements())
        {
            if (element.Class is IFeatureClass)
            {
                base.AddChildObject(new OgrLayerExplorerObject(this, element));
            }
        }

        return true;
    }  
    
    #endregion

    #region ISerializableExplorerObject Member

    async public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache?.Contains(FullName) == true)
        {
            return cache[FullName];
        }

        OgrDatasetGroupObject? group = new OgrDatasetGroupObject();
        if (FullName.IndexOf(group.FullName) != 0 || FullName.Length < group.FullName.Length + 2)
        {
            return null;
        }

        group = (OgrDatasetGroupObject?)((cache?.Contains(group.FullName) == true) ? cache[group.FullName] : group);

        if (group != null)
        {
            foreach (IExplorerObject exObject in await group.ChildObjects())
            {
                if (exObject.FullName == FullName)
                {
                    cache?.Append(exObject);
                    return exObject;
                }
            }
        }

        return null;
    }

    #endregion

    #region IExplorerObjectDeletable Member

    public event ExplorerObjectDeletedEvent? ExplorerObjectDeleted;

    public Task<bool> DeleteExplorerObject(ExplorerObjectEventArgs e)
    {
        ConfigConnections stream = new ConfigConnections("OGR", "ca7011b3-0812-47b6-a999-98a900c4087d");
        stream.Remove(_name);

        if (ExplorerObjectDeleted != null)
        {
            ExplorerObjectDeleted(this);
        }

        return Task.FromResult(true);
    }

    #endregion

    #region IExplorerObjectRenamable Member

    public event ExplorerObjectRenamedEvent? ExplorerObjectRenamed;

    public Task<bool> RenameExplorerObject(string newName)
    {
        bool ret = false;
        ConfigConnections stream = new ConfigConnections("OGR", "ca7011b3-0812-47b6-a999-98a900c4087d");
        ret = stream.Rename(_name, newName);

        if (ret == true)
        {
            _name = newName;
            if (ExplorerObjectRenamed != null)
            {
                ExplorerObjectRenamed(this);
            }
        }
        return Task.FromResult(ret);
    }

    #endregion
}
