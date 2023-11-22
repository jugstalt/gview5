using gView.Framework.Geometry;
using gView.Framework.Symbology;
using gView.GraphicsEngine;
using System;

namespace gView.Framework.Carto.Rendering
{
    public class RendererFunctions
    {
        static internal Random r = new Random(DateTime.Now.Millisecond);
        static internal ArgbColor RandomColor
        {
            get
            {
                return ArgbColor.FromArgb(r.Next(255), r.Next(255), r.Next(255));
            }
        }
        static public ISymbol CreateStandardSymbol(GeometryType type)
        {
            ISymbol symbol = null;
            switch (type)
            {
                case GeometryType.Envelope:
                case GeometryType.Polygon:
                    symbol = new SimpleFillSymbol();
                    ((SimpleFillSymbol)symbol).Color = RandomColor;
                    ((SimpleFillSymbol)symbol).SmoothingMode = SymbolSmoothing.AntiAlias;
                    ((SimpleFillSymbol)symbol).OutlineSymbol = new SimpleLineSymbol();
                    ((SimpleLineSymbol)((SimpleFillSymbol)symbol).OutlineSymbol).Color = RandomColor;
                    ((SimpleLineSymbol)((SimpleFillSymbol)symbol).OutlineSymbol).Smoothingmode = SymbolSmoothing.AntiAlias;
                    break;
                case GeometryType.Polyline:
                    symbol = new SimpleLineSymbol();
                    ((SimpleLineSymbol)symbol).Color = RandomColor;
                    ((SimpleLineSymbol)symbol).Smoothingmode = SymbolSmoothing.AntiAlias;
                    break;
                case GeometryType.Multipoint:
                case GeometryType.Point:
                    symbol = new SimplePointSymbol();
                    ((SimplePointSymbol)symbol).Color = RandomColor;
                    ((SimplePointSymbol)symbol).Smoothingmode = SymbolSmoothing.AntiAlias;
                    break;
            }
            return symbol;
        }

        static public ISymbol CreateStandardSelectionSymbol(GeometryType type)
        {
            ISymbol symbol = null;
            switch (type)
            {
                case GeometryType.Envelope:
                case GeometryType.Polygon:
                    symbol = new SimpleFillSymbol();
                    ((SimpleFillSymbol)symbol).Color = ArgbColor.Transparent;
                    ((SimpleFillSymbol)symbol).SmoothingMode = SymbolSmoothing.AntiAlias;
                    ((SimpleFillSymbol)symbol).OutlineSymbol = new SimpleLineSymbol();
                    ((SimpleLineSymbol)((SimpleFillSymbol)symbol).OutlineSymbol).Color = ArgbColor.Cyan;
                    ((SimpleLineSymbol)((SimpleFillSymbol)symbol).OutlineSymbol).Width = 3;
                    ((SimpleLineSymbol)((SimpleFillSymbol)symbol).OutlineSymbol).Smoothingmode = SymbolSmoothing.AntiAlias;
                    break;
                case GeometryType.Polyline:
                    symbol = new SimpleLineSymbol();
                    ((SimpleLineSymbol)symbol).Color = ArgbColor.Cyan;
                    ((SimpleLineSymbol)symbol).Width = 3;
                    ((SimpleLineSymbol)symbol).Smoothingmode = SymbolSmoothing.AntiAlias;
                    break;
                case GeometryType.Point:
                    symbol = new SimplePointSymbol();
                    ((SimplePointSymbol)symbol).Color = ArgbColor.Cyan;
                    ((SimplePointSymbol)symbol).Size = 5;
                    ((SimplePointSymbol)symbol).Smoothingmode = SymbolSmoothing.AntiAlias;
                    break;
            }
            return symbol;
        }

        static public ISymbol CreateStandardHighlightSymbol(GeometryType type)
        {
            ISymbol symbol = null;
            switch (type)
            {
                case GeometryType.Envelope:
                case GeometryType.Polygon:
                    symbol = new SimpleFillSymbol();
                    ((SimpleFillSymbol)symbol).Color = ArgbColor.FromArgb(100, 255, 255, 0);
                    ((SimpleFillSymbol)symbol).SmoothingMode = SymbolSmoothing.AntiAlias;
                    ((SimpleFillSymbol)symbol).OutlineSymbol = new SimpleLineSymbol();
                    ((SimpleLineSymbol)((SimpleFillSymbol)symbol).OutlineSymbol).Color = ArgbColor.Yellow;
                    ((SimpleLineSymbol)((SimpleFillSymbol)symbol).OutlineSymbol).Width = 5;
                    ((SimpleLineSymbol)((SimpleFillSymbol)symbol).OutlineSymbol).Smoothingmode = SymbolSmoothing.AntiAlias;
                    break;
                case GeometryType.Polyline:
                    symbol = new SimpleLineSymbol();
                    ((SimpleLineSymbol)symbol).Color = ArgbColor.Yellow;
                    ((SimpleLineSymbol)symbol).Width = 5;
                    ((SimpleLineSymbol)symbol).Smoothingmode = SymbolSmoothing.AntiAlias;
                    break;
                case GeometryType.Point:
                    symbol = new SimplePointSymbol();
                    ((SimplePointSymbol)symbol).Color = ArgbColor.Yellow;
                    ((SimplePointSymbol)symbol).Size = 10;
                    ((SimplePointSymbol)symbol).Smoothingmode = SymbolSmoothing.AntiAlias;
                    break;
            }
            return symbol;
        }
    }
}
