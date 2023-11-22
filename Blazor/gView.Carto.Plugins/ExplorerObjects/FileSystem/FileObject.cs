using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.system;

namespace gView.Carto.Plugins.ExplorerObjects.FileSystem;

[RegisterPlugIn("218C30B9-78F8-4B49-9EDE-6E8D6EA7D6F1")]
public class FileObject : ExplorerObjectCls<IExplorerObject, string>,
                          IExplorerFileObject,
                          ISerializableExplorerObject,
                          IExplorerObjectDeletable
{
    private string _filename = "";

    public FileObject() : base() { }
    public FileObject(IExplorerObject parent, string filename)
        : base(parent, 2)
    {
        _filename = filename;
    }

    #region IExplorerFileObject

    public string Filter
    {
        get { return "*.*"; }
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

        return Task.FromResult<IExplorerFileObject?>(new FileObject(parent, filename));
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

    public string Type => $"{this.Name.Split(".").Last()} File";

    public string Icon => "basic:square-medium";

    public Task<object?> GetInstanceAsync()
    {
        return Task.FromResult<object?>(this.FullName);
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
}
