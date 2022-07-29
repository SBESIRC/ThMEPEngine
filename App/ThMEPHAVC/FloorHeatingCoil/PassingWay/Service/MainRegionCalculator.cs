using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NetTopologySuite.Geometries;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPHVAC.FloorHeatingCoil
{
    class MainRegionCalculator
    {
        public static Polyline GetMainRegion(Polyline left, Polyline right, Polyline polygon)
        {
            var inter = left.ToNTSLineString().Intersection(polygon.ToNTSPolygon()).ToDbCollection().Cast<Polyline>();
            if (inter.Count() == 0)
                return GetMainRegion(right, polygon, true);
            var left_points = inter.First().GetPoints().ToList();
            inter = right.ToNTSLineString().Intersection(polygon.ToNTSPolygon()).ToDbCollection().Cast<Polyline>();
            if (inter.Count() == 0)
                return GetMainRegion(left, polygon, false);
            var right_points = inter.First().GetPoints().ToList();
            var points = PassageWayUtils.GetPolyPoints(polygon, true);
            var le = PassageWayUtils.GetSegIndexOnPolyline(left_points.Last(), points);
            var re = PassageWayUtils.GetSegIndexOnPolyline(right_points.Last(), points);
            points.RemoveAt(points.Count - 1);
            List<Point3d> target = new List<Point3d>();
            // add left seg
            for (int i = 0; i < left_points.Count; ++i)
                target.Add(left_points[i]);
            // add le-re seg
            if (le != re)
                for (int i = le; i != re; i = (i + 1) % points.Count)
                    target.Add(points[(i + 1) % points.Count]);
            // add right seg
            for (int i = right_points.Count - 1; i >= 0; --i)
                target.Add(right_points[i]);
            // add rs-ls seg
            target.Add(target[0]);
            return PassageWayUtils.BuildPolyline(target);
        }
        public static Polyline GetMainRegion(Polyline line, Polyline polygon, bool line_is_right)
        {
            // if line is overlap polygon
            var test_inter = polygon.Buffer(-1).Intersection(line).ToDbCollection();
            if (test_inter.Count == 0)
                return polygon.Clone() as Polyline;
            // if line is outside polygon
            var inter = line.ToNTSLineString().Intersection(polygon.ToNTSPolygon()).ToDbCollection().Cast<Polyline>().ToList();
            if (inter.Count == 0)
                return polygon.Clone() as Polyline;
            // line has segs in polygon
            var line_points = new List<Point3d>();
            foreach(var poly in inter)
            {
                test_inter = polygon.Buffer(-1).Intersection(poly).ToDbCollection();
                if (test_inter.Count > 0)
                {
                    line_points = poly.GetPoints().ToList();
                    break;
                }
            }
            if (line_points.Count == 0)
                return polygon.Clone() as Polyline;
            var points = PassageWayUtils.GetPolyPoints(polygon, true);
            // add first point
            if (PassageWayUtils.GetPointIndex(line_points.First(), points, 1e-3) == -1)
                IntersectUtils.InsertPoint(line_points.First(), ref points);
            // add last point
            if (PassageWayUtils.GetPointIndex(line_points.Last(), points, 1e-3) == -1)
                IntersectUtils.InsertPoint(line_points.Last(), ref points);
            points.RemoveAt(points.Count - 1);
            var s = PassageWayUtils.GetPointIndex(line_points.First(), points);
            var e = PassageWayUtils.GetPointIndex(line_points.Last(), points);
            var target_points = new List<Point3d>();
            if (line_is_right)
            {
                for (int i = (s + 1) % points.Count; i != e; i = (i + 1) % points.Count)
                    target_points.Add(points[i]);
                for (int i = line_points.Count - 1; i >= 0; --i)
                    target_points.Add(line_points[i]);
            }
            else
            {
                for (int i = 0; i < line_points.Count; ++i)
                    target_points.Add(line_points[i]);
                for (int i = (e + 1) % points.Count; i != s; i = (i + 1) % points.Count)
                    target_points.Add(points[i]);
            }
            target_points.Add(target_points.First());
            return PassageWayUtils.BuildPolyline(target_points);
        }
        public static void ExtandPolyline(ref List<Point3d> points,double buffer)
        {
            if (points.Count <= 1) return;
            var p0 = points[0];
            var p1 = points[1];
            points[0] = p0 + (p0 - p1).GetNormal() * buffer;
            p0 = points.Last();
            p1 = points[points.Count - 2];
            points[points.Count - 1] = p0 + (p0 - p1).GetNormal() * buffer;
        }
    }
}
