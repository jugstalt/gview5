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

[RegisterPlugIn("B15E022B-3ECC-45F9-8E36-E02946B12945")]
public class DxfExplorerObject : ExplorerObjectCls<IExplorerObject, IFeatureClass>,
                                 IExplorerFileObject,
                                 ISerializableExplorerObject,
                                 IExplorerObjectDeletable,
                                 IPlugInDependencies
{
    private string _filename = "";

    public DxfExplorerObject() : base() { }
    public DxfExplorerObject(IExplorerObject parent, string filename)
        : base(parent, 2)
    {
        _filename = filename;
    }

    #region IExplorerFileObject

    public string Filter
    {
        get { return "*.dxf"; }
    }

    public Task<IExplorerFileObject?> CreateInstance(IExplorerObject parent, string filename)
    {
        try
        {
            if (!(new FileInfo(filename).Exists))
            {
                return Task.FromResult<IExplorerFileObject?>(null);
            }
        }
        catch
        {
            return Task.FromResult<IExplorerFileObject?>(null);
        }

        return Task.FromResult<IExplorerFileObject?>(new DxfExplorerObject(parent, filename));
    }

    public string Name
    {
        get
        {
            try
            {
                var fi = new FileInfo(_filename);
                return fi.Name;
            }
            catch { return ""; }
        }
    }

    public string FullName => _filename;

    public string Type => "DXF File";

    public string Icon => "basic:edit-text";

    async public Task<object?> GetInstanceAsync()
    {
        List<IDatasetElement> elements = await DatasetElements();

        if (elements.Count == 1)
        {
            return elements[0].Class;
        }

        return null;
    }

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

    private List<IDatasetElement>? _elements = null;
    async private Task<List<IDatasetElement>> DatasetElements()
    {
        if (_elements != null)
        {
            return _elements;
        }

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
            return _elements = elements;
        }
        catch //(Exception ex)
        {
            throw;
        }
    }

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

            return Task.FromResult(true);
        }
        catch //(Exception ex)
        {
            throw;
        }
    }

    #endregion

    public void Dispose() { }

    #region IPlugInDependencies Members
    public bool HasUnsolvedDependencies()
    {
        return Dataset.hasUnsolvedDependencies;
    }
    #endregion
}
