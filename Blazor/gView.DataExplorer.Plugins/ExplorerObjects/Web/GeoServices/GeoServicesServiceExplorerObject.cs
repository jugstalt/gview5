using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.Extensions;
using gView.Framework.DataExplorer.Abstraction;
using gView.Interoperability.GeoServices.Dataset;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Web.GeoServices;

public class GeoServicesServiceExplorerObject : ExplorerParentObject<IExplorerObject, GeoServicesClass>, 
                                                IExplorerSimpleObject
{
    private string _name = "", _connectionString = "", _folder = "";
    private GeoServicesClass? _class = null;

    internal GeoServicesServiceExplorerObject(IExplorerObject parent, string name, string folder, string connectionString)
        : base(parent, 1)
    {
        _name = name;
        _folder = folder;
        _connectionString = connectionString;
    }

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

            return base.Parent.FullName +
                $@"\{_name}";
        }
    }

    public string Type => "gView.GeoServices Service";

    public string Icon => "basic:code-c-box";

    async public Task<object?> GetInstanceAsync()
    {

        if (_class == null)
        {
            GeoServicesDataset dataset = new GeoServicesDataset(
                _connectionString,
                (String.IsNullOrWhiteSpace(_folder) ? "" : $"{_folder}/") + _name);

            await dataset.Open();

            var elements = await dataset.Elements();
            if (elements.Count == 0)
            {
                dataset.Dispose();
                return null;
            }

            _class = elements[0].Class as GeoServicesClass;
        }

        return _class;
    }

    #endregion

    #region IExplorerParentObject Member

    async public override Task<bool> Refresh()
    {
        try
        {
            await base.Refresh();
            await GetInstanceAsync();

            if (_class?.Themes != null)
            {

                foreach (var theme in _class.Themes)
                {
                    if (theme?.Class is GeoServicesFeatureClass)
                    {
                        base.AddChildObject(new GeoServicesServiceLayerExplorerObject(this,
                            (GeoServicesFeatureClass)theme.Class));
                    }
                }
            }
        }
        catch //(Exception ex)
        {
            throw;
        }

        return true;
    }

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

        string name = fullName.Substring(connectionExObject.FullName.Length + 1), folderName = "";

        IExplorerObject? parentExObject;

        if (name.Contains(@"\"))
        {
            folderName = name.Substring(0, name.LastIndexOf(@"\"));
            name = name.Substring(name.LastIndexOf(@"\") + 1);
            parentExObject = await new GeoServicesFolderExplorerObject(new GeoServicesConnectionExplorerObject(), String.Empty, String.Empty).CreateInstanceByFullName($@"{connectionExObject.FullName}\{folderName}", null);
        }
        else
        {
            parentExObject = connectionExObject;
        }

        if (parentExObject != null)
        {
            var serviceExObject = new GeoServicesServiceExplorerObject(parentExObject, name, folderName, connectionExObject._connectionString);
            if (serviceExObject != null)
            {
                cache?.Append(serviceExObject);

                return serviceExObject;
            }
        }

        return null;
    }

    #endregion
}
