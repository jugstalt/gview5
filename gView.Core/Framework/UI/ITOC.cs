using gView.Framework.Data;
using gView.Framework.IO;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Framework.UI
{
    public interface IToc : IClone, IClone3, IPersistableLoadAsync
    {
        event EventHandler TocChanged;

        void Reset();

        ITocElement NextVisibleElement { get; }

        List<ITocElement> GroupedElements(ITocElement group);

        /*
		void AddGroup(string GroupName,ITOCElement parent);
         * */
        void Add2Group(ITocElement element, ITocElement Group);

        ITocElement GetTOCElement(string name, ITocElement parent);
        ITocElement GetTOCElement(ILayer element);
        ITocElement GetTocElementByLayerId(int layerId);
        //ITOCElement GetTOCElement(IClass element);

        List<ITocElement> GroupElements { get; }

        void RenameElement(ITocElement element, string newName);
        void MoveElement(ITocElement element, ITocElement refElement, bool insertAfter);

        void SplitMultiLayer(ITocElement element);
        int CountGroupLayers(ITocElement Group, bool subGroups);
        int CountVisibleGroupLayers(ITocElement Group, bool subGroups);

        List<ILayer> Layers { get; }
        List<ILayer> VisibleLayers { get; }
        List<ITocElement> Elements { get; }
        List<IWebServiceLayer> VisibleWebServiceLayers { get; }

        Task<GraphicsEngine.Abstraction.IBitmap> Legend();
        Task<GraphicsEngine.Abstraction.IBitmap> Legend(ITocElement element);
        Task<GraphicsEngine.Abstraction.IBitmap> Legend(List<ITocElement> elements);
        Task<TocLegendItems> LegendSymbol(ITocElement element);
    }
}