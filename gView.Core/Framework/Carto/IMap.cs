using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.Framework.Carto
{
    public interface IMap : IDisposable, IClone, IMetadata, IDataCopyright, IPersistableLoadAsync
    {
        event LayerAddedEvent LayerAdded;
        event LayerRemovedEvent LayerRemoved;
        event NewBitmapEvent NewBitmap;
        event DoRefreshMapViewEvent DoRefreshMapView;
        event DrawingLayerEvent DrawingLayer;
        event TOCChangedEvent TOCChanged;
        event NewExtentRenderedEvent NewExtentRendered;
        event DrawingLayerFinishedEvent DrawingLayerFinished;
        event StartRefreshMapEvent StartRefreshMap;
        event EventHandler MapRenamed;
        event UserIntefaceEvent OnUserInterface;

        void AddDataset(IDataset dataset, int order);
        void RemoveDataset(IDataset dataset);
        void RemoveAllDatasets();

        void AddLayer(ILayer layer);
        void AddLayer(ILayer layer, int pos);
        void RemoveLayer(ILayer layer);

        IDataset this[int datasetIndex]
        {
            get;
        }
        IDataset this[IDatasetElement element]
        {
            get;
        }
        IEnumerable<IDataset> Datasets { get; }
        string Name { get; set; }

        string Title { get; set; }

        Task<List<IDatasetElement>> Elements(string aliasname);
        List<IDatasetElement> MapElements { get; }
        Task<List<IDatasetElement>> ActiveLayers();

        IDatasetElement DatasetElementByClass(IClass cls);

        string ActiveLayerNames
        {
            get;
            set;
        }
        void ClearSelection();

        IToc TOC { get; }

        Task<bool> RefreshMap(DrawPhase phase, ICancelTracker cancelTracker);

        ISelectionEnvironment SelectionEnvironment { get; }

        void HighlightGeometry(IGeometry geometry, int milliseconds);

        IDisplay Display { get; }

        void Release();

        bool IsRefreshing { get; }

        ISpatialReference LayerDefaultSpatialReference
        {
            get;
            set;
        }

        IEnumerable<string> ErrorMessages { get; }
        bool HasErrorMessages { get; }

        void ResetRequestExceptions();

        bool HasRequestExceptions { get; }

        IEnumerable<Exception> RequestExceptions { get; }

        void Compress();

        string GetLayerDescription(int layerId);
        void SetLayerDescription(int layerId, string description);

        string GetLayerCopyrightText(int layerId);
        void SetLayerCopyrightText(int layerId, string copyrightText);

        IResourceContainer ResourceContainer { get; }
    }
}