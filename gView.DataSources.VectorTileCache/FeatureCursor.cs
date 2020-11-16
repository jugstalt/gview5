﻿using gView.Framework.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using gView.DataSources.VectorTileCache.Extensions;

namespace gView.DataSources.VectorTileCache
{
    class FeatureCursor : gView.Framework.Data.FeatureCursor
    {
        private readonly FeatureClass _fc;
        private readonly IQueryFilter _filter;
        private readonly FeatureCache _cache;
        private readonly GeoJSON.Net.Feature.Feature[] _geoJsonFeatures;
        private int _pos = 0;

        public FeatureCursor(FeatureClass fc, IQueryFilter filter)
            :base(fc?.SpatialReference, filter?.FeatureSpatialReference)
        {
            _fc = fc;
            _filter = filter;
            _cache = _filter?.DatasetCachingContext?.GetCache<FeatureCache>();

            _geoJsonFeatures = _cache?[_fc.Name]?.ToArray();
        }

        #region IFeatureCursor

        public override void Dispose()
        {
            
        }

        public override Task<IFeature> NextFeature()
        {
            if (_geoJsonFeatures == null || _geoJsonFeatures.Length <= _pos)
                return Task.FromResult<IFeature>(null);

            var geoJsonFeature = _geoJsonFeatures[_pos++];

            var feature = new Feature();

            if (geoJsonFeature?.Properties != null)
            {
                foreach (var propertyName in geoJsonFeature.Properties.Keys)
                {
                    feature.Fields.Add(new FieldValue(propertyName, geoJsonFeature.Properties[propertyName]));
                }
            }

            if(geoJsonFeature.Geometry != null)
            {
                feature.Shape = geoJsonFeature.Geometry.ToGeometry();
            }

            

            // ToDo: Check filter...

            Transform(feature);
            return Task.FromResult<IFeature>(feature);
        }

        #endregion
    }
}
