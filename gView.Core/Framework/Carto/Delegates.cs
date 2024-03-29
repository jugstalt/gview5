using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.system;
using System.Collections.Generic;

namespace gView.Framework.Carto
{
    //public delegate void DatasetAddedEvent(IMap sender,IDataset dataset);
    public delegate void LayerAddedEvent(IMap sender, ILayer layer);
    public delegate void LayerRemovedEvent(IMap sender, ILayer layer);
    public delegate void NewBitmapEvent(GraphicsEngine.Abstraction.IBitmap bitmap);
    public delegate void DoRefreshMapViewEvent();
    public delegate void StartRefreshMapEvent(IMap sender);
    public delegate void DrawingLayerEvent(string layerName);
    public delegate void TOCChangedEvent(IMap sender);
    public delegate void NewExtentRenderedEvent(IMap sender, IEnvelope extent);
    public delegate void DrawingLayerFinishedEvent(IMap sender, ITimeEvent timeEvent);
    public delegate void UserIntefaceEvent(IMap sender, bool lockUI);

    /*
    public interface IDataView
    {
        string Name { get; set; }

        IMap Map { get; set; }
    }
    */

    //public delegate bool LayerIsVisibleHook(string layername,bool defaultValue);
    public delegate void BeforeRenderLayersEvent(IServiceMap sender, List<ILayer> layers);

    public delegate void MapScaleChangedEvent(IDisplay sender);
    public delegate void RenderOverlayImageEvent(GraphicsEngine.Abstraction.IBitmap bitmap, bool clearOld);

    // Projective > 0
    // Geographic < 0

}