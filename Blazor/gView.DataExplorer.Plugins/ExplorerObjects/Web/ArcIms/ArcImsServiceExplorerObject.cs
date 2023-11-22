using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.Extensions;
using gView.Framework.DataExplorer.Abstraction;
using gView.Interoperability.ArcXML.Dataset;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Web.ArcIms;

public class ArcImsServiceExplorerObject : 
                ExplorerObjectCls<ArcImsConnectionExplorerObject, ArcIMSClass>, 
                IExplorerSimpleObject
{
    private string _name = "", _connectionString = "";
    private ArcIMSClass? _class = null;

    internal ArcImsServiceExplorerObject(ArcImsConnectionExplorerObject parent, string name, string connectionString)
        : base(parent, 1)
    {
        _name = name;
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

            return @$"{base.Parent.FullName}\{_name}";
        }
    }

    public string Type => "gView.ArcIMS Service";

    public string Icon => "basic:code-markup-box";

    public void Dispose()
    {

    }

    async public Task<object?> GetInstanceAsync()
    {

        if (_class == null)
        {
            ArcIMSDataset dataset = new ArcIMSDataset(_connectionString, _name);
            await dataset.Open(); // kein open, weil sonst ein GET_SERVICE_INFO durchgeführt wird...

            var elements = await dataset.Elements();
            if (elements.Count == 0)
            {
                dataset.Dispose();
                return null;
            }

            _class = elements[0].Class as ArcIMSClass;
        }

        return _class;
    }

    #endregion

    #region ISerializableExplorerObject Member

    async public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache != null && cache.Contains(FullName))
        {
            return cache[FullName];
        }

        FullName = FullName.Replace("/", @"\");
        int lastIndex = FullName.LastIndexOf(@"\");
        if (lastIndex == -1)
        {
            return null;
        }

        string cnName = FullName.Substring(0, lastIndex);
        string svName = FullName.Substring(lastIndex + 1, FullName.Length - lastIndex - 1);

        ArcImsConnectionExplorerObject? cnObject = new ArcImsConnectionExplorerObject();
        cnObject = await cnObject.CreateInstanceByFullName(cnName, cache) as ArcImsConnectionExplorerObject;

        if (cnObject == null)
        {
            return null;
        }

        var childObjects = await cnObject.ChildObjects();
        if (cnObject == null || childObjects == null)
        {
            return null;
        }

        foreach (IExplorerObject exObject in childObjects)
        {
            if (exObject.Name == svName)
            {
                cache?.Append(exObject);
                return exObject;
            }
        }
        return null;
    }

    #endregion
}
