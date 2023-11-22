using gView.Carto.Core.Abstractions;
using gView.Carto.Core.Models.Tree;
using System.Threading.Tasks;

namespace gView.Carto.Core.Services.Abstraction;
public interface ICartoApplicationScopeService
{
    public ICartoDocument Document { get; }

    public CartoEventBusService EventBus { get; }

    TocTreeNode? SelectedTocTreeNode { get; }
    Task SetSelectedTocTreeNode(TocTreeNode? selectedTocTreeNode);
}
