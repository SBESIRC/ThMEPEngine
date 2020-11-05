using System;
using NFox.Cad;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Union;
using NetTopologySuite.Operation.Polygonize;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSPolygonizer
    {
        public static ICollection<Geometry> Polygonize(this DBObjectCollection curves)
        {
            // 空间索引会过滤几何意义上“完全重叠”的图形
            // 对于几何意义上"完全重叠"的图形，Polygonizer会失败
            // 这里正好借用空间索引的特性，来解决Polygonizer会失败的问题
            using (var si = new ThCADCoreNTSSpatialIndex(curves))
            {
                var polygonizer = new Polygonizer();
                polygonizer.Add(UnaryUnionOp.Union(si.Geometries.Keys));
                return polygonizer.GetPolygons();
            }
        }

        public static ICollection<Geometry> Polygonize(this Geometry geometry)
        {
            var polygonizer = new Polygonizer();
            polygonizer.Add(UnaryUnionOp.Union(geometry));
            return polygonizer.GetPolygons();
        }

        public static DBObjectCollection Polygons(this DBObjectCollection lines)
        {
            var objs = new List<DBObject>();
            foreach (Polygon polygon in lines.Polygonize())
            {
                objs.AddRange(polygon.ToDbPolylines());
            }
            return objs.ToCollection();
        }

        public static DBObjectCollection Boundaries(this DBObjectCollection lines)
        {
            var polygons = new List<Polygon>();
            var boundaries = new DBObjectCollection();
            var geometry = CascadedPolygonUnion.Union(lines.Polygonize());
            if (geometry == null)
            {
                return boundaries;
            }
            if (geometry is MultiPolygon mPolygon)
            {
                foreach (var item in mPolygon.Geometries)
                {
                    if (item is Polygon polygon)
                    {
                        polygons.Add(polygon);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            }
            else if (geometry is Polygon polygon)
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

        public static List<Polygon> OutlineGeometries(this DBObjectCollection lines)
        {
            var geometries = new List<Polygon>();
            var geometry = CascadedPolygonUnion.Union(lines.Polygonize());
            if (geometry == null)
            {
                return geometries;
            }
            if (geometry is Polygon polygon)
            {
                geometries.Add(polygon);
            }
            else if (geometry is MultiPolygon mPolygon)
            {
                foreach (Polygon subPolygon in mPolygon.Geometries)
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
            var polygons = new List<Polygon>();
            var polygonizer = new Polygonizer();
            var loops = new DBObjectCollection();
            polygonizer.Add(lines.ToNTSNodedLineStrings());
            var geometries = polygonizer.GetPolygons().ToList();
            foreach (var geometry in geometries)
            {
                if (geometry is MultiPolygon mPolygon)
                {
                    foreach (var item in mPolygon.Geometries)
                    {
                        if (item is Polygon polygon)
                        {
                            polygons.Add(polygon);
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                }
                else if (geometry is Polygon polygon)
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
