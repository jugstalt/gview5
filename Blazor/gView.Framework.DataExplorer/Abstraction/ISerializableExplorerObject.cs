using System.Threading.Tasks;

namespace gView.Framework.DataExplorer.Abstraction
{
    public interface ISerializableExplorerObject
    {
        Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache);
    }

}
