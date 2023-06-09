﻿using System;
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
            var polygonizer = new Polygonizer();
            var dbObjs = ThCADCoreNTSGeometryFilter.GeometryEquality(curves);
            polygonizer.Add(dbObjs.ToNTSNodedLineStrings());
            return polygonizer.GetPolygons();
        }

        public static ICollection<Geometry> Polygonize(this MultiLineString lineStrings)
        {
            var polygonizer = new Polygonizer();
            polygonizer.Add(lineStrings.ToNTSNodedLineStrings());
            return polygonizer.GetPolygons();
        }

        [Obsolete("该方法已被弃用，请使用PolygonsEx代替")]
        public static DBObjectCollection Polygons(this DBObjectCollection lines)
        {
            var objs = new List<DBObject>();
            foreach (Polygon polygon in lines.Polygonize())
            {
                objs.AddRange(polygon.ToDbPolylines());
            }
            return objs.ToCollection();
        }
        
        /// <summary>
        /// 获取多边形面（支持洞）
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static DBObjectCollection PolygonsEx(this DBObjectCollection lines)
        {
            var geos = lines.Polygonize();
            var objs = new DBObjectCollection();
            foreach (Polygon polygon in geos)
            {
                objs.Add(polygon.ToDbEntity());
            }
            geos.Clear();
            geos = null;
            return objs;
        }

        public static DBObjectCollection Outline(this DBObjectCollection lines)
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
                // 暂时不考虑有“洞”的情况
                boundaries.Add(item.Shell.ToDbPolyline());
            }
            return boundaries;
        }
    }
}
