﻿using gView.Framework.Geometry;

namespace gView.Blazor.Core.Services;

public class GeoTransformerService
{
    public IGeometry ToWGS84(IGeometry geometry, ISpatialReference fromSRef)
    {
        var toSRef = new SpatialReference($"epsg:4326");

        return Transform(geometry, fromSRef, toSRef);
    }

    public IGeometry FromWGS84(IGeometry geometry, ISpatialReference toSRef)
    {
        var fromSRef = new SpatialReference($"epsg:4326");

        return Transform(geometry, fromSRef, toSRef);
    }

    public IGeometry Transform(IGeometry geometry, int fromEpsg, int toEpsg)
    {
        var fromSRef = new SpatialReference($"epsg:{fromEpsg}");
        var toSRef = new SpatialReference($"epsg:{toEpsg}");

        return Transform(geometry, fromSRef, toSRef);
    }

    public IGeometry Transform(IGeometry geometry, ISpatialReference fromSRef, ISpatialReference toSRef)
    {
        using (var transformer = GeometricTransformerFactory.Create())
        {
            transformer.SetSpatialReferences(fromSRef, toSRef);
            return transformer.Transform2D(geometry) as IGeometry;
        }
    }
}