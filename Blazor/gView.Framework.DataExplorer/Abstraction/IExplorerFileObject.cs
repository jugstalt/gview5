using System.Threading.Tasks;

namespace gView.Framework.DataExplorer.Abstraction
{
    public interface IExplorerFileObject : IExplorerObject
    {
        string Filter { get; }
        Task<IExplorerFileObject?> CreateInstance(IExplorerObject parent, string filename);
    }
}
