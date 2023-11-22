using gView.Framework.Geometry;
using gView.Framework.Symbology;

namespace gView.Framework.Carto
{
    public interface IDisplay
    {
        event MapScaleChangedEvent MapScaleChanged;
        event RenderOverlayImageEvent RenderOverlayImage;

        IEnvelope Envelope
        {
            get;
        }
        IEnvelope Limit
        {
            get;
            set;
        }
        void ZoomTo(IEnvelope envelope);

        double ReferenceScale { get; set; }
        double MapScale { get; set; }

        int ImageWidth { get; set; }
        int ImageHeight { get; set; }

        double Dpm { get; }
        double Dpi { get; set; }

        gView.GraphicsEngine.Abstraction.IBitmap Bitmap { get; }
        gView.GraphicsEngine.Abstraction.ICanvas Canvas { get; }

        gView.GraphicsEngine.ArgbColor BackgroundColor { get; set; }
        gView.GraphicsEngine.ArgbColor TransparentColor { get; set; }

        bool MakeTransparent
        {
            get;
            set;
        }

        void World2Image(ref double x, ref double y);
        void Image2World(ref double x, ref double y);

        ISpatialReference SpatialReference
        {
            get;
            set;
        }

        IGeometricTransformer GeometricTransformer
        {
            get;
            set;
        }

        void Draw(ISymbol symbol, IGeometry geometry);
        void DrawOverlay(IGraphicsContainer container, bool clearOld);
        void ClearOverlay();

        IGraphicsContainer GraphicsContainer { get; }

        ILabelEngine LabelEngine { get; }

        GeoUnits MapUnits { get; set; }
        GeoUnits DisplayUnits { get; set; }

        IScreen Screen { get; }
        IMap Map { get; }

        IDisplayTransformation DisplayTransformation
        {
            get;
        }
    }
}