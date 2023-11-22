using gView.Blazor.Core.Extensions;
using gView.Carto.Core.Abstractions;
using gView.Carto.Core.Models.Tree;
using gView.Carto.Core.Services.Abstraction;
using gView.Framework.Carto;
using gView.Framework.UI;

namespace gView.Carto.Razor.Components.Trees
{
    public partial class ContentTree
    {
        private ICartoApplicationScopeService? _cartoScope;

        private Task SetAppScope(ICartoApplicationScopeService cartoApplicationScopeService)
        {
            _cartoScope = cartoApplicationScopeService;

            InitEvents();

            return Rebuild();
        }

        private Task OnCartoDocumentLoadedAsync(ICartoDocument arg)
        {
            return Rebuild();
        }

        private Task OnMapSettingsChangedAsync()
        {
            return Rebuild();
        }

        private async Task Rebuild()
        {
            if (_cartoScope == null)
            {
                return;
            }

            _treeNodes.Clear();

            var map = _cartoScope.Document?.Map;

            Rebuild(map, null);

            fullReloadKey = Guid.NewGuid();
            await _cartoScope.EventBus.FireRefreshContentTreeAsync();
        }

        private void Rebuild(IMap? map, TocTreeNode? parentTreeNode)
        {
            if (map?.TOC?.Elements != null)
            {
                foreach (var tocElement in map.TOC.Elements.Where(e => e.ParentGroup == parentTreeNode?.TocElement))
                {
                    var locked = 
                        tocElement.LayerLocked
                        || tocElement.Layers?.All(l => l.IsLocked()) == true;

                    if (locked)
                    {
                        continue;
                    }

                    TocTreeNode? childTreeNode = tocElement.ElementType switch
                    {
                        TocElementType.OpenedGroup => new TocParentNode(tocElement),
                        TocElementType.ClosedGroup => new TocParentNode(tocElement),
                        TocElementType.Layer => new TocLayerNode(tocElement),
                        TocElementType.Legend => new TocLegendNode(tocElement),
                        _ => null,
                    };

                    if (childTreeNode != null)
                    {
                        if (parentTreeNode == null)
                        {
                            _treeNodes.Add(childTreeNode);
                        }
                        else
                        {
                            parentTreeNode.Children = parentTreeNode.Children ?? new HashSet<TocTreeNode>();
                            parentTreeNode.Children.Add(childTreeNode);
                        }
                    }

                    if (childTreeNode is TocParentNode)
                    {
                        Rebuild(map, childTreeNode);
                    }
                }
            }
        }

        private Task RefreshContentTree()
            => this.InvokeAsync(() =>
            {
                StateHasChanged();
            });

        private void InitEvents()
        {
            EventBus.OnCartoDocumentLoadedAsync += OnCartoDocumentLoadedAsync;
            EventBus.OnMapSettingsChangedAsync += OnMapSettingsChangedAsync;
            EventBus.OnRefreshContentTreeAsync += RefreshContentTree;
        }

        private void RelaseEvents()
        {
            EventBus.OnRefreshContentTreeAsync -= RefreshContentTree;
            EventBus.OnMapSettingsChangedAsync -= OnMapSettingsChangedAsync;
            EventBus.OnCartoDocumentLoadedAsync -= OnCartoDocumentLoadedAsync;

        }

        public void Dispose()
        {

            RelaseEvents();

            foreach (var treeNode in _treeNodes)
            {
                treeNode?.Dispose();
            }
        }
    }
}
