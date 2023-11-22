using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.IO;
using gView.Framework.Web;
using gView.Interoperability.GeoServices.Dataset;
using gView.Interoperability.GeoServices.Rest.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using gView.Framework.system;
using gView.DataExplorer.Plugins.ExplorerObjects.Extensions;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Web.GeoServices;

public class GeoServicesFolderExplorerObject : ExplorerParentObject<GeoServicesConnectionExplorerObject>, 
                                               IExplorerSimpleObject
{
    private string _name = "", _connectionString = "";

    internal GeoServicesFolderExplorerObject(GeoServicesConnectionExplorerObject parent, string name, string connectionString)
        : base(parent,  1)
    {
        _name = name;
        _connectionString = connectionString;
    }

    #region IExplorerParentObject Member

    async public override Task<bool> Refresh()
    {
        await base.Refresh();

        try
        {
            string server = ConfigTextStream.ExtractValue(_connectionString, "server");
            string user = ConfigTextStream.ExtractValue(_connectionString, "user");
            string pwd = ConfigTextStream.ExtractValue(_connectionString, "pwd");

            var url = server
                    .UrlAppendPath(this._name)
                    .UrlAppendParameters("f=json");

            if (!String.IsNullOrEmpty(user) && !String.IsNullOrEmpty(pwd))
            {
                string token = await RequestTokenCache.RefreshTokenAsync(server, user, pwd);
                url = url.UrlAppendParameters($"token={token}");
            }

            var jsonServices = await WebFunctions.DownloadObjectAsync<JsonServices>(url);

            if (jsonServices != null)
            {
                if (jsonServices.Folders != null)
                {
                    foreach (var folder in jsonServices.Folders)
                    {
                        base.AddChildObject(
                            new GeoServicesFolderExplorerObject(
                                base.TypedParent,
                                this._name.UrlAppendPath(folder),
                                _connectionString)
                            );
                    }
                }
                if (jsonServices.Services != null)
                {
                    foreach (var service in jsonServices.Services.Where(s => s.Type.ToLower() == "mapserver"))
                    {
                        base.AddChildObject(
                            new GeoServicesServiceExplorerObject(
                                this,
                                service.ServiceName,
                                this._name,
                                _connectionString));
                    }
                }
            }

            return true;
        }
        catch //(Exception ex)
        {
            throw;
        }
    }

    #endregion

    #region IExplorerObject Member

    public string Name => _name; 

    public string FullName
    {
        get
        {
            if (base.Parent.IsNull())
            {
                return "";
            }

            return @$"{base.Parent.FullName}\{_name}";
        }
    }

    public string Type => "gView.GeoServices Folder";

    public string Icon => "basic:folder";

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    #endregion

    #region ISerializableExplorerObject Member

    async public Task<IExplorerObject?> CreateInstanceByFullName(string fullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache != null && cache.Contains(fullName))
        {
            return cache[fullName];
        }

        fullName = fullName.Replace("/", @"\");

        GeoServicesExplorerObjects group = new GeoServicesExplorerObjects();
        GeoServicesConnectionExplorerObject? connectionExObject = null;
        foreach (var connectionObject in (await group.ChildObjects()).OrderByDescending(e => e.FullName.Length))
        {
            if (fullName.StartsWith($@"{connectionObject.FullName}\"))
            {
                connectionExObject = connectionObject as GeoServicesConnectionExplorerObject;
                break;
            }
        }

        if (connectionExObject == null)
        {
            return null;
        }

        string name = fullName.Substring(connectionExObject.FullName.Length + 1);

        if (name.Contains(@"\"))
        {
            return null;
        }

        var folderExObject = new GeoServicesFolderExplorerObject(connectionExObject, name, connectionExObject._connectionString);
        if (folderExObject != null)
        {
            cache?.Append(folderExObject);

            return folderExObject;
        }

        return null;
    }

    #endregion
}
