using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using ThCADCore.NTS;
using static ThMEPArchitecture.GeoUtilities;
using Linq2Acad;

namespace ThMEPArchitecture
{
    public class ParkingPartition
    {
        public ParkingPartition(List<Polyline> walls, List<Line> iniLanes,
            List<Polyline> obstacles)
        {
            Walls = walls;
            IniLanes = iniLanes;
            Obstacles = obstacles;
            Boundary = JoinCurves(Walls, IniLanes)[0];
            BoundingBox = Boundary.CalObb();
            Walls.ForEach(e => Cutters.Add(e));
            IniLanes.ForEach(e => Cutters.Add(e));
            Obstacles.ForEach(e => Cutters.Add(e));
            ObstaclesSpatialIndex = new ThCADCoreNTSSpatialIndex(Cutters);
        }
        private List<Polyline> Walls;
        private List<Line> IniLanes;
        private List<Polyline> Obstacles;
        private Polyline Boundary;
        private Polyline BoundingBox;
        private DBObjectCollection Cutters = new DBObjectCollection();
        private ThCADCoreNTSSpatialIndex ObstaclesSpatialIndex;
        private List<Polyline> CarSpots = new List<Polyline>();

        const double DisLaneWidth = 5500;
        const double DisCarLength = 5100;
        const double DisCarWidth = 2400;
        const double DisCarAndHalfLane = DisLaneWidth / 2 + DisCarLength;
        const double DisCarAndLane = DisLaneWidth + DisCarLength;
        const double DisModulus = DisCarAndHalfLane * 2;

        //custom
        const double LengthGenetateHalfLane = 10600;
        const double LengthGenetateModuleLane = 10600;


        public int CalNumOfParkingSpaces()
        {
            int sum = 0;
            GenerateParkingSpaces();
            sum = CarSpots.Count;
            return sum;
        }

        public void GenerateParkingSpaces()
        {
            GenerateLanes();
            //IniLanes.AddToCurrentSpace();
            GenerateParkingSpots();
            //CarSpots.AddToCurrentSpace();
        }

        public void test()
        {
            var splited = new DBObjectCollection();
            Boundary.Explode(splited);
            var ls = splited.Cast<Line>().ToList();
            //IniLanes.ForEach(e => ls.Remove(e));
            foreach (var lane in IniLanes)
            {
                for (int i = 0; i < ls.Count; i++)
                {
                    if (Math.Abs(ls[i].Length - lane.Length) < 1
                        && ls[i].GetClosestPointTo(lane.StartPoint, false).DistanceTo(lane.StartPoint) < 1
                        && ls[i].GetClosestPointTo(lane.EndPoint, false).DistanceTo(lane.EndPoint) < 1)
                    {
                        ls.RemoveAt(i);
                        break;
                    }
                }
            }
            var pls = JoinCurves(new List<Polyline>(), ls);
            pls.AddToCurrentSpace();
        }

        /// <summary>
        /// 生成停车位
        /// </summary>
        private void GenerateParkingSpots()
        {
            IniLanes.ForEach(e => AddToSpatialIndex(e, ref ObstaclesSpatialIndex));
            SortLinesByLength(IniLanes, false);
            List<Polyline> laneBoxes = new List<Polyline>();
            foreach (var l in IniLanes)
            {
                var a = OffsetLine(l, DisLaneWidth / 2)[0];
                var b = OffsetLine(l, DisLaneWidth / 2)[1];
                var pl = PolyFromPoints(new List<Point3d>() {
                    a.StartPoint,a.EndPoint,b.EndPoint,b.StartPoint,a.StartPoint});
                AddToSpatialIndex(pl, ref ObstaclesSpatialIndex);
                laneBoxes.Add(pl);
            }
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var l = IniLanes[i];
                var ltops = OffsetLine(l, DisLaneWidth / 2);
                var lbottoms = OffsetLine(l, DisCarLength + DisLaneWidth / 2);
                List<Line> edges = new List<Line>() { ltops[0], lbottoms[0], ltops[1], lbottoms[1] };
                List<List<Line>> segs = new List<List<Line>>();
                foreach (var e in edges)
                {
                    List<Line> seg = new List<Line>();
                    DBObjectCollection segobjs = new DBObjectCollection();
                    DivideCurveByLength(e, DisCarWidth, ref segobjs);
                    seg.AddRange(segobjs.Cast<Line>().Where(f => Math.Abs(f.GetLength() - DisCarWidth) < 1).ToList());
                    segs.Add(seg);
                }
                for (int j = 0; j < segs[0].Count; j++)
                {
                    var pla = PolyFromPoints(new List<Point3d>() {
                        segs[0][j].StartPoint,segs[0][j].EndPoint,segs[1][j].EndPoint,segs[1][j].StartPoint});
                    pla.Closed = true;
                    var plb = PolyFromPoints(new List<Point3d>() {
                        segs[2][j].StartPoint,segs[2][j].EndPoint,segs[3][j].EndPoint,segs[3][j].StartPoint});
                    plb.Closed = true;
                    var a = pla.Clone() as Polyline;
                    a.TransformBy(Matrix3d.Scaling(0.9, a.GetCenter()));
                    var b = plb.Clone() as Polyline;
                    b.TransformBy(Matrix3d.Scaling(0.9, b.GetCenter()));
                    var crosseda = ObstaclesSpatialIndex.SelectCrossingPolygon(a);
                    if (crosseda.Count == 0 && Boundary.IsPointIn(pla.GetCenter())
                        && (!IsInAnyPolys(pla.GetCenter(), laneBoxes)))
                    {
                        CarSpots.Add(pla);
                        AddToSpatialIndex(pla, ref ObstaclesSpatialIndex);
                    }
                    var crossedb = ObstaclesSpatialIndex.SelectCrossingPolygon(b);
                    if (crossedb.Count == 0 && Boundary.IsPointIn(plb.GetCenter())
                        && (!IsInAnyPolys(plb.GetCenter(), laneBoxes)))
                    {
                        CarSpots.Add(plb);
                        AddToSpatialIndex(plb, ref ObstaclesSpatialIndex);
                    }
                }
            }
        }

        /// <summary>
        /// 生成车道中心线
        /// </summary>
        private void GenerateLanes()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                int count = 0;
                while (true)
                {
                    count++;
                    if (count > 10) break;
                    int lanecount = IniLanes.Count;
                    SortLinesByLength(IniLanes, false);

                    bool generated_adjwall = false;
                    for (int i = 0; i < IniLanes.Count; i++)
                    {
                        if (IniLanes[i].Length >= LengthGenetateHalfLane)
                        {
                            var l = IniLanes[i];
                            var lines = GenerateLanesFromExistingLaneEndpoint(l, Boundary);
                            var newlane = new List<Line>();

                            for (int j = 0; j < lines.Count; j++)
                            {
                                var k = (Line)SplitCurve(lines[j], Cutters)[0];
                                Point3d ptori = j == 0 ? l.StartPoint : l.EndPoint;
                                k = MoveLaneForMoreParkingSpace(k, ptori);
                                bool hasNeighLane = false;
                                foreach (var lane in IniLanes)
                                {
                                    if (IsParallelLine(lane, k) && DisBetweenTwoParallelLines(lane, k) < DisModulus + DisCarAndHalfLane)
                                    {
                                        hasNeighLane = true;
                                        break;
                                    }
                                }
                                if (!hasNeighLane && k.Length >= LengthGenetateHalfLane && Boundary.IsPointIn(k.GetCenter()) && (!IsInAnyPolys(k.GetCenter(), Obstacles)))
                                {
                                    IniLanes.Add(k);
                                    Cutters.Add(k);
                                    generated_adjwall = true;
                                }
                            }
                            if (generated_adjwall) break;
                        }
                    }
                    if (generated_adjwall) continue;

                    bool generated_wallext = false;
                    foreach (var wall in Walls)
                    {
                        var wallpl = wall.Clone() as Polyline;
                        DBObjectCollection explodedbounds = new DBObjectCollection();
                        wallpl.Explode(explodedbounds);
                        var edges = explodedbounds.Cast<Line>().ToList().Where(e => e.Length > DisModulus).ToList();
                        SortLinesByLength(edges, false);
                        foreach (var e in edges)
                        {
                            foreach (var lane in IniLanes)
                            {
                                Point3d ponlane = lane.GetClosestPointTo(e.StartPoint, false);
                                Line ltest = new Line(ponlane, e.StartPoint);
                                var cond = IsParallelLine(ltest, e);
                                if (IsPerpLine(e, lane) && DisBetweenTwoParallelLines(e, lane) < DisModulus && e.Length > DisModulus && cond)
                                {
                                    Point3d pes = e.StartPoint.DistanceTo(ponlane) > e.EndPoint.DistanceTo(ponlane) ? e.StartPoint : e.EndPoint;
                                    Line newlane = new Line(ponlane, pes);
                                    Vector3d vec = Vector(newlane).GetPerpendicularVector().GetNormal();
                                    Point3d ptest = e.GetCenter();
                                    ptest = ptest.TransformBy(Matrix3d.Displacement(vec));
                                    if (!Boundary.IsPointIn(ptest)) vec = -vec;
                                    newlane.TransformBy(Matrix3d.Displacement(vec * DisCarAndHalfLane));
                                    Line ln = new Line();
                                    var k = SplitCurve(newlane, Cutters).Cast<Line>().ToList();
                                    if (k.Count == 0) ln = newlane;
                                    else
                                    {
                                        for (int j = 0; j < k.Count; j++)
                                        {
                                            if (k[j].Length > DisModulus && DisBetweenTwoParallelLines(k[j], lane) < 1000)
                                            {
                                                ln = k[j];
                                                break;
                                            }
                                        }
                                    }
                                    if (ln.Length > 0)
                                    {
                                        bool hasNeighLane = false;
                                        foreach (var exlane in IniLanes)
                                        {
                                            if (IsParallelLine(exlane, ln) && DisBetweenTwoParallelLines(exlane, ln) < DisModulus * 2)
                                            {
                                                hasNeighLane = true;
                                                break;
                                            }
                                        }
                                        if (!hasNeighLane && ln.Length >= LengthGenetateHalfLane)
                                        {
                                            //ln.AddToCurrentSpace();
                                            IniLanes.Add(ln);
                                            Cutters.Add(ln);
                                            generated_wallext = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (generated_wallext) break;
                        }
                        if (generated_wallext) break;
                    }
                    if (generated_wallext) continue;

                    bool generate_bblane = false;
                    SortLinesByLength(IniLanes, false);
                    List<Line> offsetest = new List<Line>();
                    foreach (var l in IniLanes)
                    {
                        offsetest.AddRange(GenerateLanesForBackBackParking(l));
                    }
                    if (offsetest.Count == 0) break;
                    SortLinesByLength(offsetest, false);
                    foreach (var e in offsetest)
                    {
                        bool hasNeighLane = false;
                        bool IsInvalidLane = false;
                        foreach (var lane in IniLanes)
                        {
                            if (IsParallelLine(lane, e) && DisBetweenTwoParallelLines(lane, e) < DisModulus - 100)
                            {
                                hasNeighLane = true;
                                break;
                            }

                        }



                        Point3d ps = new Point3d();
                        Point3d pe = new Point3d();
                        double ds = -1;
                        double de = -1;
                        int ist = -1;
                        int ie = -1;
                        ClosestPointInCurve(e.StartPoint, IniLanes.Cast<Curve>().ToList(), ref ps, ref ds, ref ist);
                        ClosestPointInCurve(e.EndPoint, IniLanes.Cast<Curve>().ToList(), ref pe, ref de, ref ie);
                        if (ds < 5000 && de < 5000 && e.Length <= DisModulus)
                        {
                            IsInvalidLane = true;
                            break;
                        }



                        if ((!hasNeighLane) && (!IsInvalidLane))
                        {
                            //e.AddToCurrentSpace();
                            IniLanes.Add(e);
                            Cutters.Add(e);
                            generate_bblane = true;
                        }
                        if (generate_bblane) break;
                    }
                    if (generate_bblane) continue;

                    if (lanecount == IniLanes.Count) break;
                }

                //extent lane
                ExtendLanesToBound();

                //connect lane
                ConnectIsolatedLanes();
            }
        }

        /// <summary>
        /// 在车道线的末端/端点生成新的车道线
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="bound"></param>
        /// <param name="bothsides"></param>
        /// <returns></returns>
        private List<Line> GenerateLanesFromExistingLaneEndpoint(Line lane, Polyline bound, bool bothsides = true)
        {
            var box = bound.CalObb();
            var objs = new DBObjectCollection();
            ((Polyline)box.Clone()).Explode(objs);
            var edges = objs.Cast<Line>().ToList();
            SortLinesByLength(edges, false);
            var maxdistance = edges[0].Length;
            var ps = lane.StartPoint;
            var pe = lane.EndPoint;
            Vector3d vec_a = Vector(lane).GetPerpendicularVector();
            var pts = new List<Point3d>() { pe };
            if (bothsides) pts.Insert(0, ps);
            List<Line> lines = new List<Line>();
            foreach (var pt in pts)
            {
                var linestmp = new List<Line>();
                Line a = LineSDL(pt, vec_a, maxdistance);
                Line b = LineSDL(pt, vec_a, -maxdistance);
                linestmp.AddRange(OffsetLine(a, DisCarAndHalfLane));
                linestmp.AddRange(OffsetLine(b, DisCarAndHalfLane));
                var center = box.GetCenter();
                SortLinesByDistanceToPoint(linestmp, center);
                lines.Add(linestmp[0]);
            }
            return lines;
        }

        /// <summary>
        /// 移动在新旧车道线区内有更长墙线的车道线至以更长的墙边为起始边
        /// </summary>
        /// <returns></returns>
        private Line MoveLaneForMoreParkingSpace(Line lane, Point3d ptori)
        {
            Vector3d vec = Vector(lane.GetClosestPointTo(ptori, false), ptori);
            var k_toini = lane.Clone() as Line;
            k_toini.TransformBy(Matrix3d.Displacement(vec));
            Polyline ply = PolyFromPoints(new List<Point3d>() { lane.StartPoint, lane.EndPoint, k_toini.EndPoint, k_toini.StartPoint, lane.StartPoint });
            var splited = SplitCurve(Boundary, new DBObjectCollection() { ply });
            Extents3d ext = ply.GeometricExtents;
            foreach (var split in splited)
            {
                if (ext.IsPointIn(split.GetCenter()))
                {
                    DBObjectCollection edgeobjs = new DBObjectCollection();
                    split.Explode(edgeobjs);
                    bool quit = false;
                    foreach (var edge in edgeobjs)
                    {
                        var cond = IsParallelLine(lane, (Line)edge);
                        if (cond && ((Line)edge).Length >= ((Line)(lane)).Length / 2)
                        {
                            var lnew = lane.Clone() as Line;
                            Point3d pt_on_k = ((Line)lane).GetClosestPointTo(((Line)edge).GetCenter(), false);
                            Point3d pt_on_edge = ((Line)edge).GetClosestPointTo(pt_on_k, false);
                            Vector3d vec_to_edge = Vector(pt_on_k, pt_on_edge);
                            lnew.TransformBy(Matrix3d.Displacement(vec_to_edge));
                            Vector3d vec_to_new = -vec_to_edge.GetNormal() * DisCarAndLane;
                            lnew.TransformBy(Matrix3d.Displacement(vec_to_new));
                            var s = SplitCurve(lnew, Cutters);
                            if (s.Count == 0)
                            {
                                lnew.TransformBy(Matrix3d.Displacement(vec_to_edge.GetNormal() * DisLaneWidth / 2));
                                lane = lnew;
                                quit = true;
                                break;
                            }
                            else
                            {
                                foreach (var ed in s)
                                {
                                    var d = (Line)ed;
                                    if (Math.Abs(d.Length - lnew.Length) < 1)
                                    {
                                        d.TransformBy(Matrix3d.Displacement(vec_to_edge.GetNormal() * DisLaneWidth / 2));
                                        lane = d;
                                        quit = true;
                                        break;
                                    }
                                }
                                if (quit) break;
                            }
                        }
                    }
                    if (quit) break;
                }
            }
            return lane;
        }

        /// <summary>
        /// 生成完整模块的车道线
        /// </summary>
        /// <param name="lane"></param>
        private List<Line> GenerateLanesForBackBackParking(Line lane)
        {
            var offsetedtest = OffsetLine(lane, DisModulus + DisLaneWidth / 2);
            List<Line> offseted = new List<Line>();
            List<Line> results = new List<Line>();
            foreach (var l in offsetedtest)
            {
                var splited = SplitCurve(l, Cutters);
                if (splited.Count == 0) splited.Add(l);
                splited = splited.Where(e => Boundary.IsPointIn(e.GetCenter()))
                    .Where(e => e.GetLength() > LengthGenetateModuleLane)
                    .Where(e => Math.Abs(e.GetLength() - DisModulus) > 1)
                    .Where(e => !IsInAnyPolys(e.GetCenter(), Obstacles)).ToList();
                foreach (var s in splited)
                {
                    Point3d ps = lane.GetClosestPointTo(s.StartPoint, false);
                    Point3d pe = lane.GetClosestPointTo(s.EndPoint, false);
                    var vec = Vector(ps, s.StartPoint).GetNormal() * DisModulus;
                    Line r = new Line(ps, pe);
                    r.TransformBy(Matrix3d.Displacement(vec));
                    offseted.Add(r);
                }
            }
            foreach (var l in offseted)
            {
                var splited = SplitCurve(l, Cutters);
                if (splited.Count == 0) splited.Add(l);
                splited = splited.Where(e => Boundary.IsPointIn(e.GetCenter()))
                    .Where(e => e.GetLength() > LengthGenetateModuleLane)
                    .Where(e => Math.Abs(e.GetLength() - DisModulus) > 1)
                    .Where(e => !IsInAnyPolys(e.GetCenter(), Obstacles)).ToList();
                results.AddRange(splited.Cast<Line>().ToList());
            }
            return results;
        }

        /// <summary>
        /// 延伸一些中断的车道线至边界上
        /// </summary>
        private void ExtendLanesToBound()
        {
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var l = IniLanes[i];
                var pls = Cutters.Cast<Curve>().ToList();
                pls.Remove(l);
                var ps = new Point3d();
                var pe = new Point3d();
                double ds = -1;
                double de = -1;
                int ist = -1;
                int ie = -1;
                ClosestPointInCurve(l.StartPoint, pls, ref ps, ref ds, ref ist);
                ClosestPointInCurve(l.EndPoint, pls, ref pe, ref de, ref ie);
                if (ds > 1)
                {
                    Line ls = LineSDL(l.StartPoint, Vector(l.EndPoint, l.StartPoint), 100000);
                    var rs = new Line();
                    foreach (var k in SplitCurve(ls, Cutters))
                    {
                        if (k.GetClosestPointTo(l.StartPoint, false).DistanceTo(l.StartPoint) < 1) rs = (Line)k;
                    }
                    var ply = JoinCurves(new List<Polyline>(), new List<Line>() { rs, l })[0];
                    IniLanes[i] = new Line(ply.StartPoint, ply.EndPoint);
                    Cutters.Remove(l);
                    Cutters.Add(IniLanes[i]);
                }
                if (de > 1)
                {
                    Line le = LineSDL(l.EndPoint, Vector(l.StartPoint, l.EndPoint), 100000);
                    var re = new Line();
                    foreach (var k in SplitCurve(le, Cutters))
                    {
                        if (k.GetClosestPointTo(l.EndPoint, false).DistanceTo(l.EndPoint) < 1) re = (Line)k;
                    }
                    var ply = JoinCurves(new List<Polyline>(), new List<Line>() { l, re })[0];
                    IniLanes[i] = new Line(ply.StartPoint, ply.EndPoint);
                    Cutters.Remove(l);
                    Cutters.Add(IniLanes[i]);
                }
            }
        }

        /// <summary>
        /// 连接孤立的车道线
        /// </summary>
        private void ConnectIsolatedLanes()
        {
            int c = 0;
            while (true)
            {
                c++;
                if (c > 10) break;
                bool found = false;
                foreach (var l in IniLanes)
                {
                    var lc = Cutters.Cast<Curve>().ToList();
                    lc.Remove(l);
                    var ps = new Point3d();
                    var pe = new Point3d();
                    double ds = -1;
                    double de = -1;
                    int ist = -1;
                    int ie = -1;
                    ClosestPointInCurve(l.StartPoint, lc, ref ps, ref ds, ref ist);
                    ClosestPointInCurve(l.EndPoint, lc, ref pe, ref de, ref ie);
                    if (ds > DisLaneWidth && de > DisLaneWidth)
                    {
                        found = true;
                        lc = lc.Where(e => IsParallelLine(l, (Line)e)).Cast<Curve>().ToList();
                        ClosestPointInCurve(l.StartPoint, lc, ref ps, ref ds, ref ist);
                        Line lx = new Line(l.StartPoint, ps);
                        lx.TransformBy(Matrix3d.Displacement(Vector(l.StartPoint, l.EndPoint).GetNormal() * DisLaneWidth / 2));
                        IniLanes.Add(lx);
                        break;
                    }
                }
                if (!found) break;
            }
        }
    }

    public static class GeoUtilities
    {
        public static void SortLinesByLength(List<Line> lines, bool ascending = true)
        {
            var comparer = new LineLengthComparer();
            lines.Sort(comparer);
            if (!ascending) lines.Reverse();
            return;
        }
        private class LineLengthComparer : IComparer<Line>
        {
            public LineLengthComparer()
            {

            }
            public int Compare(Line a, Line b)
            {
                if (a.Length == b.Length) return 0;
                else if (a.Length < b.Length) return -1;
                else return 1;
            }
        }

        public static void SortLinesByDistanceToPoint(List<Line> lines, Point3d point)
        {
            var comparer = new LineDisToPointComparer(point);
            lines.Sort(comparer);
            return;
        }
        private class LineDisToPointComparer : IComparer<Line>
        {
            public LineDisToPointComparer(Point3d pt)
            {
                Pt = pt;
            }
            private Point3d Pt;
            public int Compare(Line a, Line b)
            {
                var disa = a.GetClosestPointTo(Pt, false).DistanceTo(Pt);
                var disb = b.GetClosestPointTo(Pt, false).DistanceTo(Pt);
                if (disa == disb) return 0;
                else if (disa < disb) return -1;
                else return 1;
            }
        }

        private static void SortAlongCurve(List<Point3d> points, Curve curve)
        {
            var comparer = new PointAlongCurveComparer(curve);
            points.Sort(comparer);
            return;
        }
        private class PointAlongCurveComparer : IComparer<Point3d>
        {
            public PointAlongCurveComparer(Curve curve)
            {
                Curve = curve;
            }
            private Curve Curve;
            public int Compare(Point3d a, Point3d b)
            {
                var param_a = Curve.GetParamAtPointX(Curve.GetClosestPointTo(a, false));
                var param_b = Curve.GetParamAtPointX(Curve.GetClosestPointTo(b, false));
                if (param_a == param_b) return 0;
                else if (param_a < param_b) return -1;
                else return 1;
            }
        }

        public static Polyline PolyFromLine(Line a)
        {
            Polyline p = new Polyline();
            p.AddVertexAt(0, a.StartPoint.ToPoint2d(), 0, 0, 0);
            p.AddVertexAt(1, a.EndPoint.ToPoint2d(), 0, 0, 0);
            return p;
        }

        public static Line LineSDL(Point3d start, Vector3d vec, double length)
        {
            var v = vec.GetNormal() * length;
            var pe = start.TransformBy(Matrix3d.Displacement(v));
            return new Line(start, pe);
        }

        public static Vector3d Vector(Line line)
        {
            return Vector(line.StartPoint, line.EndPoint);
        }

        public static Vector3d Vector(Point3d ps, Point3d pe)
        {
            return new Vector3d(pe.X - ps.X, pe.Y - ps.Y, pe.Z - ps.Z);
        }

        public static Polyline PolyFromPoints(List<Point3d> points)
        {
            Polyline p = new Polyline();
            for (int i = 0; i < points.Count; i++)
            {
                p.AddVertexAt(i, points[i].ToPoint2d(), 0, 0, 0);
            }
            return p;
        }

        public static List<Polyline> JoinCurves(List<Polyline> plys, List<Line> lines, double tol = 1)
        {
            List<Polyline> pls = new List<Polyline>();
            pls.AddRange(plys);
            lines.ForEach(e => pls.Add(PolyFromLine(e)));
            List<Polyline> result = new List<Polyline>();
            if (pls.Count == 0) return result;
            result.Add(pls[0]);
            pls.RemoveAt(0);
            while (pls.Count > 0)
            {
                bool quit = false;
                for (int i = 0; i < pls.Count; i++)
                {
                    Point3d pe = result[result.Count - 1].EndPoint;

                    if (pls[i].GetClosestPointTo(pe, false).DistanceTo(pe) <= tol)
                    {
                        if (pls[i].EndPoint.DistanceTo(pe) <= tol) pls[i].ReverseCurve();
                        result[result.Count - 1] = result[result.Count - 1].PolyJoin(pls[i]);
                        pls.RemoveAt(i);
                        quit = true;
                        break;
                    }
                    Point3d ps = result[result.Count - 1].StartPoint;
                    if (pls[i].GetClosestPointTo(ps, false).DistanceTo(ps) <= tol)
                    {
                        if (pls[i].StartPoint.DistanceTo(ps) <= tol) pls[i].ReverseCurve();
                        result[result.Count - 1] = pls[i].PolyJoin(result[result.Count - 1]);
                        pls.RemoveAt(i);
                        quit = true;
                        break;
                    }
                }
                if (quit) continue;
                result.Add(pls[0]);
                pls.RemoveAt(0);
            }
            return result;
        }

        public static List<Line> OffsetLine(Line a, double dis)
        {
            var vec_a = Vector(a).GetPerpendicularVector().GetNormal() * dis;
            var la = (Line)a.Clone();
            var lb = (Line)a.Clone();
            la.TransformBy(Matrix3d.Displacement(vec_a));
            lb.TransformBy(Matrix3d.Displacement(-vec_a));
            return new List<Line>() { la, lb };
        }

        public static List<Curve> SplitCurve(Curve curve, DBObjectCollection objs)
        {
            List<Point3d> pts = new List<Point3d>();
            objs.Cast<Entity>().ToList().ForEach(e => pts.AddRange(curve.Intersect(e, Intersect.OnBothOperands)));
            pts = RemoveDuplicatePts(pts, 1);
            for (int i = 0; i < pts.Count; i++)
            {
                if (curve.GetClosestPointTo(pts[i], false).DistanceTo(pts[i]) > 0)
                {
                    pts.RemoveAt(i);
                    i--;
                }
            }
            if (pts.Count > 0)
            {
                SortAlongCurve(pts, curve);
                Point3dCollection ps = new Point3dCollection(pts.ToArray());
                var splited = curve.GetSplitCurves(ps);
                return splited.Cast<Curve>().ToArray().ToList();
            }
            else return new List<Curve>() { curve };
        }

        public static List<Point3d> RemoveDuplicatePts(List<Point3d> points, double tol = 0)
        {
            List<Point3d> results = new List<Point3d>(points);
            for (int i = 1; i < results.Count; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (results[i].DistanceTo(results[j]) <= tol)
                    {
                        results.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
            return results;
        }

        public static bool IsParallelLine(Line a, Line b, double degreetol = 1)
        {
            double angle = Vector((Line)a).GetAngleTo(Vector((Line)b));
            return Math.Min(angle, Math.Abs(Math.PI - angle)) / Math.PI * 180 < degreetol;
        }

        public static bool IsPerpLine(Line a, Line b, double degreetol = 1)
        {
            double angle = Vector((Line)a).GetAngleTo(Vector((Line)b));
            return Math.Abs(Math.Min(angle, Math.Abs(Math.PI * 2 - angle)) / Math.PI * 180 - 90) < degreetol;
        }

        public static double DisBetweenTwoParallelLines(Line a, Line b)
        {
            Point3d pt_on_a = a.GetClosestPointTo(b.GetCenter(), false);
            Point3d pt_on_b = b.GetClosestPointTo(pt_on_a, false);
            return pt_on_a.DistanceTo(pt_on_b);
        }

        public static bool IsInAnyPolys(Point3d pt, List<Polyline> pls)
        {
            foreach (var p in pls)
            {
                if (p.IsPointIn(pt)) return true;
            }
            return false;
        }

        public static void ClosestPointInCurve(Point3d pt, List<Curve> crvs,
            ref Point3d result, ref double dis, ref int index)
        {
            if (crvs.Count == 0) return;
            result = crvs[0].GetClosestPointTo(pt, false);
            dis = result.DistanceTo(pt);
            index = 0;
            if (crvs.Count == 1) return;
            for (int i = 1; i < crvs.Count; i++)
            {
                var p = crvs[i].GetClosestPointTo(pt, false);
                var d = p.DistanceTo(pt);
                if (d < dis)
                {
                    dis = d;
                    index = i;
                    result = p;
                }
            }
            return;
        }

        public static Point3dCollection DivideCurveByLength(Curve crv, double length, ref DBObjectCollection segs)
        {
            Point3dCollection pts = new Point3dCollection(crv.GetPointsByDist(length).ToArray());
            segs = crv.GetSplitCurves(pts);
            return pts;
        }

        public static void AddToSpatialIndex(Entity e, ref ThCADCoreNTSSpatialIndex spatialIndex)
        {
            DBObjectCollection add = new DBObjectCollection();
            add.Add(e);
            spatialIndex.Update(add, new DBObjectCollection());
            return;
        }
    }
}
