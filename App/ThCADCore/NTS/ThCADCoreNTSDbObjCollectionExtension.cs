using System;
using GeoAPI.Geometries;
using System.Collections.Generic;
using NetTopologySuite.Algorithm;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSDbObjCollectionExtension
    {
        public static IMultiPolygon ToNTSPolygons(this DBObjectCollection objs)
        {
            var polygons = new List<IPolygon>();
            foreach (Entity entity in objs)
            {
                if (entity is Polyline polyline)
                {
                    polygons.Add(polyline.ToNTSPolygon());
                }
                else if (entity is Region region)
                {
                    polygons.Add(region.ToNTSPolygon());
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPolygon(polygons.ToArray());
        }

        public static IGeometryCollection ToNTSPolygonCollection(this DBObjectCollection curves)
        {
            var polygons = new List<IPolygon>();
            foreach (Polyline polyline in curves)
            {
                polygons.Add(polyline.ToNTSPolygon());
            }
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateGeometryCollection(polygons.ToArray());
        }


        public static IGeometry ToNTSNodedLineStrings(this DBObjectCollection curves, double chord = 5.0)
        {
            IGeometry nodedLineString = ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString();
            foreach (DBObject curve in curves)
            {
                if (curve is Line line)
                {
                    nodedLineString = nodedLineString.Union(line.ToNTSLineString());
                }
                else if (curve is Polyline polyline)
                {
                    nodedLineString = nodedLineString.Union(polyline.ToNTSLineString());
                }
                else if (curve is Arc arc)
                {
                    nodedLineString = nodedLineString.Union(arc.TessellateWithChord(chord));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return nodedLineString;
        }

        public static Polyline GetMinimumRectangle(this DBObjectCollection curves)
        {
            var geom = curves.ToNTSNodedLineStrings();
            var rectangle = MinimumDiameter.GetMinimumRectangle(geom);
            if (rectangle is IPolygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static List<IGeometry> ToNTSLineStrings(this DBObjectCollection curves, double chord = 5.0)
        {
            var geometries = new List<IGeometry>();
            foreach (DBObject curve in curves)
            {
                if (curve is Line line)
                {
                    geometries.Add(line.ToNTSLineString());
                }
                else if (curve is Arc arc)
                {
                    geometries.Add(arc.TessellateWithChord(chord));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return geometries;
        }

        public static Region ToDbRegion(this IPolygon polygon)
        {
            try
            {
                // 暂时不考虑有“洞”的情况
                var curves = new DBObjectCollection
                {
                    polygon.Shell.ToDbPolyline()
                };
                return Region.CreateFromCurves(curves)[0] as Region;
            }
            catch
            {
                // 未知错误
                return null;
            }
        }
    }
}