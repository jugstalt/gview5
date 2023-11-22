using gView.Blazor.Core.Exceptions;
using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.system;
using gView.GraphicsEngine.Abstraction;
using System;
using System.IO;
using System.Threading.Tasks;

namespace gView.Blazor.Core.Services;

public class MapRenderService : IDisposable
{
    private Map? _map;
    private IBitmap? _iBitmap;
    private MapRenderInstance2? _renderer;
    private ICancelTracker? _cancelTracker;

    private ISpatialReference? _mapControlSRef;
    private IEnvelope? _mapControlBounds;

    private readonly string _scopeId;
    private readonly GeoTransformerService _geoTransformer;

    public MapRenderService(GeoTransformerService geoTransformer)
    {
        _scopeId = Guid.NewGuid().ToString();
        _geoTransformer = geoTransformer;
    }

    #region IDispose

    public void Dispose()
    {
        ReleaseCurrentMapRendererInstance();
        DisposeMap();
        DisposeBitmap();
    }

    #endregion

    public Map CreateMap(ISpatialReference mapSRef)
    {
        var map = new Map();
        map.Display.SpatialReference = mapSRef;
        map.MakeTransparent = true;

        return map;
    }

    public void InitMap(Map map, ISpatialReference? mapControlSRef = null)
    {
        ReleaseCurrentMapRendererInstance();

        _mapControlSRef = mapControlSRef ?? map.SpatialReference;

        #region Remember current extent and size

        var bounds = _map?.Envelope;
        var iWidth = _map?.ImageWidth;
        var iHeight = _map?.ImageHeight;
        var sRef = _map?.SpatialReference;

        #endregion

        if (_map != null)
        {
            _map.Dispose();
            _map.NewBitmap -= NewBitmapCreated;
            _map.DoRefreshMapView -= MakeMapViewRefresh;
        }

        _map = map;

        #region Recover current extend and size

        if (bounds != null && iWidth.HasValue && iHeight.HasValue && sRef != null)
        {
            bounds = _geoTransformer.Transform(bounds, sRef, _map.SpatialReference).Envelope;
            _map.Envelope = bounds;
            _map.ImageWidth = iWidth.Value;
            _map.ImageHeight = iHeight.Value;
        }

        #endregion

        _map.NewBitmap += NewBitmapCreated;
        _map.DoRefreshMapView += MakeMapViewRefresh;
    }

    public void SetBoundsAndSize(IEnvelope bounds, int imageWidth, int imageHeight)
    {
        MapRendererNotIntializedException.ThrowIfNull(_map);

        _map.Display.ImageWidth = imageWidth;
        _map.Display.ImageHeight = imageHeight;

        _mapControlBounds = bounds;

        if (_mapControlSRef != null && _map.SpatialReference != null)
        {
            _map.Display.ZoomTo(_geoTransformer.Transform(bounds, _mapControlSRef, _map.SpatialReference).Envelope);
        }
    }

    #region Properties

    public IEnvelope Bounds => _map?.Envelope ?? Envelope.Null();
    public int ImageWidth => _map?.ImageWidth ?? 0;
    public int ImageHeight => _map?.ImageHeight ?? 0;
    public bool BoundsIntialized() => _map != null &&
        _map?.Envelope != null &&
        !Envelope.IsNull(_map?.Envelope) &&
        _map.Envelope.Width > 0 &&
        _map.Envelope.Height > 0 &&
        ImageWidth > 0 && ImageHeight > 0;

    public ISpatialReference SpatialReference => _map?.SpatialReference ?? new SpatialReference("wgs:3857");

    #endregion

    public bool AddWebFeatureClass(IWebFeatureClass webFeatureClass, IWebServiceClass webServiceClass)
    {
        MapRendererNotIntializedException.ThrowIfNull(_map);
        _map.AddLayer(LayerFactory.Create(webServiceClass));
        _map.AddLayer(LayerFactory.Create(webFeatureClass, webServiceClass));
        return true;
    }

    public bool AddFeatureClass(IFeatureClass featureClass)
    {
        MapRendererNotIntializedException.ThrowIfNull(_map);
        _map.AddLayer(LayerFactory.Create(featureClass));
        return true;
    }

    public bool AddRasterClass(IRasterClass rasterClass)
    {
        MapRendererNotIntializedException.ThrowIfNull(_map);
        _map.AddLayer(LayerFactory.Create(rasterClass));
        return true;
    }

    public bool AddWebServiceClass(IWebServiceClass webServiceClass)
    {
        MapRendererNotIntializedException.ThrowIfNull(_map);
        _map.AddLayer(LayerFactory.Create(webServiceClass));
        return true;
    }

    async public Task<bool> AddFeatureDataset(IFeatureDataset featureDataset)
    {
        MapRendererNotIntializedException.ThrowIfNull(_map);

        foreach (var datasetElement in (await featureDataset.Elements()).OrEmpty())
        {
            if (datasetElement?.Class is IFeatureClass)
            {
                AddFeatureClass((IFeatureClass)datasetElement.Class);
            }
        }

        return true;
    }

    public void BeginRender()
    {
        MapRendererNotIntializedException.ThrowIfNull(_map);
        MapRendererNotIntializedException.ThrowIfNull(_mapControlSRef);
        MapRendererNotIntializedException.ThrowIfNull(_mapControlBounds);

        Task.Run(async () =>
        {
            var mapRenderInstance = await CreateMapRendererInstance();
            mapRenderInstance.SpatialReference = _mapControlSRef;
            mapRenderInstance.Display.ZoomTo(_mapControlBounds);
            await mapRenderInstance.RefreshMap(DrawPhase.All, _cancelTracker = new CancelTracker());
        });
    }

    public void CancelRender()
    {
        _cancelTracker?.Cancel();
    }

    public async Task Rerender(int delay = 0)
    {
        if (delay <= 0)
        {
            CancelRender();
            BeginRender();
        }

        using (var mutex = await FuzzyMutexAsync.LockAsync(_scopeId))
        {
            if (mutex.WasBlocked == false)
            {
                await Task.Delay(delay);

                CancelRender();
                BeginRender();
            }
        }
    }

    #region Helper

    async private Task<MapRenderInstance2> CreateMapRendererInstance()
    {
        ReleaseCurrentMapRendererInstance();

        var mapRendererInstance = await MapRenderInstance2.CreateAsync(_map);

        mapRendererInstance.NewBitmap += NewBitmapCreated;
        mapRendererInstance.DoRefreshMapView += MakeMapViewRefresh;

        return mapRendererInstance;
    }

    private void ReleaseCurrentMapRendererInstance()
    {
        if (_cancelTracker != null)
        {
            _cancelTracker.Cancel();
        }

        if (_renderer != null)
        {
            _renderer.NewBitmap -= NewBitmapCreated;
            _renderer.DoRefreshMapView -= MakeMapViewRefresh;

            _renderer = null;
        }
    }

    private byte[]? CreateBitmapSnapshot()
    {
        if (_iBitmap != null)
        {
            try
            {
                var ms = new MemoryStream();

                _iBitmap.Save(ms, GraphicsEngine.ImageFormat.Png);
                return ms.ToArray();
            }
            catch { }
        }

        return null;
    }

    private void DisposeMap()
    {
        if (_map != null)
        {
            _map.Dispose();
            _map = null;
        }
    }

    private void DisposeBitmap()
    {
        if (_iBitmap != null)
        {
            _iBitmap.Dispose();
            _iBitmap = null;
        }
    }

    #endregion

    #region Events

    public delegate Task RefreshMapImage(byte[] data);
    public event RefreshMapImage? OnRefreshMapImage;

    public void FireRefreshMapImage(byte[]? data)
        => OnRefreshMapImage?.Invoke(data ?? Array.Empty<byte>());

    #endregion

    #region EventHandlers

    private void NewBitmapCreated(IBitmap bitmap)
    {
        _iBitmap = bitmap;

        FireRefreshMapImage(CreateBitmapSnapshot());
    }

    private void MakeMapViewRefresh()
    {
        FireRefreshMapImage(CreateBitmapSnapshot());
    }

    #endregion
}
