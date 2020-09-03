using System;
using System.Linq;
using GeoAPI.Geometries;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Union;
using NetTopologySuite.Operation.Polygonize;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSPolygonizer
    {
        public static ICollection<IGeometry> Polygonize(this DBObjectCollection lines)
        {
            var polygonizer = new Polygonizer();
            polygonizer.Add(lines.ToNTSNodedLineStrings());
            return polygonizer.GetPolygons();
        }

        public static ICollection<IGeometry> Polygonize(this Polyline polyline)
        {
            var lines = new DBObjectCollection()
            {
                polyline,
            };
            return lines.Polygonize();
        }

        public static ICollection<IGeometry> Polygonize(this IGeometry geometry)
        {
            var polygonizer = new Polygonizer();
            polygonizer.Add(UnaryUnionOp.Union(geometry));
            return polygonizer.GetPolygons();
        }

        public static DBObjectCollection Polygons(this DBObjectCollection lines)
        {
            var objs = new DBObjectCollection();
            foreach (IPolygon polygon in lines.Polygonize())
            {
                objs.Add(polygon.Shell.ToDbPolyline());
            }
            return objs;
        }

        public static DBObjectCollection Boundaries(this DBObjectCollection lines)
        {
            using (var ov = new ThCADCoreNTSPrecisionReducer())
            {
                var polygons = new List<IPolygon>();
                var boundaries = new DBObjectCollection();
                var geometry = CascadedPolygonUnion.Union(lines.Polygonize());
                if (geometry == null)
                {
                    return boundaries;
                }
                if (geometry is IMultiPolygon mPolygon)
                {
                    foreach (var item in mPolygon.Geometries)
                    {
                        if (item is IPolygon polygon)
                        {
                            polygons.Add(polygon);
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                }
                else if (geometry is IPolygon polygon)
                {
                    polygons.Add(polygon);
                }
                else
                {
                    throw new NotSupportedException();
                }
                foreach (var item in polygons)
                {
                    boundaries.Add(item.Shell.ToDbPolyline());
                }
                return boundaries;
            }
        }

        public static List<IPolygon> OutlineGeometries(this DBObjectCollection lines)
        {
            var geometries = new List<IPolygon>();
            var geometry = CascadedPolygonUnion.Union(lines.Polygonize());
            if (geometry == null)
            {
                return geometries;
            }
            if (geometry is IPolygon polygon)
            {
                geometries.Add(polygon);
            }
            else if (geometry is IMultiPolygon mPolygon)
            {
                foreach (IPolygon subPolygon in mPolygon.Geometries)
                {
                    geometries.Add(subPolygon);
                }
            }
            else
            {
                throw new NotSupportedException();
            }
            return geometries;
        }

        public static DBObjectCollection Outline(this DBObjectCollection lines)
        {
            var objs = new DBObjectCollection();
            if (lines.Count > 0)
            {
                foreach (var geometry in lines.OutlineGeometries())
                {
                    objs.Add(geometry.Shell.ToDbPolyline());
                }
            }
            return objs;
        }

        public static DBObjectCollection FindLoops(this DBObjectCollection lines)
        {
            var polygons = new List<IPolygon>();
            var polygonizer = new Polygonizer();
            var loops = new DBObjectCollection();
            polygonizer.Add(lines.ToNTSNodedLineStrings());
            var geometries = polygonizer.GetPolygons().ToList();
            foreach (var geometry in geometries)
            {
                if (geometry is IMultiPolygon mPolygon)
                {
                    foreach (var item in mPolygon.Geometries)
                    {
                        if (item is IPolygon polygon)
                        {
                            polygons.Add(polygon);
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                }
                else if (geometry is IPolygon polygon)
                {
                    polygons.Add(polygon);
                }
                else
                {
                    continue;
                }
            }
            foreach (var item in polygons)
            {
                loops.Add(item.Shell.ToDbPolyline());
            }
            return loops;
        }
    }
}
