using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.UI;
using gView.MapServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace gView.Framework.Carto
{
    public interface IServiceMap : IMetadata, IServiceRequestContext, IDisposable
    {
        //event LayerIsVisibleHook OverrideLayerIsVisible;
        event BeforeRenderLayersEvent BeforeRenderLayers;

        string Name { get; }
        string Title { get; }
        IDisplay Display { get; }
        IToc TOC { get; }

        List<IDatasetElement> MapElements { get; }

        void Release();
        Task<bool> Render();
        Task<GraphicsEngine.Abstraction.IBitmap> Legend();
        GraphicsEngine.Abstraction.IBitmap MapImage { get; }
        Task<int> SaveImage(string path, GraphicsEngine.ImageFormat format);
        Task<int> SaveImage(Stream ms, GraphicsEngine.ImageFormat format);
        void ReleaseImage();

        //float ScaleSymbolFactor { get; set; }

        T GetModule<T>();

        string GetLayerDescription(int layerId);

        string GetLayerCopyrightText(int layerId);

        ISpatialReference LayerDefaultSpatialReference
        {
            get;
            set;
        }
    }
}