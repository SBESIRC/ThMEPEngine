using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
using NetTopologySuite.Operation.Polygonize;

namespace ThMEPHVAC.FloorHeatingCoil.Service
{
    public static class LineStringEx
    {
        public static List<Polygon> GetPolygons(this List<LineString> linestrings)
        {
            var LSTR_Union = linestrings.Union();
            var geos = LSTR_Union.Polygonize();
            return geos.ToList();
        }
        public static Geometry Union(this List<LineString> linestrings)
        {
            // UnaryUnionOp.Union()有Robust issue
            // 会抛出"non-noded intersection" TopologyException
            // OverlayNGRobust.Union()在某些情况下仍然会抛出TopologyException (NTS 2.2.0)
            var lineStrSet = linestrings.ToHashSet();//去重
            var firstOne = lineStrSet.First();
            lineStrSet.Remove(firstOne);
            var multiLineStrings = new MultiLineString(lineStrSet.ToArray());
            return OverlayNGRobust.Overlay(firstOne, multiLineStrings, SpatialFunction.Union);
        }

        public static IEnumerable<Polygon> Polygonize(this Geometry geo)
        {
            var polygonizer = new Polygonizer();
            polygonizer.Add(geo);
            return polygonizer.GetPolygons().Cast<Polygon>();
        }
    }
}
