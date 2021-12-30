using System;
using ThCADCore.NTS;
using NetTopologySuite.Features;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThGeoJSONFeature
    {
        public Entity Geometry { get; set; }
        public Dictionary<string, object> Attributes { get; set; }
        public ThGeoJSONFeature()
        {
            Attributes = new Dictionary<string, object>();
        }
        public Feature ToFeature()
        {
            if (Geometry == null)
            {
                var geo = ThCADCoreNTSService.Instance.GeometryFactory.CreateEmpty(
                    NetTopologySuite.Geometries.Dimension.Unknown);
                var attributesTable = new AttributesTable(Attributes);
                var feature = new Feature(geo, attributesTable);
                return feature;
            }
            else
            {
                if (Geometry is Polyline polyline)
                {
                    if (polyline.Closed)
                    {
                        var geo = polyline.ToNTSPolygon();
                        var attributesTable = new AttributesTable(Attributes);
                        var feature = new Feature(geo, attributesTable);
                        return feature;
                    }
                    else
                    {
                        var geo = polyline.ToNTSLineString();
                        var attributesTable = new AttributesTable(Attributes);
                        var feature = new Feature(geo, attributesTable);
                        return feature;
                    }
                }
                else if (Geometry is Line line)
                {
                    var geo = line.ToNTSLineString();
                    var attributesTable = new AttributesTable(Attributes);
                    var feature = new Feature(geo, attributesTable);
                    return feature;
                }
                else if (Geometry is DBPoint dbPoint)
                {
                    var geo = dbPoint.ToNTSPoint();
                    var attributesTable = new AttributesTable(Attributes);
                    var feature = new Feature(geo, attributesTable);
                    return feature;
                }
                else if (Geometry is MPolygon mPolygon)
                {
                    var geo = mPolygon.ToNTSPolygon();
                    var attributesTable = new AttributesTable(Attributes);
                    var feature = new Feature(geo, attributesTable);
                    return feature;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
