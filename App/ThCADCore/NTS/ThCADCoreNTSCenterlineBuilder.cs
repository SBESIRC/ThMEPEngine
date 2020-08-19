using GeoAPI.Geometries;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.LinearReferencing;

namespace ThCADCore.NTS
{
    public class ThCADCoreNTSCenterlineBuilder
    {
        public static DBObjectCollection Centerline(Polyline polyline, double interpolationDistance)
        {
            var geometry = polyline.ToNTSPolygon();
            var lineStrings = new List<IGeometry>();
            foreach (IPolygon polygon in polyline.VoronoiDiagram(interpolationDistance).Geometries)
            {
                var iterator = new LinearIterator(polygon.Shell);
                for (; iterator.HasNext();iterator.Next())
                {
                    if (!iterator.IsEndOfLine)
                    {
                        var line = CreateLineString(iterator);
                        if (line.Within(geometry))
                        {
                            lineStrings.Add(line);
                        }
                    }
                }
            }
            var centerlines = new DBObjectCollection();
            foreach(ILineString lineString in lineStrings)
            {
                centerlines.Add(lineString.ToDbPolyline());
            }
            return centerlines;
        }

        private static IGeometry CreateLineString(LinearIterator iterator)
        {
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString(new Coordinate[]
            {
                iterator.SegmentStart,
                iterator.SegmentEnd
            });
        }
    }
}
