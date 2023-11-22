using gView.Carto.Core.Services.Abstraction;
using gView.Framework.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gView.Carto.Core.Models.Tree;

//public class TocMapNode : TocTreeNode
//{
//    private readonly ICartoApplicationScopeService _cartoScope;
//    private List<TocTreeNode> _treeNodes = new List<TocTreeNode>();

//    public TocMapNode(ICartoApplicationScopeService cartoScope)
//        : base(null)
//    {
//        _cartoScope = cartoScope;
//        _cartoScope.EventBus.OnCartoDocumentLoadedAsync += OnCartoDocumentLoadedAsync;

//        base.Text = _cartoScope.Document?.Map?.Name ?? "Map";
//        base.Icon = "basic:globe-table";
//    }

//    private Task OnCartoDocumentLoadedAsync(Abstractions.ICartoDocument arg)
//    {
//        base.Text = _cartoScope.Document?.Map?.Name ?? "Map";

//        return Rebuild();
//    }

//    public Task Rebuild()
//    {
//        this.Children = new HashSet<TocTreeNode>();

//        var map = _cartoScope.Document?.Map;

//        if (map?.TOC.Elements != null)
//        {
//            foreach (var tocElement in map.TOC.Elements)
//            {
//                TocTreeNode? childTreeNode = tocElement.ElementType switch
//                {
//                    TOCElementType.OpenedGroup => new TocParentNode(tocElement),
//                    TOCElementType.ClosedGroup => new TocParentNode(tocElement),
//                    TOCElementType.Layer => new TocLayerNode(tocElement),
//                    TOCElementType.Legend => new TocLegendNode(tocElement),
//                    _ => null,
//                };

//                if (childTreeNode != null)
//                {
//                    _treeNodes.Add(childTreeNode);
//                    if (tocElement.ParentGroup == null)
//                    {
//                        this.Children.Add(childTreeNode);
//                    }
//                    else
//                    {
//                        var parentTocTreeNode = _treeNodes.FirstOrDefault(t => t.TocElement == tocElement.ParentGroup);
//                        if (parentTocTreeNode != null)
//                        {
//                            parentTocTreeNode.Children = parentTocTreeNode.Children ?? new HashSet<TocTreeNode>();
//                            parentTocTreeNode.Children.Add(childTreeNode);
//                        }
//                    }
//                }
//            }
//        }

//        return _cartoScope.EventBus.FireRefreshContentTreeAsync();
//    }

//    public override void Dispose()
//    {
//        _cartoScope.EventBus.OnCartoDocumentLoadedAsync -= OnCartoDocumentLoadedAsync;

//        base.Dispose();
//    }
//}