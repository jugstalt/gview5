using gView.Framework.Data;
using gView.Framework.IO;
using System.Collections.Generic;

namespace gView.Framework.UI
{
    public interface ITocElement : IPersistableLoadAsync
    {
        string Name { get; set; }
        TocElementType ElementType { get; }
        List<ILayer> Layers { get; }
        void AddLayer(ILayer layer);
        void RemoveLayer(ILayer layer);
        ITocElement ParentGroup { get; }
        bool LayerVisible { get; set; }
        bool LayerLocked { get; set; }
        bool LegendVisible { get; set; }
        void OpenCloseGroup(bool open);

        IToc TOC { get; }
    }


}