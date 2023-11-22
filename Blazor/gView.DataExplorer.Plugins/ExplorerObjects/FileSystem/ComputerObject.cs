using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.IO;
using gView.Framework.system;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.FileSystem;

[RegisterPlugIn("2453E8C6-E52F-4D17-9DE0-9EAC8343B66B")]
public class ComputerObject : ExplorerParentObject,
                              IExplorerGroupObject
{
    public ComputerObject()
        : base(0)
    {
    }

    #region IExplorerObject Member

    public string Name
    {
        get { return "Computer"; }
    }

    public string FullName
    {
        get { return ""; }
    }

    public string? Type
    {
        get { return "Filesystem"; }
    }

    public string Icon => "basic:monitor";

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    #endregion

    #region ISerializableExplorerObject Member

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (FullName == string.Empty)
        {
            return Task.FromResult<IExplorerObject?>(new ComputerObject());
        }

        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion

    #region IExplorerParentObject Member

    async public override Task<bool> Refresh()
    {
        await base.Refresh();

        string[] drives = System.IO.Directory.GetLogicalDrives();

        foreach (string drive in drives)
        {
            System.IO.DriveInfo info = new System.IO.DriveInfo(drive);

            DriveObject exObject = new DriveObject(this, info.Name.Replace("\\", ""), (uint)info.DriveType);
            AddChildObject(exObject);
        }

        ConfigConnections configStream = new ConfigConnections("directories");
        Dictionary<string, string> networkDirectories = configStream.Connections;
        if (networkDirectories != null)
        {
            foreach (string dir in networkDirectories.Keys)
            {
                MappedDriveObject exObject = new MappedDriveObject(this, networkDirectories[dir]);
                AddChildObject(exObject);
            }
        }

        return true;
    }

    #endregion

    #region IExplorerGroupObject

    public void SetParentExplorerObject(IExplorerObject parentExplorerObject)
    {
        Parent = parentExplorerObject;
    }

    #endregion
}
