using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataSources.OGR;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.system;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.OSGeo.Ogr;

[RegisterPlugIn("6D0126BD-144B-42CA-96BE-ED0C4BCD18D8")]
public class KmlExplorerObject : ExplorerParentObject<IExplorerObject>,
                                 IExplorerFileObject,
                                 ISerializableExplorerObject,
                                 IExplorerObjectDeletable,
                                 IPlugInDependencies
{
    private string _filename = "";

    public KmlExplorerObject() : base() { }
    public KmlExplorerObject(IExplorerObject parent, string filename)
        : base(parent, 2)
    {
        _filename = filename;
    }

    #region IExplorerFileObject
    public string Filter
    {
        get { return "*.kml"; }
    }

    public Task<IExplorerFileObject?> CreateInstance(IExplorerObject parent, string filename)
    {
        try
        {
            if (new FileInfo(filename).Exists)
            {
                return Task.FromResult<IExplorerFileObject?>(new KmlExplorerObject(parent, filename));
            }
        }
        catch { }

        return Task.FromResult<IExplorerFileObject?>(null);
    }

    public string Name
    {
        get
        {
            try
            {
                return new FileInfo(_filename).Name;
            }
            catch { return ""; }
        }
    }

    public string FullName => _filename;

    public string Type => "KML File";

    public string Icon => "basic:globe2";

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    #endregion

    #region ISerializableExplorerObject Members

    async public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        IExplorerObject? obj = (cache?.Contains(FullName) == true) ? cache[FullName] : await CreateInstance(new NullParentExplorerObject(), FullName);

        if (obj != null)
        {
            cache?.Append(obj);
        }

        return obj;
    }

    #endregion

    async private Task<List<IDatasetElement>> DatasetElements()
    {
        try
        {
            Dataset dataset = new Dataset();
            await dataset.SetConnectionString(_filename);

            List<IDatasetElement> elements = new List<IDatasetElement>();
            await dataset.Open();
            foreach (IDatasetElement element in await dataset.Elements())
            {
                elements.Add(element);
            }

            dataset.Dispose();
            return elements;
        }
        catch //(Exception ex)
        {
            throw;
        }
    }

    #region IExplorerParentObject Members

    async public override Task<bool> Refresh()
    {
        await base.Refresh();
        List<IDatasetElement> elements = await DatasetElements();
        if (elements != null)
        {
            foreach (IDatasetElement element in elements)
            {
                base.AddChildObject(new KmlFeatureClassExplorerObject(this, _filename, element));
            }
        }

        return true;
    }

    #endregion

    #region IExplorerObjectDeletable Member

    public event ExplorerObjectDeletedEvent? ExplorerObjectDeleted;

    public Task<bool> DeleteExplorerObject(ExplorerObjectEventArgs e)
    {
        try
        {
            FileInfo fi = new FileInfo(_filename);
            fi.Delete();
            if (ExplorerObjectDeleted != null)
            {
                ExplorerObjectDeleted(this);
            }

            return Task.FromResult<bool>(true);
        }
        catch //(Exception ex)
        {
            throw;
        }
    }

    #endregion

    new public void Dispose()
    {
        base.Dispose();
    }

    #region IPlugInDependencies Members
    public bool HasUnsolvedDependencies()
    {
        return Dataset.hasUnsolvedDependencies;
    }
    #endregion
}
