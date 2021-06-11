using System;
using ThCADCore.NTS;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Features
{
    public class ThGeometryFeature
    {
        public static Feature Construct(ThGeometry geometry)
        {
            if (geometry != null && geometry.Boundary!=null)
            {
                if (geometry.Boundary is Polyline polyline)
                {
                    if (polyline.Closed)
                    {
                        var geo = polyline.ToNTSPolygon();
                        var attributesTable = new AttributesTable(geometry.Properties);
                        var feature = new Feature(geo, attributesTable);
                        return feature;
                    }
                    else
                    {
                        var geo = polyline.ToNTSLineString();
                        var attributesTable = new AttributesTable(geometry.Properties);
                        var feature = new Feature(geo, attributesTable);
                        return feature;
                    }
                }
                else if (geometry.Boundary is Line line)
                {
                    var geo = line.ToNTSLineString();
                    var attributesTable = new AttributesTable(geometry.Properties);
                    var feature = new Feature(geo, attributesTable);
                    return feature;
                }
                else if (geometry.Boundary is DBPoint dbPoint)
                {
                    var geo = new Point(dbPoint.Position.ToNTSCoordinate());
                    var attributesTable = new AttributesTable(geometry.Properties);
                    var feature = new Feature(geo, attributesTable);
                    return feature;
                }
                else if (geometry.Boundary is MPolygon mPolygon)
                {
                    var geo = mPolygon.ToNTSPolygon();
                    var attributesTable = new AttributesTable(geometry.Properties);
                    var feature = new Feature(geo, attributesTable);
                    return feature;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return null;
        }
    }
}
