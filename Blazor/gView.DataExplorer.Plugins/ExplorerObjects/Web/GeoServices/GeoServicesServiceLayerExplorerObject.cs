using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.Extensions;
using gView.Framework.DataExplorer.Abstraction;
using gView.Interoperability.GeoServices.Dataset;
using System;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Web.GeoServices;

[gView.Framework.system.RegisterPlugIn("5133CFA1-AA5E-47FC-990A-462772158CA5")]
public class GeoServicesServiceLayerExplorerObject : ExplorerObjectCls<IExplorerObject, GeoServicesFeatureClass>,
                                                     IExplorerSimpleObject
{
    private readonly GeoServicesFeatureClass? _fc;

    public GeoServicesServiceLayerExplorerObject()
        : base()
    {
    }

    public GeoServicesServiceLayerExplorerObject(IExplorerObject parent, GeoServicesFeatureClass featureClass)
        : base(parent, 1)
    {
        _fc = featureClass;
    }

    public string Name => _fc != null ? _fc.Name : string.Empty;

    public string FullName
    {
        get
        {
            if (base.Parent.IsNull() || _fc == null)
            {
                return "";
            }

            return base.Parent.FullName +
                $@"\{_fc.ID}";
        }
    }

    public string Type => "Service layer";

    public string Icon => "basic:code-c";

    async public Task<IExplorerObject?> CreateInstanceByFullName(string fullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache != null && cache.Contains(fullName))
        {
            return cache[fullName];
        }

        fullName = fullName.Replace("/", @"\");
        int pos = fullName.LastIndexOf(@"\");

        if (pos < 0)
        {
            return null;
        }

        string layerId = fullName.Substring(pos + 1);
        string parentFullName = fullName.Substring(0, pos);

        var parentObject =
            (IExplorerParentObject?)(await new GeoServicesFolderExplorerObject(new GeoServicesConnectionExplorerObject(), String.Empty, String.Empty).CreateInstanceByFullName(parentFullName, null)) ??
            (IExplorerParentObject?)(await new GeoServicesServiceExplorerObject(new NullParentExplorerObject(), String.Empty, String.Empty, String.Empty).CreateInstanceByFullName(parentFullName, null));

        if (parentObject != null)
        {
            foreach (var child in await parentObject.ChildObjects())
            {
                if (child.FullName == fullName)
                {
                    cache?.Append(child);
                    return child;
                }
            }
        }

        return null;
    }

    public void Dispose()
    {

    }

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(_fc);
}
