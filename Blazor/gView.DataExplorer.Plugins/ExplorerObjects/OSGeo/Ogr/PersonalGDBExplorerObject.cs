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

[RegisterPlugIn("095A5627-DCC3-4A58-AD81-336873CDC73B")]
public class PersonalGDBExplorerObject : ExplorerParentObject<IExplorerObject>,
                                         IExplorerFileObject,
                                         ISerializableExplorerObject,
                                         IExplorerObjectDeletable,
                                         IPlugInDependencies
{
    private string _filename = "";

    public PersonalGDBExplorerObject() : base() { }
    public PersonalGDBExplorerObject(IExplorerObject parent, string filename)
        : base(parent, 2)
    {
        _filename = filename;
    }
    #region IExplorerObject Members

    public string Filter
    {
        get { return "*.mdb"; }
    }

    public string Name
    {
        get
        {
            try
            {
                System.IO.FileInfo fi = new System.IO.FileInfo(_filename);
                return fi.Name;
            }
            catch { return ""; }
        }
    }

    public string FullName
    {
        get
        {
            return _filename;
        }
    }

    public string Type
    {
        get { return "ESRI Personal Geodatabase"; }
    }

    public string Icon => "basic:database";

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    async public Task<IExplorerFileObject?> CreateInstance(IExplorerObject parent, string filename)
    {
        try
        {
            if (!(new FileInfo(filename).Exists))
            {
                return null;
            }

            using (Dataset dataset = new Dataset())
            {
                await dataset.SetConnectionString(filename);
                if (await dataset.Open() == false)
                {
                    return null;
                }
            }
        }
        catch { return null; }

        return new PersonalGDBExplorerObject(parent, filename);
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
                base.AddChildObject(new PersonalGDBFeatureClassExplorerObject(this, _filename, element));
            }
        }

        return true;
    }

    #endregion

    #region ISerializableExplorerObject Member

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
        catch // (Exception ex)
        {
            throw;
        }
    }

    #endregion

    #region IPlugInDependencies Members
    public bool HasUnsolvedDependencies()
    {
        return Dataset.hasUnsolvedDependencies;
    }
    #endregion
}
