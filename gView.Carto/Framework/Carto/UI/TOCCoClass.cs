using gView.Framework.Data;
using gView.Framework.Symbology;
using gView.Framework.system;
using gView.Framework.UI;
using gView.Symbology.Framework.Symbology.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gView.Framework.Carto.UI
{
    public enum TocModifier { Public, Private }
    /// <summary>
    /// Zusammenfassung für TOCCoClass.
    /// </summary>
    public class TOC : IToc
    {
        public event EventHandler TocChanged = null;

        private List<ITocElement> _elements;
        private int _pos;
        private TocModifier _modifier = TocModifier.Public;

        public IMap _map;

        public TOC(IMap map)
        {
            _elements = new List<ITocElement>();
            _pos = 0;
            _map = map;
        }
        public void Dispose()
        {
        }

        public TocModifier Modifier
        {
            get { return _modifier; }
            set { _modifier = value; }
        }

        #region ITOC Member

        public void Reset()
        {
            _pos = 0;
        }

        public gView.Framework.UI.ITocElement NextVisibleElement
        {
            get
            {
                if (_pos >= _elements.Count)
                {
                    return null;
                }

                ITocElement element = (ITocElement)_elements[_pos];
                if (element.ElementType == TocElementType.ClosedGroup)
                {
                    for (int i = _pos + 1; i < _elements.Count; i++)
                    {
                        ITocElement elemParent = ((ITocElement)_elements[i]).ParentGroup;
                        ITocElement parent = element.ParentGroup;
                        while (true)
                        {
                            if (parent == elemParent || elemParent == null)
                            {
                                _pos = i;
                                return element;
                            }
                            if (parent == null)
                            {
                                break;
                            }

                            parent = parent.ParentGroup;
                        }
                    }
                    _pos = _elements.Count;
                }
                else
                {
                    _pos++;
                }
                return element;
            }
        }

        public List<ITocElement> GroupedElements(ITocElement group)
        {
            List<ITocElement> elements = new List<ITocElement>();
            if (group == null || group.ElementType != TocElementType.OpenedGroup)
            {
                return elements;
            }

            int index = _elements.IndexOf(group);
            if (index == -1)
            {
                return elements;
            }

            while (++index < _elements.Count)
            {
                ITocElement element = _elements[index];
                if (!IsChild(group, element))
                {
                    break;
                }

                if (element.ParentGroup != group)
                {
                    continue;
                }

                elements.Add(element);
                if (element.ElementType == TocElementType.OpenedGroup)
                {
                    foreach (ITocElement childElement in GroupedElements(element))
                    {
                        elements.Add(childElement);
                    }
                }
            }
            return elements;
        }

        public List<ITocElement> Elements
        {
            get
            {
                List<ITocElement> elements = new List<ITocElement>();

                foreach (ITocElement element in _elements)
                {
                    elements.Add(element);
                }

                return elements;
            }
        }

        public Task<GraphicsEngine.Abstraction.IBitmap> Legend()
        {
            List<ITocElement> list = new List<ITocElement>();
            foreach (ITocElement element in _elements)
            {
                if (element.Layers == null)
                {
                    continue;
                }

                foreach (ILayer layer in element.Layers)
                {
                    if (layer == null)
                    {
                        continue;
                    }

                    if (_map != null && _map.Display != null)
                    {
                        if (layer.MinimumScale > 1 && layer.MinimumScale > _map.Display.MapScale)
                        {
                            continue;
                        }

                        if (layer.MaximumScale > 1 && layer.MaximumScale < _map.Display.MapScale)
                        {
                            continue;
                        }
                    }
                    if (layer.Visible)
                    {
                        list.Add(element);
                        break;
                    }
                }
            }

            return Legend(list);
        }
        async public Task<GraphicsEngine.Abstraction.IBitmap> Legend(List<ITocElement> elements)
        {
            var bitmaps = new List<GraphicsEngine.Abstraction.IBitmap>();
            GraphicsEngine.Abstraction.IBitmap legendBitmap = null;

            try
            {
                foreach (ITocElement element in elements)
                {
                    var bm = await Legend(element);
                    if (bm != null)
                    {
                        bitmaps.Add(bm);
                    }
                }

                if (bitmaps.Count == 0)
                {
                    return GraphicsEngine.Current.Engine.CreateBitmap(1, 1);
                }

                int width = 0, height = 0;
                foreach (var bm in bitmaps)
                {
                    width = (int)Math.Max(width, bm.Width);
                    height += bm.Height;
                }

                legendBitmap = GraphicsEngine.Current.Engine.CreateBitmap(width, height);
                using (var gr = legendBitmap.CreateCanvas())
                using (var whiteBrush = GraphicsEngine.Current.Engine.CreateSolidBrush(GraphicsEngine.ArgbColor.White))
                {
                    gr.FillRectangle(whiteBrush, new GraphicsEngine.CanvasRectangle(0, 0, legendBitmap.Width, legendBitmap.Height));

                    int y = 0;
                    foreach (var bm in bitmaps)
                    {
                        gr.DrawBitmap(bm, new GraphicsEngine.CanvasPoint(0, y));
                        y += bm.Height;
                    }

                    return legendBitmap;
                }
            }
            catch
            {
                if (legendBitmap != null)
                {
                    legendBitmap.Dispose();
                }

                return null;
            }
            finally
            {
                foreach (var bm in bitmaps)
                {
                    bm.Dispose();
                }
                bitmaps.Clear();
            }
        }
        async public Task<GraphicsEngine.Abstraction.IBitmap> Legend(ITocElement element)
        {
            if (element == null || element.Layers == null || !_elements.Contains(element))
            {
                return null;
            }

            GraphicsEngine.Abstraction.IBitmap previewBitmap = null;

            using (var bm = GraphicsEngine.Current.Engine.CreateBitmap(1, 1))
            using (var gr = bm.CreateCanvas())
            using (var font1 = GraphicsEngine.Current.Engine.CreateFont("Arial", 8, GraphicsEngine.FontStyle.Bold))
            using (var font2 = GraphicsEngine.Current.Engine.CreateFont("Arial", 8, GraphicsEngine.FontStyle.Bold))
            {
                try
                {

                    int height = 0, width = 130;
                    List<object> items = new List<object>();
                    foreach (ILayer layer in element.Layers)
                    {
                        if (layer is IWebServiceLayer && layer.Class is IWebServiceClass)
                        {
                            IWebServiceClass wClass = layer.Class as IWebServiceClass;
                            if (await wClass.LegendRequest(_map.Display))
                            {
                                var lBm = wClass.Legend;
                                if (lBm == null)
                                {
                                    continue;
                                }

                                height += lBm.Height;
                                items.Add(lBm);
                            }
                        }
                        else if (layer is IFeatureLayer && ((IFeatureLayer)layer).FeatureRenderer is ILegendGroup)
                        {
                            IFeatureLayer fLayer = layer as IFeatureLayer;
                            ILegendGroup lGroup = fLayer.FeatureRenderer as ILegendGroup;

                            width = (int)Math.Max(40 + gr.MeasureText(element.Name, font1).Width, width);
                            for (int i = 0; i < lGroup.LegendItemCount; i++)
                            {
                                ILegendItem lItem = lGroup.LegendItem(i);
                                if (lItem is ISymbol)
                                {
                                    height += 22;
                                    width = (int)Math.Max(40 + gr.MeasureText(lItem.LegendLabel, font1).Width, width);
                                    items.Add(lItem);
                                }
                            }
                            break;
                        }
                    }

                    if (items.Count == 1)
                    {
                        previewBitmap = GraphicsEngine.Current.Engine.CreateBitmap(width, height);
                        using (var previewCanvas = previewBitmap.CreateCanvas())
                        using (var whiteBrush = GraphicsEngine.Current.Engine.CreateSolidBrush(GraphicsEngine.ArgbColor.White))
                        using (var blackBrush = GraphicsEngine.Current.Engine.CreateSolidBrush(GraphicsEngine.ArgbColor.Black))
                        {
                            previewCanvas.FillRectangle(whiteBrush, new GraphicsEngine.CanvasRectangle(0, 0, bm.Width, bm.Height));

                            if (items[0] is GraphicsEngine.Abstraction.IBitmap)
                            {
                                var lBm = (GraphicsEngine.Abstraction.IBitmap)items[0];
                                previewCanvas.DrawBitmap(lBm, new GraphicsEngine.CanvasPoint(0, 0));
                                lBm.Dispose();
                            }
                            else if (items[0] is ILegendItem)
                            {
                                ISymbol symbol = items[0] as ISymbol;
                                new SymbolPreview(_map).Draw(
                                    previewCanvas,
                                    new GraphicsEngine.CanvasRectangle(2, 1, 30, 20),
                                    symbol);
                                previewCanvas.DrawText(element.Name, font1, blackBrush, 32, 3);
                            }
                        }
                    }
                    else if (items.Count > 1)
                    {
                        previewBitmap = GraphicsEngine.Current.Engine.CreateBitmap(width, height + 15);
                        using (var previewCanvas = previewBitmap.CreateCanvas())
                        using (var whiteBrush = GraphicsEngine.Current.Engine.CreateSolidBrush(GraphicsEngine.ArgbColor.White))
                        using (var blackBrush = GraphicsEngine.Current.Engine.CreateSolidBrush(GraphicsEngine.ArgbColor.Black))
                        {
                            previewCanvas.FillRectangle(whiteBrush, new GraphicsEngine.CanvasRectangle(0, 0, bm.Width, bm.Height));

                            int y = 12;
                            foreach (object item in items)
                            {
                                if (item is GraphicsEngine.Abstraction.IBitmap)
                                {
                                    var lBm = (GraphicsEngine.Abstraction.IBitmap)item;
                                    previewCanvas.DrawBitmap(lBm, new GraphicsEngine.CanvasPoint(0, y));
                                    y += lBm.Height;
                                    lBm.Dispose();
                                }
                                else if (item is ILegendItem)
                                {
                                    ISymbol symbol = item as ISymbol;
                                    new SymbolPreview(_map).Draw(
                                        previewCanvas,
                                        new GraphicsEngine.CanvasRectangle(4, 1 + y, 30, 20),
                                        symbol);
                                    previewCanvas.DrawText(((ILegendItem)item).LegendLabel, font2, blackBrush, 34, y + 3);
                                    y += 22;
                                }
                            }
                            previewCanvas.DrawText(element.Name, font1, blackBrush, 2, 2);
                        }
                    }
                }
                catch
                {
                    if (previewBitmap != null)
                    {
                        previewBitmap.Dispose();
                    }
                }
            }

            return previewBitmap;
        }
        async public Task<TocLegendItems> LegendSymbol(ITocElement element)
        {
            var items = new List<TocLegendItem>();
            if (element == null || element.Layers == null || !_elements.Contains(element))
            {
                return null;
            }

            try
            {
                foreach (ILayer layer in element.Layers)
                {
                    if (layer is IWebServiceLayer && layer.Class is IWebServiceClass)
                    {
                        IWebServiceClass wClass = layer.Class as IWebServiceClass;
                        if (await wClass.LegendRequest(_map.Display))
                        {
                            var lBm = wClass.Legend;

                            if (lBm == null)
                            {
                                continue;
                            }

                            items.Add(new TocLegendItem()
                            {
                                Image = lBm
                            });
                        }
                    }
                    else if (layer is IFeatureLayer && ((IFeatureLayer)layer).FeatureRenderer is ILegendGroup)
                    {
                        IFeatureLayer fLayer = layer as IFeatureLayer;
                        ILegendGroup lGroup = fLayer.FeatureRenderer as ILegendGroup;

                        for (int i = 0; i < lGroup.LegendItemCount; i++)
                        {
                            ILegendItem lItem = lGroup.LegendItem(i);
                            if (lItem is ISymbol)
                            {
                                var bm = GraphicsEngine.Current.Engine.CreateBitmap(20, 20);
                                using (var canvas = bm.CreateCanvas())
                                {
                                    ISymbol symbol = lItem as ISymbol;
                                    new SymbolPreview(_map).Draw(
                                        canvas,
                                        new GraphicsEngine.CanvasRectangle().ToLegendItemSymbolRect(),
                                        symbol);
                                }
                                items.Add(new TocLegendItem()
                                {
                                    Image = bm,
                                    Label = lItem.LegendLabel
                                });
                            }
                        }
                        break;
                    }
                }
            }
            catch
            {

            }

            return new TocLegendItems()
            {
                Items = items
            };
        }

        public void Add2Group(ITocElement element, ITocElement Group)
        {
            if (element == null)
            {
                return;
            }

            if (_elements.IndexOf(element) == -1)
            {
                return;
            }

            if (element == Group)
            {
                return;
            }

            if (Group == null || Group.Layers.Count != 1 ||
                !(Group.Layers[0] is GroupLayer))
            {
                return;
            }

            ITocElement pElement = element.ParentGroup;
            IGroupLayer pLayer = null;
            if (pElement != null && pElement.Layers.Count == 1 &&
                (pElement.Layers[0] is GroupLayer))
            {
                pLayer = pElement.Layers[0] as IGroupLayer;
            }


            IGroupLayer gLayer = Group.Layers[0] as IGroupLayer;

            if (element.ElementType == TocElementType.Layer)
            {
                _elements.Remove(element);
                _elements.Insert(this.lastGroupItemIndex(Group) + 1, element);
            }
            else if (element.ElementType == TocElementType.OpenedGroup ||
                element.ElementType == TocElementType.ClosedGroup)
            {
                // Zirkulationen vermeiden
                ITocElement parent = Group;
                while (parent != null)
                {
                    if (parent.ParentGroup == element)
                    {
                        ((TocElement)parent).ParentGroup = element.ParentGroup;
                    }

                    parent = parent.ParentGroup;
                }

                _elements.Remove(element);
                _elements.Insert(this.lastGroupItemIndex(Group) + 1, element);

                MoveGroup(element);
            }

            ((TocElement)element).ParentGroup = Group;
            foreach (ILayer layer in element.Layers)
            {
                if (pLayer is GroupLayer)
                {
                    ((GroupLayer)pLayer).Remove(layer as Layer);
                }
                if (gLayer is GroupLayer)
                {
                    ((GroupLayer)gLayer).Add(layer as Layer);
                }
            }

            if (TocChanged != null)
            {
                TocChanged(this, new EventArgs());
            }
        }

        public ITocElement GetTOCElement(string name, ITocElement parent)
        {
            foreach (ITocElement element in _elements)
            {
                if (element.Name == name && element.ParentGroup == parent)
                {
                    return element;
                }
            }
            return null;
        }

        public ITocElement GetTOCElement(ILayer layer)
        {
            foreach (ITocElement tocElement in _elements)
            {
                if (tocElement.Layers == null)
                {
                    continue;
                }

                foreach (ILayer e in tocElement.Layers)
                {
                    if (e == layer)
                    {
                        return tocElement;
                    }
                }
            }
            return null;
        }

        public ITocElement GetTocElementByLayerId(int layerId)
        {
            foreach (ITocElement tocElement in _elements)
            {
                if (tocElement.Layers == null)
                {
                    continue;
                }

                foreach (ILayer e in tocElement.Layers)
                {
                    if (e.ID == layerId)
                    {
                        return tocElement;
                    }
                }
            }
            return null;
        }

        public ITocElement GetTOCElement(IClass Class)
        {
            if (Class == null)
            {
                return null;
            }

            foreach (ITocElement tocElement in _elements)
            {
                if (tocElement.Layers == null)
                {
                    continue;
                }

                foreach (ILayer e in tocElement.Layers)
                {
                    if (e == null)
                    {
                        continue;
                    }

                    if (e.Class == Class)
                    {
                        return tocElement;
                    }
                }
            }
            return null;
        }

        public List<ITocElement> GroupElements
        {
            get
            {
                List<ITocElement> e = new List<ITocElement>();
                foreach (ITocElement elem in _elements)
                {
                    if (elem.ElementType == TocElementType.ClosedGroup ||
                        elem.ElementType == TocElementType.OpenedGroup)
                    {
                        e.Add(elem);
                    }
                }
                return e;
            }
        }

        public void RenameElement(ITocElement element, string newName)
        {
            if (element.ElementType == TocElementType.Layer)
            {
                if (_elements.IndexOf(element) == -1)
                {
                    return;
                }

                // Zwei Layer mit selben Namen und gleicher ParentGroup
                // vereinigen...
                foreach (ITocElement e in _elements)
                {
                    if (e == element)
                    {
                        continue;
                    }

                    if (e.Name == newName && e.ParentGroup == element.ParentGroup)
                    {
                        if (e.ElementType != TocElementType.Layer)
                        {
                            return;  // Nur mit Layer vereinigbar...
                        }

                        foreach (ILayer layer in element.Layers)
                        {
                            ((TocElement)e).LayersList.Add(layer);
                        }
                        _elements.Remove(element);
                        return;
                    }
                }
                ((TocElement)element).rename(newName);
            }
            else if (element.ElementType == TocElementType.OpenedGroup ||
                     element.ElementType == TocElementType.ClosedGroup)
            {
                // Zwei Gruppen mit selben Namen und gleicher ParentGroup
                // vereinigen...
                foreach (ITocElement e in _elements)
                {
                    if (e == element)
                    {
                        continue;
                    }

                    if (e.ElementType != TocElementType.ClosedGroup &&
                        e.ElementType != TocElementType.OpenedGroup)
                    {
                        continue;
                    }

                    if (e.ParentGroup == element.ParentGroup &&
                        e.Name == newName)
                    {
                        foreach (ITocElement e2 in ListOperations<ITocElement>.Clone(_elements))
                        {
                            if (e2.ParentGroup == element)
                            {
                                ((TocElement)e2).ParentGroup = e;
                                this.RenameElement(e2, e2.Name);
                            }
                        }
                        _elements.Remove(element);
                        this.MoveGroup(e);

                        // Groupelement können auch Layer enthalten (zB WebserviceLayer...)
                        foreach (ILayer layer in element.Layers)
                        {
                            ((TocElement)e).LayersList.Add(layer);
                        }
                        break;
                    }
                }
                if (_elements.IndexOf(element) == -1)
                {
                    return;
                } ((TocElement)element).rename(newName);
            }

            if (TocChanged != null)
            {
                TocChanged(this, new EventArgs());
            }
        }

        public void MoveElement(ITocElement element, ITocElement insertBefore, bool insertAfter)
        {
            if (element == insertBefore)
            {
                return;
            }

            if (_elements.IndexOf(element) == -1 ||
                _elements.IndexOf(insertBefore) == -1)
            {
                return;
            }

            int sum = (insertAfter) ? 1 : 0;

            if (element.ElementType == TocElementType.Layer)
            {
                _elements.Remove(element);
                ((TocElement)element).ParentGroup = insertBefore.ParentGroup;
                _elements.Insert(_elements.IndexOf(insertBefore) + sum, element);
            }
            else if (element.ElementType == TocElementType.OpenedGroup ||
                    element.ElementType == TocElementType.ClosedGroup)
            {
                _elements.Remove(element);
                ((TocElement)element).ParentGroup = insertBefore.ParentGroup;
                _elements.Insert(_elements.IndexOf(insertBefore) + sum, element);
                this.MoveGroup(element);
            }

            if (TocChanged != null)
            {
                TocChanged(this, new EventArgs());
            }
        }

        public int CountGroupLayers(ITocElement Group, bool subGroups)
        {
            if (Group.ElementType != TocElementType.OpenedGroup &&
                Group.ElementType != TocElementType.ClosedGroup)
            {
                return 0;
            }

            if (_elements.IndexOf(Group) == -1)
            {
                return 0;
            }

            int count = 0;
            foreach (ITocElement element in _elements)
            {
                if (element.ParentGroup == Group)
                {
                    if (element.ElementType == TocElementType.Layer)
                    {
                        count++;
                    }

                    if ((element.ElementType == TocElementType.ClosedGroup ||
                        element.ElementType == TocElementType.OpenedGroup) &&
                        subGroups)
                    {
                        count += CountGroupLayers(element, true);
                    }
                }
            }
            return count;
        }
        public int CountVisibleGroupLayers(ITocElement Group, bool subGroups)
        {
            if (Group.ElementType != TocElementType.OpenedGroup &&
                Group.ElementType != TocElementType.ClosedGroup)
            {
                return 0;
            }

            if (_elements.IndexOf(Group) == -1)
            {
                return 0;
            }

            int count = 0;
            foreach (ITocElement element in _elements)
            {
                if (element.ParentGroup == Group)
                {
                    if (element.ElementType == TocElementType.Layer)
                    {
                        if (element.LayerVisible)
                        {
                            count++;
                        }
                    }
                    if ((element.ElementType == TocElementType.ClosedGroup ||
                        element.ElementType == TocElementType.OpenedGroup) &&
                        subGroups)
                    {
                        count += CountVisibleGroupLayers(element, true);
                    }
                }
            }
            return count;
        }
        public void SplitMultiLayer(ITocElement element)
        {
            int index = _elements.IndexOf(element);
            if (index == -1)
            {
                return;
            }

            if (element.ElementType != TocElementType.Layer)
            {
                return;
            }

            for (int i = ((TocElement)element).LayersList.Count - 1; i > 0; i--)
            {
                ILayer layer = (ILayer)((TocElement)element).LayersList[i];
                ((TocElement)element).LayersList.Remove(layer);

                AddLayer(layer, element.ParentGroup, ++index);
            }
        }
        #endregion

        public void AddLayer(ILayer layer, ITocElement parent)
        {
            AddLayer(layer, parent, -1);
        }
        internal void AddLayer(ILayer layer, ITocElement parent, int pos)
        {
            int c = 1;
            string alias = layer.Title, alias2 = alias;

            while (GetTOCElement(alias2, parent) != null)
            {
                alias2 = alias + "_" + c.ToString();
                c++;
            }
            alias = alias2;

            InsertLayer(pos, layer, alias, parent);
        }
        public void RemoveLayer(ILayer layer)
        {
            foreach (ITocElement element in ListOperations<ITocElement>.Clone(_elements))
            {
                foreach (ILayer l in ListOperations<ILayer>.Clone(element.Layers))
                {
                    if (l == layer)
                    {
                        element.RemoveLayer(l);
                        if (element.Layers.Count == 0)
                        {
                            _elements.Remove(element);
                            if (TocChanged != null)
                            {
                                TocChanged(this, new EventArgs());
                            }

                            break;
                        }
                    }
                }
            }
        }
        private void InsertLayer(int pos, ILayer layer, string alias, ITocElement parent)
        {
            TocElement element =
                (layer is IWebServiceLayer) ?
                new TocElement(layer, alias, parent, this, TocElementType.OpenedGroup) :
                new TocElement(layer, alias, parent, this, (layer is IGroupLayer) ? TocElementType.OpenedGroup : TocElementType.Layer);

            if (pos < 0 || pos >= _elements.Count)
            {
                _elements.Add(element);
            }
            else
            {
                _elements.Insert(pos, element);
            }

            if (layer is IWebServiceLayer && layer.Class is IWebServiceClass && ((IWebServiceClass)layer.Class).Themes != null)
            {
                IWebServiceClass wc = layer.Class as IWebServiceClass;
                foreach (IWebServiceTheme theme in wc.Themes)
                {
                    theme.DatasetID = layer.DatasetID;
                    if (theme.Locked)
                    {
                        continue;
                    }

                    InsertLayer(pos > 0 ? pos + 1 : -1, theme, theme.Title, element);
                }
            }

            if (TocChanged != null)
            {
                TocChanged(this, new EventArgs());
            }
        }

        private int lastGroupItemIndex(ITocElement parent)
        {
            int index = 0, i = 0;
            foreach (ITocElement elem in _elements)
            {
                if (elem == parent)
                {
                    index = i;
                }
                else if (elem.ParentGroup == parent && elem.ElementType == TocElementType.Layer)
                {
                    index = i;
                }

                i++;
            }
            return index;
        }
        private void MoveGroup(ITocElement Group)
        {
            List<ITocElement> collect = new List<ITocElement>();
            foreach (ITocElement elem in _elements)
            {
                if (elem.ParentGroup == Group)
                {
                    collect.Add(elem);
                }
            }
            foreach (ITocElement elem in collect)
            {
                _elements.Remove(elem);
            }
            int index = _elements.IndexOf(Group) + 1;
            foreach (ITocElement elem in collect)
            {
                _elements.Insert(index++, elem);
                if (elem.ElementType == TocElementType.ClosedGroup ||
                    elem.ElementType == TocElementType.OpenedGroup)
                {
                    MoveGroup(elem);
                }
            }

            if (TocChanged != null)
            {
                TocChanged(this, new EventArgs());
            }
        }

        public void SetGroupVisibility(ITocElement Group, bool visible)
        {
            foreach (ITocElement element in _elements)
            {
                if (element.ParentGroup == Group)
                {
                    if (element.ElementType == TocElementType.Layer ||
                        element.ElementType == TocElementType.ClosedGroup ||
                        element.ElementType == TocElementType.OpenedGroup)

                    {
                        element.LayerVisible = visible;
                    }
                }
            }
        }

        public List<ILayer> VisibleLayers
        {
            get
            {
                List<ILayer> layers = new List<ILayer>();

                foreach (ITocElement tocElement in _elements)
                {
                    if (tocElement == null || tocElement.ElementType != TocElementType.Layer ||
                        tocElement.Layers == null)
                    {
                        continue;
                    }

                    //if(!tocElement.LayerVisible) continue;

                    if (this.Modifier == TocModifier.Public)
                    {
                        foreach (ILayer layer in tocElement.Layers)
                        {
                            if (layer == null || !layer.Visible)
                            {
                                continue;
                            }

                            layers.Add(layer);
                        }
                    }
                    else
                    {
                        ITocElement e = tocElement;
                        bool visible = e.LayerVisible;
                        while (visible && e.ParentGroup != null)
                        {
                            visible = e.ParentGroup.LayerVisible;
                            e = e.ParentGroup;
                        }
                        if (visible)
                        {
                            foreach (ILayer layer in tocElement.Layers)
                            {
                                layers.Add(layer);
                            }
                        }
                    }
                }

                return layers;
            }
        }

        public List<IWebServiceLayer> VisibleWebServiceLayers
        {
            get
            {
                List<IWebServiceLayer> layers = new List<IWebServiceLayer>();

                foreach (ITocElement tocElement in _elements)
                {
                    if (tocElement == null || tocElement.ElementType == TocElementType.Layer ||
                        tocElement.Layers == null)
                    {
                        continue;
                    }

                    if (this.Modifier == TocModifier.Public)
                    {
                        foreach (ILayer layer in tocElement.Layers)
                        {
                            if (!(layer is IWebServiceLayer) || !layer.Visible)
                            {
                                continue;
                            }

                            layers.Add(layer as IWebServiceLayer);
                        }
                    }
                    else
                    {
                        ITocElement e = tocElement;
                        bool visible = e.LayerVisible;
                        while (visible && e.ParentGroup != null)
                        {
                            visible = e.ParentGroup.LayerVisible;
                            e = e.ParentGroup;
                        }
                        if (visible)
                        {
                            foreach (ILayer layer in tocElement.Layers)
                            {
                                if (!(layer is IWebServiceLayer))
                                {
                                    continue;
                                }

                                layers.Add(layer as IWebServiceLayer);
                            }
                        }
                    }
                }

                return layers;
            }
        }

        public List<ILayer> Layers
        {
            get
            {
                List<ILayer> layers = new List<ILayer>();

                foreach (ITocElement tocElement in _elements)
                {
                    if (tocElement.ElementType != TocElementType.Layer)
                    {
                        continue;
                    }

                    foreach (ILayer layer in tocElement.Layers)
                    {
                        layers.Add(layer);
                    }
                }

                return layers;
            }
        }

        public void RemoveAllElements()
        {
            _elements.Clear();
            if (TocChanged != null)
            {
                TocChanged(this, new EventArgs());
            }
        }

        private bool IsChild(ITocElement parent, ITocElement child)
        {
            if (child == null)
            {
                return false;
            }

            if (parent == null)
            {
                return true; // null ist immer Parent
            }

            while (child.ParentGroup != null)
            {
                if (child.ParentGroup == parent)
                {
                    return true;
                }

                child = child.ParentGroup;
            }
            return false;
        }

        #region IPersistable Member

        async public Task<bool> LoadAsync(gView.Framework.IO.IPersistStream stream)
        {
            _elements.Clear();

            TocElement element = null;
            while ((element = (TocElement)await stream.LoadAsync("ITOCElement", new TocElement(this))) != null)
            {
                _elements.Add(element);
            }

            ReorderElements();

            return true;
        }

        public void Save(gView.Framework.IO.IPersistStream stream)
        {
            foreach (TocElement element in _elements)
            {
                stream.Save("ITOCElement", element);
            }
        }

        #endregion

        #region IClone
        public object Clone()
        {
            return Clone(_map);
        }
        #endregion

        #region IClone3 Member

        public object Clone(IMap map)
        {
            TOC toc = new TOC(map);

            foreach (ITocElement element in _elements)
            {
                int parentIndex = -1;
                if (element.ParentGroup != null)
                {
                    parentIndex = _elements.IndexOf(element.ParentGroup);
                    if (parentIndex >= toc._elements.Count)
                    {
                        parentIndex = -1;
                    }
                }

                toc._elements.Add(((TocElement)element).Copy(toc, (parentIndex == -1) ? null : toc._elements[parentIndex]));
            }
            return toc;
        }

        #endregion

        #region Helper

        private void ReorderElements()
        {
            List<ITocElement> elements = new List<ITocElement>();

            foreach (var element in _elements)
            {
                if (element.ParentGroup == null)
                {
                    elements.Add(element);
                    elements.AddRange(GetChildElementsRecursive(element));
                }
            }

            if (elements.Count == _elements.Count)
            {
                _elements = elements;
            }
        }

        private IEnumerable<ITocElement> GetChildElementsRecursive(ITocElement parentElement)
        {
            List<ITocElement> childElements = new List<ITocElement>();

            foreach (var childElement in _elements.Where(e => e.ParentGroup == parentElement))
            {
                childElements.Add(childElement);
                childElements.AddRange(GetChildElementsRecursive(childElement));
            }

            return childElements;
        }

        #endregion
    }

    internal class TocElement : gView.Framework.UI.ITocElement
    {
        private string _name;
        private TocElementType _type;
        ITocElement _parent;
        List<IDatasetElement> _layers = new List<IDatasetElement>();
        private TOC _toc;
        private bool _showLegend = true;
        private bool _visible = false;
        private bool _locked = false;

        public TocElement(TOC parentTOC)
        {
            _toc = parentTOC;
        }
        public TocElement(ILayer layer, string name, ITocElement parent, TOC parentTOC)
        {
            _layers.Add(layer);
            _name = name;
            _type = TocElementType.Layer;
            _parent = parent;
            _toc = parentTOC;
            if (layer is ILayer)
            {
                _visible = ((ILayer)layer).Visible;
            }
        }
        public TocElement(ILayer layer, string name, ITocElement parent, TOC parentTOC, TocElementType type)
            : this(layer, name, parent, parentTOC)
        {
            _type = type;
        }
        public TocElement(string name, TOC parentTOC)
        {
            _name = name;
            _type = TocElementType.Layer;
            _parent = null;
            _toc = parentTOC;
        }
        public TocElement(string name, ITocElement parent, TOC parentTOC)
        {
            _name = name;
            _type = TocElementType.Layer;
            _parent = parent;
            _toc = parentTOC;
        }
        public TocElement(string name, ITocElement parent, TocElementType type, TOC parentTOC)
        {
            _name = name;
            _type = type;
            _parent = parent;
            _toc = parentTOC;
        }

        internal TocElement Copy(TOC toc, ITocElement parent)
        {
            TocElement elem = new TocElement(toc);
            elem._name = _name;
            elem._type = _type;
            elem._parent = parent;
            elem._layers = ListOperations<IDatasetElement>.Clone(_layers);
            elem._showLegend = _showLegend;
            elem._visible = LayerVisible;
            elem._locked = _locked;

            return elem;
        }

        private static string RecursiveName(TocElement element)
        {
            string name = "";
            RecursiveName(element, ref name);
            return name;
        }
        private static void RecursiveName(TocElement element, ref string name)
        {
            if (element == null)
            {
                return;
            }

            name = ((name != "") ? element._name + "|" + name : element._name);
            RecursiveName(element.ParentGroup as TocElement, ref name);
        }

        internal static bool layerVisible(ILayer layer)
        {
            if (layer is Layer)
            {
                // Sollte nicht Recursive über Grouplayer bestimmt werden
                IGroupLayer gLayer = layer.GroupLayer;
                ((Layer)layer).GroupLayer = null;
                bool visible = layer.Visible;
                ((Layer)layer).GroupLayer = gLayer;

                return visible;
            }
            else if (layer != null)
            {
                return layer.Visible;
            }
            return false;
        }
        #region ITOCElement Member

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value == null || value == "" || _toc == null)
                {
                    return;
                }

                _toc.RenameElement(this, value);
            }
        }

        public gView.Framework.UI.TocElementType ElementType
        {
            get
            {
                return _type;
            }
        }

        public List<ILayer> Layers
        {
            get
            {
                List<ILayer> e = new List<ILayer>();
                foreach (ILayer layer in _layers)
                {
                    e.Add(layer);
                }
                return e;
            }
        }
        public void RemoveLayer(ILayer layer)
        {
            if (!_layers.Contains(layer))
            {
                return;
            }

            _layers.Remove(layer);
        }
        public void AddLayer(ILayer layer)
        {
            if (_layers.Contains(layer))
            {
                return;
            }

            _layers.Add(layer);
        }
        public ITocElement ParentGroup
        {
            get
            {
                return _parent;
            }
            set
            {
                if (_parent == value)
                {
                    return;
                }

                _parent = value;

                if (_layers != null)
                {
                    foreach (ILayer layer in _layers)
                    {
                        if (!(layer is Layer))
                        {
                            continue;
                        }

                        if (layer.GroupLayer is GroupLayer)
                        {
                            ((GroupLayer)layer.GroupLayer).Remove(layer as Layer);
                        }

                        ((Layer)layer).GroupLayer =
                            (_parent != null && _parent.Layers.Count == 1) ? _parent.Layers[0] as IGroupLayer : null;
                    }
                }
            }
        }

        public bool LayerVisible
        {
            get
            {
                if (_toc.Modifier == TocModifier.Public)
                {
                    if (_type == TocElementType.Layer)
                    {
                        foreach (ILayer layer in _layers)
                        {
                            if (layer == null)
                            {
                                continue;
                            }

                            if (TocElement.layerVisible(layer))
                            {
                                return _visible = true;
                            }
                        }
                        return _visible = false;
                    }
                    if (_type == TocElementType.OpenedGroup ||
                        _type == TocElementType.ClosedGroup)
                    {
                        //return _visible = (_toc.CountVisibleGroupLayers(this, true) > 0);
                        if (_layers.Count == 1 && _layers[0] is IGroupLayer)
                        {
                            return _visible = TocElement.layerVisible(_layers[0] as ILayer);
                        }
                        else if (_layers.Count == 1 && _layers[0] is IWebServiceLayer)
                        {
                            return _visible = TocElement.layerVisible(_layers[0] as ILayer);
                        }
                    }
                    return _visible = false;
                }
                else
                {
                    return _visible;
                }
            }
            set
            {
                if (_type == TocElementType.Layer)
                {
                    if (_toc.Modifier == TocModifier.Public)
                    {
                        foreach (ILayer layer in _layers)
                        {
                            if (layer == null)
                            {
                                continue;
                            }

                            layer.Visible = _visible = value;
                        }
                    }
                    else
                    {
                        _visible = value;
                    }
                }
                if (_type == TocElementType.OpenedGroup ||
                    _type == TocElementType.ClosedGroup)
                {
                    if (_toc.Modifier == TocModifier.Public)
                    {
                        if (_layers.Count == 1 && _layers[0] is IGroupLayer)
                        {
                            _visible = ((IGroupLayer)_layers[0]).Visible = value;
                        }
                        else if (_layers.Count == 1 && _layers[0] is IWebServiceLayer)
                        {
                            _visible = ((IWebServiceLayer)_layers[0]).Visible = value;
                        }
                    }
                    else
                    {
                        _visible = value;
                    }
                    //if(_toc is TOC) 
                    //{
                    //    ((TOC)_toc).SetGroupVisibility(this,value);
                    //}
                }
            }
        }

        public bool LayerLocked
        {
            get { return _locked; }
            set { _locked = value; }
        }
        public bool LegendVisible
        {
            get { return _showLegend; }
            set { _showLegend = value; }
        }

        public void OpenCloseGroup(bool open)
        {
            if (_type == TocElementType.OpenedGroup ||
                _type == TocElementType.ClosedGroup)
            {
                if (open)
                {
                    _type = TocElementType.OpenedGroup;
                }
                else
                {
                    _type = TocElementType.ClosedGroup;
                }
            }
        }

        public IToc TOC
        {
            get { return _toc; }
        }
        #endregion

        public void rename(string newName)
        {
            _name = newName;
            foreach (ILayer layer in _layers)
            {
                if (layer is IGroupLayer)
                {
                    ((IGroupLayer)layer).Title = newName;
                }
            }
        }
        public List<IDatasetElement> LayersList
        {
            get { return _layers; }
        }

        #region IPersistable Member

        async public Task<bool> LoadAsync(gView.Framework.IO.IPersistStream stream)
        {
            _name = (string)stream.Load("Name");
            _type = (TocElementType)stream.Load("Type");
            _showLegend = (bool)stream.Load("legend", false);
            _locked = (bool)stream.Load("locked", false);

            if ((_type == TocElementType.ClosedGroup || _type == TocElementType.OpenedGroup) &&
                _name.IndexOf("|") != -1)
            {
                int pos = _name.LastIndexOf("|");
                _name = _name.Substring(pos + 1, _name.Length - pos - 1);
            }

            if (_toc.Modifier == TocModifier.Private)
            {
                _visible = (bool)stream.Load("visible", false);
            }

            string parentName = (string)stream.Load("Parent");
            if (parentName != null)
            {
                foreach (ITocElement group in _toc.GroupElements)
                {
                    if (RecursiveName(group as TocElement) == parentName)
                    {
                        _parent = group;
                        break;
                    }
                }
            }

            _layers.Clear();
            if (_type == TocElementType.ClosedGroup || _type == TocElementType.OpenedGroup)
            {
                PersistLayer pElement = null;

                pElement = (PersistLayer)await stream.LoadAsync("DatasetElement", new PersistLayer(_toc._map));

                if (pElement != null && pElement.DatasetElement != null)
                {
                    ILayer gLayer = null;
                    foreach (IDatasetElement dsElement in _toc._map.MapElements)
                    {
                        if (dsElement is IGroupLayer &&
                            dsElement.ID == pElement.DatasetElement.ID/*RecursiveName(this)*/)
                        {
                            gLayer = dsElement as IGroupLayer;
                            break;
                        }
                    }

                    if (gLayer == null)
                    {
                        // wenn Gruppe ein WebServiceLayer ist....
                        gLayer = pElement.DatasetElement as ILayer;

                        //
                        // Für alte Versionen: Suchen, ob Grouplayer in Karte vorhanden
                        // Wenn nein: einfügen
                        //
                        if (gLayer == null)
                        {
                            _toc._map.AddLayer(gLayer = new GroupLayer(RecursiveName(this)));
                        }
                    }

                    if (gLayer != null)
                    {
                        _layers.Add(gLayer);
                        if (_parent != null && _parent.Layers.Count == 1 && _parent.Layers[0] is GroupLayer)
                        {
                            ((GroupLayer)_parent.Layers[0]).Add(gLayer as Layer);
                        }
                    }
                }
            }
            else
            {
                PersistLayer pElement = null;

                while ((pElement = (PersistLayer)await stream.LoadAsync("DatasetElement", new PersistLayer(_toc._map))) != null)
                {
                    _layers.Add(pElement.DatasetElement);  // DatasetElement kann auch null sein, wenn (vorübergehend) nicht mehr im Dataset...
                    if (_parent != null && _parent.Layers.Count == 1 && _parent.Layers[0] is GroupLayer)
                    {
                        ((GroupLayer)_parent.Layers[0]).Add(pElement.DatasetElement as Layer);
                    }
                }
            }

            return true;
        }

        public void Save(gView.Framework.IO.IPersistStream stream)
        {
            stream.Save("Name", (_type == TocElementType.OpenedGroup || _type == TocElementType.ClosedGroup) ? RecursiveName(this) : _name);
            stream.Save("Type", (int)_type);
            stream.Save("legend", _showLegend);
            stream.Save("locked", _locked);

            if (_toc.Modifier == TocModifier.Private)
            {
                stream.Save("visible", _visible);
            }
            if (_parent != null)
            {
                stream.Save("Parent", RecursiveName(_parent as TocElement));
            }
            foreach (ILayer layer in _layers)
            {
                PersistLayer pElement = new PersistLayer(_toc._map, layer);
                stream.Save("DatasetElement", pElement);
            }
        }

        #endregion
    }

    internal class PersistLayer : gView.Framework.IO.IPersistableLoadAsync
    {
        private IMap _map = null;
        private IDatasetElement _element = new NullLayer();

        public PersistLayer(IMap map)
        {
            _map = map;
        }
        public PersistLayer(IMap map, IDatasetElement element) : this(map)
        {
            _element = element;
        }

        public IDatasetElement DatasetElement
        {
            get { return _element; }
        }

        #region IPersistable Member

        async public Task<bool> LoadAsync(gView.Framework.IO.IPersistStream stream)
        {
            if (_map == null)
            {
                return true;
            }

            int datasetIndex = (int)stream.Load("DatasetIndex", -1);

            IDataset dataset = _map[datasetIndex];
            // dataset ist bei Grouplayern immer null, darum kein abbruch
            //if(dataset==null) return;

            bool isWebTheme = (bool)stream.Load("IsWebTheme", false);

            string webThemeId = String.Empty;
            string webClassName = String.Empty;
            if (isWebTheme && dataset != null)
            {
                webThemeId = (string)stream.Load("ID", "");
                webClassName = (string)stream.Load("ClassName", "");

                IDatasetElement wElement = await dataset.Element(webClassName);
                if (wElement == null || !(wElement.Class is IWebServiceClass))
                {
                    return true;
                }

                IWebServiceClass wc = wElement.Class as IWebServiceClass;
                if (wc == null || wc.Themes == null)
                {
                    return true;
                }

                foreach (IWebServiceTheme theme in wc.Themes)
                {
                    if (theme.LayerID == webThemeId)
                    {
                        _element = theme;
                        return true;
                    }
                }

                return true;
            }

            string name = (string)stream.Load("Name", "");
            int _id_ = (int)stream.Load("_ID_", -1);

            if (_id_ == -1)  // Old Version
            {
                foreach (IDatasetElement layer in _map.MapElements)
                {
                    if (layer.Class != null && layer.Class.Dataset == dataset && layer.Title == name)
                    {
                        _element = layer;
                        return true;
                    }
                }
            }
            else
            {
                foreach (IDatasetElement layer in _map.MapElements)
                {
                    // Grouplayer
                    if (dataset == null &&
                        layer is IGroupLayer &&
                        layer.ID == _id_ &&
                        layer.Title == name)
                    {
                        _element = layer;
                        return true;
                    }
                    // Layer
                    if (layer.Class != null && layer.Class.Dataset == dataset &&
                        layer.Title == name && layer.ID == _id_)
                    {
                        _element = layer;
                        return true;
                    }
                }
            }

            // für ein späters speichern des projektes die werte merken
            if (_element is NullLayer)
            {
                ((NullLayer)_element).PersistLayerID = _id_;
                ((NullLayer)_element).PersistDatasetID = datasetIndex;
                ((NullLayer)_element).PersistIsWebTheme = isWebTheme;
                ((NullLayer)_element).PersistWebThemeID = webThemeId;
                ((NullLayer)_element).PersistClassName = webClassName;
                ((NullLayer)_element).Title = name;
            }

            return true;
        }

        public void Save(gView.Framework.IO.IPersistStream stream)
        {
            if (_element == null || _map == null)
            {
                return;
            }

            if (_element is NullLayer)
            {
                NullLayer nLayer = (NullLayer)_element;

                stream.Save("DatasetIndex", nLayer.PersistDatasetID);

                if (nLayer.PersistIsWebTheme)
                {
                    stream.Save("ID", nLayer.PersistWebThemeID);
                    stream.Save("IsWebTheme", true);
                    stream.Save("ClassName", nLayer.PersistClassName);
                }
                else
                {
                    stream.Save("_ID_", nLayer.PersistLayerID);
                    stream.Save("Name", nLayer.Title);
                }
            }
            else
            {
                //IDataset dataset = _map[_element.DatasetID];

                stream.Save("DatasetIndex", _element.DatasetID);

                if (_element is IWebServiceTheme)
                {
                    stream.Save("ID", ((IWebServiceTheme)_element).LayerID);
                    stream.Save("IsWebTheme", true);

                    //if (dataset.Elements.Count==1 && dataset.Elements[0].Class != null)
                    if (((IWebServiceTheme)_element).ServiceClass != null)
                    {
                        //stream.Save("ClassName", dataset.Elements[0].Class.Name);
                        stream.Save("ClassName", ((IWebServiceTheme)_element).ServiceClass.Name);
                    }
                }
                else
                {
                    stream.Save("_ID_", _element.ID);
                    stream.Save("Name", _element.Title);
                }
            }

            return;
        }

        #endregion
    }
}
