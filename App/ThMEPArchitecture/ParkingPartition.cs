using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using static ThMEPArchitecture.GeoUtilities;

namespace ThMEPArchitecture
{
    public class ParkingPartition
    {
        public ParkingPartition(List<Polyline> walls, List<Line> iniLanes,
            List<Polyline> obstacles, Polyline boundary)
        {
            Walls = walls;
            IniLanes = new List<Line>(iniLanes);
            Obstacles = obstacles;
            Boundary = boundary;
            SequenceLanes = new List<Line>(iniLanes);

        }
        public List<Polyline> Walls;
        public List<Line> IniLanes;
        public List<Line> SequenceLanes;
        public List<Polyline> Obstacles;
        public Polyline Boundary;
        public DBObjectCollection Cutters = new DBObjectCollection();
        public ThCADCoreNTSSpatialIndex ObstaclesSpatialIndex;
        private List<Polyline> CarSpots = new List<Polyline>();
        private List<ParkModule> Modules = new List<ParkModule>();

        const double DisLaneWidth = 5500;
        const double DisCarLength = 5100;
        const double DisCarWidth = 2400;
        const double DisCarAndHalfLane = DisLaneWidth / 2 + DisCarLength;
        const double DisCarAndLane = DisLaneWidth + DisCarLength;
        const double DisModulus = DisCarAndHalfLane * 2;

        //custom
        const double LengthGenetateHalfLane = 10600;
        const double LengthGenetateModuleLane = 10600;

        /// <summary>
        ///  验证数据有效性并进行接口转换
        /// </summary>
        /// <returns></returns>
        public bool Validate()
        {
            if (IniLanes.Count == 0) return false;
            GenerateWallsForInput();
            return true;
        }

        /// <summary>
        /// 从输入端的闭合边界提取出墙线
        /// </summary>
        private void GenerateWallsForInput()
        {
            var splited = new DBObjectCollection();
            var p = Boundary.Clone() as Polyline;
            p.Explode(splited);
            var ls = splited.Cast<Line>().ToList();
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
            Walls = JoinCurves(new List<Polyline>(), ls);
            splited.Dispose();         
        }

        /// <summary>
        /// 生成离线实时图形数据可视化调试进程
        /// </summary>
        /// <param name="path"></param>
        public void Log(string path)
        {
            string w = "";
            string l = "";
            foreach (var e in Walls)
            {
                foreach (var pt in e.Vertices().Cast<Point3d>().ToList())
                    w += pt.X.ToString() + "," + pt.Y.ToString() + ",";
            }
            foreach (var e in IniLanes)
            {
                l += e.StartPoint.X.ToString() + "," + e.StartPoint.Y.ToString() + ","
                    + e.EndPoint.X.ToString() + "," + e.EndPoint.Y.ToString() + ",";
            }

            FileStream fs1 = new FileStream(path, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs1);
            sw.WriteLine(w);
            sw.WriteLine(l);
            sw.Close();
            fs1.Close();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            Walls.ForEach(e => Cutters.Add(e));
            IniLanes.ForEach(e => Cutters.Add(e));
            Obstacles.ForEach(e => Cutters.Add(e));
            ObstaclesSpatialIndex = new ThCADCoreNTSSpatialIndex(Cutters);
        }

        /// <summary>
        /// 计算停车位数量
        /// </summary>
        /// <returns></returns>
        public int CalNumOfParkingSpaces()
        {
            GenerateParkingSpaces();
            return CarSpots.Count;
        }

        /// <summary>
        /// 显示车位排布结果
        /// </summary>
        public void Display()
        {
            GenerateParkingSpaces();
            CarSpots.AddToCurrentSpace();
            //foreach (var e in CarSpots)
            //{
            //    try
            //    {
            //        e.AddToCurrentSpace();
            //    }
            //    catch { }
            //}
        }

        /// <summary>
        /// 主函数
        /// </summary>
        public void GenerateParkingSpaces()
        {
            GenerateLanes();

            

            GenerateParkingSpots();

            //IniLanes.AddToCurrentSpace();
            CarSpots.ForEach(e => e.ColorIndex = 250);
            //CarSpots.AddToCurrentSpace();
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
                    if (count > 20) break;
                    int lanecount = IniLanes.Count;
                    SortLinesByLength(IniLanes, false);

                    //
                    var generate_adjwall = GenerateLanesAdjWall();
                    if (generate_adjwall) continue;

                    var generated_wallext = GenerateLanesExtWall();
                    if (generated_wallext) continue;

                    SortLinesByLength(IniLanes, false);
                    List<Line> offsetest = new List<Line>();
                    List<Line> offseini = new List<Line>();
                    foreach (var l in IniLanes)
                    {
                        var lis = GenerateLanesForBackBackParking(l);
                        offsetest.AddRange(lis);
                        if (lis.Count > 0) offseini.Add(l);
                    }

                    if (offsetest.Count == 0) break;
                    SortLinesByLength(offsetest, false);

                    var generate_bblane = GenerateLanesBackToBack(offsetest, offseini);
                    if (generate_bblane) continue;

                    if (lanecount == IniLanes.Count) break;
                }

                //extent lane
                ExtendLanesToBound();

                //connect lane
                ConnectIsolatedLanes();

                SplitAndRemoveLanesNearObstacles();

            }
        }

        private List<Line> RemoveCollisionalLanesForObstacles(Line lane)
        {
            var l = lane;
            var bf = l.Buffer(DisLaneWidth / 2);
            var obstalesobjs = new DBObjectCollection();
            Obstacles.ForEach(o => obstalesobjs.Add(o));
            var splited = SplitCurve(bf, obstalesobjs);
            List<Point3d> pts = new List<Point3d>();
            foreach (var s in splited)
            {
                pts.Add(s.StartPoint);
                pts.Add(s.EndPoint);
            }
            pts = pts.Distinct().ToList();
            List<Point3d> ps = new List<Point3d>();
            pts.ForEach(t => ps.Add(l.GetClosestPointTo(t, false)));
            SortAlongCurve(ps, l);
            var segs = l.GetSplitCurves(ps.ToCollection()).Cast<Line>().ToList();
            segs = segs.Where(e => e.Length > 1).ToList();
            List<Line> res = new List<Line>();
            if (segs.Count > 1)
            {
                res.Add(segs[0]);
                res.Add(segs[segs.Count - 1]);
            }
            else if (segs.Count == 1)
            {
                res.Add(segs[0]);
            }
            res = res.Where(e => e.Length > DisModulus).ToList();
            obstalesobjs.Dispose();
            pts.Clear();
            ps.Clear();
            segs.Clear();
            return res;
        }

        /// <summary>
        /// 生成车道两端的新车道线
        /// </summary>
        /// <returns></returns>
        private bool GenerateLanesAdjWall()
        {
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
                        Point3d ptori;
                        Point3d pto;
                        if (lines.Count > 1) ptori = j == 0 ? l.StartPoint : l.EndPoint;
                        else
                        {
                            var dis = l.StartPoint.DistanceTo(lines[j].StartPoint);
                            ptori = Math.Abs(dis - DisCarAndHalfLane) < 1 ? l.StartPoint : l.EndPoint;
                        }
                        pto = ptori.DistanceTo(l.StartPoint) < 1 ? l.EndPoint : l.StartPoint;

 
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
                        if (!hasNeighLane && k.Length >= LengthGenetateHalfLane && Boundary.IsPointIn(k.GetCenter()) && (!IsInAnyPolys(k.GetCenter(), Obstacles))
                            && k.Length > DisModulus)
                        {
                            //lines[j].AddToCurrentSpace();



                            //IniLanes = IniLanes.Distinct().ToList();

                            //var res = SplitLaneWithNearObstacles(k);
                            //res = res.Where(e => e.Length >= 10).ToList();

                            //if (res.Count > 1)
                            //{
                            //    if (res[0].Length >= DisModulus) k = res[0];
                            //    else k = new Line();
                            //}
                            //if (k.Length > DisModulus)
                            //{
                            //    Line li = new Line(k.StartPoint, k.EndPoint);
                            //    Vector3d vec_m = Vector(pto, ptori).GetNormal() * DisCarAndHalfLane;
                            //    li.TransformBy(Matrix3d.Displacement(vec_m));
                            //    ParkModule pm = new ParkModule();
                            //    pm.Lanes = new Line[] { k, li };
                            //    pm.LayoutMode = ((int)LayoutMode.SingleVert);
                            //    Modules.Add(pm);

                            //    IniLanes.Add(k);
                            //    SequenceLanes.Add(k);
                            //    Cutters.Add(k);
                            //    generated_adjwall = true;
                            //}


                            Line li = new Line(k.StartPoint, k.EndPoint);
                            Vector3d vec_m = Vector(pto, ptori).GetNormal() * DisCarAndHalfLane;
                            li.TransformBy(Matrix3d.Displacement(vec_m));
                            ParkModule pm = new ParkModule();
                            pm.Lanes = new Line[] { k, li };
                            pm.LayoutMode = ((int)LayoutMode.SingleVert);
                            Modules.Add(pm);

                            IniLanes.Add(k);
                            SequenceLanes.Add(k);
                            Cutters.Add(k);
                            generated_adjwall = true;



                        }
                    }
                    if (generated_adjwall) break;
                }
            }
            return generated_adjwall;
        }

        /// <summary>
        /// 生成靠墙单排的车道线
        /// </summary>
        /// <returns></returns>
        private bool GenerateLanesExtWall()
        {
            bool generated_wallext = false;
            foreach (var wall in Walls)
            {
                var wallpl = wall.Clone() as Polyline;
                DBObjectCollection explodedbounds = new DBObjectCollection();
                wallpl.Explode(explodedbounds);
                var edges = explodedbounds.Cast<Line>().ToList().Where(e => e.Length > DisModulus).ToList();
                SortLinesByLength(edges, false);

                explodedbounds.Dispose();
                wallpl.Dispose();

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
                            k.Clear();
                            if (ln.Length > 0)
                            {
                                bool hasNeighLane = false;
                                foreach (var exlane in IniLanes)
                                {
                                    if (IsParallelLine(exlane, ln) && DisBetweenTwoParallelLines(exlane, ln) < DisModulus/* * 2*/
                                        && GetLengthDifferentFromParallelBofA(ln, exlane) < DisModulus)
                                    {
                                        hasNeighLane = true;
                                        break;
                                    }
                                }
                                if (!hasNeighLane && ln.Length >= LengthGenetateHalfLane)
                                {
                                    var ps = e.GetClosestPointTo(ln.StartPoint, true);
                                    var pe = e.GetClosestPointTo(ln.EndPoint, true);
                                    var lnn = new Line(ps, pe);
                                    ParkModule pm = new ParkModule();
                                    pm.Lanes = new Line[] { ln, lnn };
                                    pm.LayoutMode = ((int)LayoutMode.SingleVert);
                                    Modules.Add(pm);

                                    IniLanes.Add(ln);
                                    SequenceLanes.Add(ln);
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
            return generated_wallext;
        }

        /// <summary>
        /// 按双排模数生成车道线
        /// </summary>
        /// <returns></returns>
        private bool GenerateLanesBackToBack(List<Line> offsetest, List<Line> offsetini)
        {
            bool generate_bblane = false;
 
            foreach (var ef in offsetest)
            {
                var res = RemoveCollisionalLanesForObstacles(ef);
                int tmpcout = IniLanes.Count;
                foreach (var e in res)
                {
                    bool hasNeighLane = false;
                    bool IsInvalidLane = false;
                    foreach (var lane in IniLanes)
                    {
                        if (IsParallelLine(lane, e) && (DisBetweenTwoParallelLines(e, lane) < DisModulus - 100
                            && GetLengthDifferentFromParallelBofA(e, lane) < DisModulus))
                        {
                            hasNeighLane = true;
                            GetLengthDifferentFromParallelBofA(e, lane);
                            break;
                        }
                    }
                    var ds = ClosestPointInCurves(e.StartPoint, IniLanes.Cast<Curve>().ToList());
                    var de = ClosestPointInCurves(e.EndPoint, IniLanes.Cast<Curve>().ToList());
                    if (ds < 5000 && de < 5000 && e.Length <= DisModulus * 2)
                    {
                        IsInvalidLane = true;
                        break;
                    }
                    if ((!hasNeighLane) && (!IsInvalidLane) && e.Length > DisModulus)
                    {

                        Line lini = new Line();
                        foreach (var ini in offsetini)
                        {
                            var cd1 = Math.Abs(DisBetweenTwoParallelLines(ini, e) - DisModulus) < 1;
                            var cd2 = GetLengthDifferentFromParallelBofA(e, ini) < 1;
                            if (IsParallelLine(ini, e) && cd1)
                            {
                                lini = ini;
                                break;
                            }
                        }
                        var ps = lini.GetClosestPointTo(e.StartPoint, false);
                        var pe = lini.GetClosestPointTo(e.EndPoint, false);
                        var le = new Line(ps, pe);

                        if (e.Length > DisCarAndHalfLane && le.Length > DisCarAndHalfLane)
                        {

                            ParkModule pm = new ParkModule();
                            pm.Lanes = new Line[] { e, le };
                            pm.LayoutMode = ((int)LayoutMode.DoubleVert);
                            Modules.Add(pm);
                        }

 

                        IniLanes.Add(e);
                        SequenceLanes.Add(e);
                        Cutters.Add(e);
                        generate_bblane = true;
                    }
                }
                if (tmpcout != IniLanes.Count) generate_bblane = true;

                if (generate_bblane) break;
            }
            return generate_bblane;
        }

        /// <summary>
        /// 生成停车位
        /// </summary>
        private void GenerateParkingSpots()
        {
            var cplys = new List<Polyline>(Obstacles);
            cplys.ForEach(e => e.Closed = true);
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
            List<Polyline> pcars = new List<Polyline>();
            var mtwo = Modules.Where(e => e.LayoutMode == ((int)LayoutMode.DoubleVert)).ToList();
            var mone = Modules.Where(e => e.LayoutMode == ((int)LayoutMode.SingleVert)).ToList();
            foreach (var pm in mtwo)
            {
                var la = pm.Lanes[0];
                //la.AddToCurrentSpace();
                var lb = pm.Lanes[1];
                var lanesclonea = IniLanes.Clone().ToList();
                var lanescloneb = IniLanes.Clone().ToList();
                lanesclonea.Remove(la);
                lanescloneb.Remove(lb);
                var ase = Vector(la.StartPoint, la.EndPoint).GetNormal() * DisLaneWidth / 2;
                var bse = Vector(lb.StartPoint, lb.EndPoint).GetNormal() * DisLaneWidth / 2;
                if (ClosestPointInCurves(la.StartPoint, lanesclonea.Cast<Curve>().ToList()) < 10)
                    la = new Line(la.StartPoint.TransformBy(Matrix3d.Displacement(ase)), la.EndPoint);
                if (ClosestPointInCurves(la.EndPoint, lanesclonea.Cast<Curve>().ToList()) < 10)
                    la = new Line(la.StartPoint, la.EndPoint.TransformBy(Matrix3d.Displacement(-ase)));
                if (ClosestPointInCurves(lb.StartPoint, lanescloneb.Cast<Curve>().ToList()) < 10)
                    lb = new Line(lb.StartPoint.TransformBy(Matrix3d.Displacement(bse)), lb.EndPoint);
                if (ClosestPointInCurves(lb.EndPoint, lanescloneb.Cast<Curve>().ToList()) < 10)
                    lb = new Line(lb.StartPoint, lb.EndPoint.TransformBy(Matrix3d.Displacement(-bse)));

                var vec_ab = Vector(la.GetClosestPointTo(lb.GetCenter(), true), lb.GetCenter()).GetNormal();

                var labottom = new Line(la.StartPoint, la.EndPoint);
                var latop = new Line(la.StartPoint, la.EndPoint);
                labottom.TransformBy(Matrix3d.Displacement(vec_ab * DisLaneWidth / 2));
                latop.TransformBy(Matrix3d.Displacement(vec_ab * DisModulus / 2));

                var lbbottom = new Line(lb.StartPoint, lb.EndPoint);
                var lbtop = new Line(lb.StartPoint, lb.EndPoint);
                lbbottom.TransformBy(Matrix3d.Displacement(-vec_ab * DisLaneWidth / 2));
                lbtop.TransformBy(Matrix3d.Displacement(-vec_ab * DisModulus / 2));

                List<Line> edges = new List<Line>() { latop, labottom, lbtop, lbbottom };
                List<List<Line>> segs = new List<List<Line>>();
                foreach (var e in edges)
                {
                    List<Line> seg = new List<Line>();
                    DBObjectCollection segobjs = new DBObjectCollection();

                    try
                    {
                        DivideCurveByLength(e, DisCarWidth, ref segobjs);
                    }
                    catch
                    {
                        ;
                    }
                    seg.AddRange(segobjs.Cast<Line>().Where(f => Math.Abs(f.GetLength() - DisCarWidth) < 1).ToList());
                    segs.Add(seg);
                }
                for (int j = 0; j < segs[0].Count; j++)
                {
                    var pla = PolyFromPoints(new List<Point3d>() {
                        segs[0][j].StartPoint,segs[0][j].EndPoint,segs[1][j].EndPoint,segs[1][j].StartPoint});
                    pla.Closed = true;
                    var a = pla.Clone() as Polyline;
                    a.TransformBy(Matrix3d.Scaling(0.9, a.GetCenter()));
                    var crosseda = ObstaclesSpatialIndex.SelectCrossingPolygon(a);
                    if (crosseda.Count == 0 && Boundary.IsPointIn(pla.GetCenter())
                        && (!IsInAnyPolys(pla.GetCenter(), laneBoxes))
                        && (!IsInAnyPolys(pla.GetCenter(), cplys)))
                    {
                        pcars.Add(pla);
                        CarSpots.Add(pla);
                        AddToSpatialIndex(pla, ref ObstaclesSpatialIndex);
                    }
                }
                for (int j = 0; j < segs[2].Count; j++)
                {

                    var plb = PolyFromPoints(new List<Point3d>() {
                        segs[2][j].StartPoint,segs[2][j].EndPoint,segs[3][j].EndPoint,segs[3][j].StartPoint});
                    plb.Closed = true;

                    var b = plb.Clone() as Polyline;
                    b.TransformBy(Matrix3d.Scaling(0.9, b.GetCenter()));

                    var crossedb = ObstaclesSpatialIndex.SelectCrossingPolygon(b);
                    if (crossedb.Count == 0 && Boundary.IsPointIn(plb.GetCenter())
                        && (!IsInAnyPolys(plb.GetCenter(), laneBoxes))
                         && (!IsInAnyPolys(plb.GetCenter(), cplys)))
                    {
                        pcars.Add(plb);
                        //CarSpots.Add(plb);
                        AddToSpatialIndex(plb, ref ObstaclesSpatialIndex);
                    }
                }
            }
            foreach (var pm in mone)
            {
                var la = pm.Lanes[0];
                //la.AddToCurrentSpace();
                var lb = pm.Lanes[1];
                var lanesclonea = IniLanes.Clone().ToList();
                lanesclonea.Remove(la);
                var ase = Vector(la.StartPoint, la.EndPoint).GetNormal() * DisLaneWidth / 2;
                if (ClosestPointInCurves(la.StartPoint, lanesclonea.Cast<Curve>().ToList()) < 10)
                    la = new Line(la.StartPoint.TransformBy(Matrix3d.Displacement(ase)), la.EndPoint);
                if (ClosestPointInCurves(la.EndPoint, lanesclonea.Cast<Curve>().ToList()) < 10)
                    la = new Line(la.StartPoint, la.EndPoint.TransformBy(Matrix3d.Displacement(-ase)));

                var vec_ab = Vector(la.GetClosestPointTo(lb.GetCenter(), true), lb.GetCenter()).GetNormal();

                var labottom = new Line(la.StartPoint, la.EndPoint);
                var latop = new Line(la.StartPoint, la.EndPoint);
                labottom.TransformBy(Matrix3d.Displacement(vec_ab * DisLaneWidth / 2));
                latop.TransformBy(Matrix3d.Displacement(vec_ab * DisModulus / 2));


                List<Line> edges = new List<Line>() { latop, labottom };
                List<List<Line>> segs = new List<List<Line>>();
                foreach (var e in edges)
                {
                    List<Line> seg = new List<Line>();
                    DBObjectCollection segobjs = new DBObjectCollection();
                    //DivideCurveByLength(e, DisCarWidth, ref segobjs);

                    try
                    {
                        DivideCurveByLength(e, DisCarWidth, ref segobjs);
                    }
                    catch
                    {
                        ;
                    }

                    seg.AddRange(segobjs.Cast<Line>().Where(f => Math.Abs(f.GetLength() - DisCarWidth) < 1).ToList());
                    segs.Add(seg);
                   
                }
                //segs.ForEach(e => e.AddToCurrentSpace());
                for (int j = 0; j < segs[0].Count; j++)
                {
                    var pla = PolyFromPoints(new List<Point3d>() {
                        segs[0][j].StartPoint,segs[0][j].EndPoint,segs[1][j].EndPoint,segs[1][j].StartPoint});
                    pla.Closed = true;
                    var a = pla.Clone() as Polyline;
                    a.TransformBy(Matrix3d.Scaling(0.9, a.GetCenter()));
                    var crosseda = ObstaclesSpatialIndex.SelectCrossingPolygon(a);
                    if (crosseda.Count == 0 && Boundary.IsPointIn(pla.GetCenter())
                        && (!IsInAnyPolys(pla.GetCenter(), laneBoxes))
                        && (!IsInAnyPolys(pla.GetCenter(), cplys)))
                    {               
                        pcars.Add(pla);
                        CarSpots.Add(pla);
                        AddToSpatialIndex(pla, ref ObstaclesSpatialIndex);
                    }
                }

            }
            //pcars.AddToCurrentSpace();
            cplys.AddRange(pcars);

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
                    //DivideCurveByLength(e, DisCarWidth, ref segobjs);

                    try
                    {
                        DivideCurveByLength(e, DisCarWidth, ref segobjs);
                    }
                    catch
                    {
                        ;
                    }

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
                        && (!IsInAnyPolys(pla.GetCenter(), laneBoxes))
                        && (!IsInAnyPolys(pla.GetCenter(), cplys)))
                    {
                        CarSpots.Add(pla);
                        AddToSpatialIndex(pla, ref ObstaclesSpatialIndex);
                    }
                    var crossedb = ObstaclesSpatialIndex.SelectCrossingPolygon(b);
                    if (crossedb.Count == 0 && Boundary.IsPointIn(plb.GetCenter())
                        && (!IsInAnyPolys(plb.GetCenter(), laneBoxes))
                         && (!IsInAnyPolys(plb.GetCenter(), cplys)))
                    {
                        CarSpots.Add(plb);
                        AddToSpatialIndex(plb, ref ObstaclesSpatialIndex);
                    }
                }
            }
            cplys.Clear();
            laneBoxes.Clear();
            pcars.Clear();
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
                linestmp.Clear();
            }

            for (int i = 0; i < lines.Count; i++)
            {
                var li = lines[i];
                bool hasNeighLane = false;
                foreach (var l in IniLanes)
                {
                    if (IsParallelLine(li, l) && DisBetweenTwoParallelLines(li, l) < DisModulus + DisCarAndHalfLane)
                    {
                        hasNeighLane = true;
                        break;
                    }
                }
                if (hasNeighLane)
                {
                    lines.RemoveAt(i);
                    i--;
                }
            }

            box.Dispose();
            objs.Dispose();
            edges.Clear();
            pts.Clear();


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
                    edgeobjs.Dispose();
                    if (quit) break;
                }
            }
            
            splited.Clear();
            

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
                splited.Clear();
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
                splited.Clear();
            }

            offsetedtest.Clear();
            offseted.Clear();

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
                if (ClosestPointInCurves(l.StartPoint, pls) > 10)
                {
                    Line ls = LineSDL(l.StartPoint, Vector(l.EndPoint, l.StartPoint), 100000);
                    var rs = new Line();
                    foreach (var k in SplitCurve(ls, Cutters))
                    {
                        if (k.GetClosestPointTo(l.StartPoint, false).DistanceTo(l.StartPoint) < 1) rs = (Line)k;
                    }
                    var ply = JoinCurves(new List<Polyline>(), new List<Line>() { rs, l })[0];
                    var nl = new Line(ply.StartPoint, ply.EndPoint);
                    var res = RemoveCollisionalLanesForObstacles(nl);
                    if (res.Count > 0)
                    {
                        foreach (var pm in Modules)
                        {
                            if (pm.Lanes.First().StartPoint.DistanceTo(IniLanes[i].StartPoint) < 1
                                && pm.Lanes.First().EndPoint.DistanceTo(IniLanes[i].EndPoint) < 1)
                            {
                                pm.Lanes[0] = res[0];
                                break;
                            }
                        }

                        IniLanes[i] = res[0];
                        SequenceLanes[i] = res[0];
                        Cutters.Remove(l);
                        Cutters.Add(IniLanes[i]);
                    }


                }
                if (ClosestPointInCurves(l.EndPoint, pls) > 10)
                {

                    Line le = LineSDL(l.EndPoint, Vector(l.StartPoint, l.EndPoint), 100000);
                    var re = new Line();
                    foreach (var k in SplitCurve(le, Cutters))
                    {
                        if (k.GetClosestPointTo(l.EndPoint, false).DistanceTo(l.EndPoint) < 1) re = (Line)k;
                    }
                    var ply = JoinCurves(new List<Polyline>(), new List<Line>() { l, re })[0];

                    var nl = new Line(ply.StartPoint, ply.EndPoint);
                    var res = RemoveCollisionalLanesForObstacles(nl);
                    if (res.Count > 0)
                    {

                        foreach (var pm in Modules)
                        {
                            if (pm.Lanes.First().StartPoint.DistanceTo(IniLanes[i].StartPoint) < 1
                                && pm.Lanes.First().EndPoint.DistanceTo(IniLanes[i].EndPoint) < 1)
                            {
                                pm.Lanes[0] = res[0];
                                break;
                            }
                        }

                        IniLanes[i] = res[0];
                        SequenceLanes[i] = res[0];
                        Cutters.Remove(l);
                        Cutters.Add(IniLanes[i]);
                    }
                }
                pls.Clear();
            }
        }

        /// <summary>
        /// 连接孤立的车道线
        /// </summary>
        private void ConnectIsolatedLanes()
        {
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var l = IniLanes[i];
                var ls = new List<Curve>(IniLanes);
                ls.RemoveAt(i);
                var intersected = ClosestPointInCurves(l.StartPoint, ls) < 10 || ClosestPointInCurves(l.EndPoint, ls) < 10
                    || IsIntersect(l, ls);
                if (intersected) continue;
                if (!intersected)
                {
                    var ps = new Point3d();
                    var pe = new Point3d();
                    double ds = -1;
                    double de = -1;
                    int ist = -1;
                    int ie = -1;
                    var lps = ls.Select(e => new Line(e.StartPoint, e.EndPoint)).ToList();
                    ls = ls.Where(e => IsParallelLine(l, (Line)e)).Cast<Curve>().ToList();
                    var cs = lps.Where(e => IsParallelLine(l, (Line)e)).Cast<Curve>().ToList();
                    ClosestPointInCurves(l.StartPoint, cs, ref ps, ref ds, ref ist);
                    Line lx = new Line(l.StartPoint, ps);
                    lx.TransformBy(Matrix3d.Displacement(Vector(l.StartPoint, l.EndPoint).GetNormal() * DisLaneWidth / 2));
                    IniLanes.Add(lx);
                    SequenceLanes.Add(lx);
                }
                ls.Clear();
            }
        }

        /// <summary>
        /// 将车道线在邻近障碍物处切分
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="lengthfilter"></param>
        /// <param name="dis"></param>
        /// <returns></returns>
        private List<Line> SplitLaneWithNearObstacles(Line lane,
             double dis = DisLaneWidth / 2)
        {
            List<Line> results = new List<Line>();
            var ls = OffsetLine(lane, dis);
            var lanebox = PolyFromPoints(new List<Point3d>() {  ls[0].StartPoint, ls[0].EndPoint,
                 ls[1].EndPoint,ls[1].StartPoint,ls[0].StartPoint});
            var crossed = ObstaclesSpatialIndex.SelectCrossingPolygon(lanebox);
            if (crossed.Count == 0)
            {
                results.Add(lane);
                return results;
            }
            else
            {
                //lane.AddToCurrentSpace();
                List<Point3d> pts = new List<Point3d>();
                //var pls = crossed.Cast<Polyline>().ToArray();
                foreach (var pl in crossed.Cast<Entity>())
                {
                    pts.AddRange(lanebox.Intersect(pl, Intersect.OnBothOperands));
                    if (pl is Polyline)
                    {
                        pts.AddRange(((Polyline)pl).Vertices().Cast<Point3d>()
                            .Where(p => lanebox.IsPointIn(p)));
                    }
                }


                pts = pts.Select(e => lane.GetClosestPointTo(e, false)).Distinct().ToList();

                //pts.ForEach(p => p.CreateSquare(500).AddToCurrentSpace());

                SortAlongCurve(pts, lane);
                var res = lane.GetSplitCurves(pts).Cast<Line>().ToList();

                //res.AddToCurrentSpace();

                pts.Clear();
                return res;
            }
        }

        private void SplitAndRemoveLanesNearObstacles(double lengthfilter = DisModulus)
        {

            var mtwo = Modules.Where(e => e.LayoutMode == ((int)LayoutMode.DoubleVert)).ToList();
            var mone = Modules.Where(e => e.LayoutMode == ((int)LayoutMode.SingleVert)).ToList();
            List<ParkModule> pmdts = new List<ParkModule>();
            for (int i = 0; i < mtwo.Count; i++)
            {
                var la = mtwo[i].Lanes[0];
                var lb = mtwo[i].Lanes[1];
                var lal = new Line(lb.GetClosestPointTo(la.StartPoint, true),
                    lb.GetClosestPointTo(la.EndPoint, true));
                var lbl = new Line(la.GetClosestPointTo(lb.StartPoint, true),
                    la.GetClosestPointTo(lb.EndPoint, true));

                var resa = SplitLaneWithNearObstacles(la);
                resa = resa.Where(e => e.Length >= 10).ToList();
                if (resa.Count == 1)
                {
                    ParkModule module= new ParkModule();
                    module.Lanes = new Line[] { la, lal };
                    module.LayoutMode = ((int)LayoutMode.SingleVert);
                    pmdts.Add(module);
                }
                else
                {
                    foreach (var r in resa)
                    {
                        var ls = new List<Curve>(IniLanes);
                        ls.Remove(la);
                        var intersected = ClosestPointInCurves(r.StartPoint, ls) < 10 || ClosestPointInCurves(r.EndPoint, ls) < 10
        || IsIntersect(r, ls);
                        if (intersected && r.Length >= lengthfilter)
                        {
                            ParkModule pm = new ParkModule();
                            pm.Lanes = new Line[] { r, lal };
                            pm.LayoutMode = ((int)LayoutMode.SingleVert);
                            mone.Add(pm);
                        }
                    }
                }

                var resb = SplitLaneWithNearObstacles(lb);
                resb = resb.Where(e => e.Length >= 10).ToList();
                ;
                if (resb.Count == 1)
                {
                    
                    ParkModule module = new ParkModule();
                    module.Lanes = new Line[] { lb, lbl };
                    module.LayoutMode = ((int)LayoutMode.SingleVert);
                    pmdts.Add(module);
                }
                else
                {
                    foreach (var r in resb)
                    {
                        var ls = new List<Curve>(IniLanes);
                        ls.Remove(lb);
                        var intersected = ClosestPointInCurves(r.StartPoint, ls) < 10 || ClosestPointInCurves(r.EndPoint, ls) < 10
        || IsIntersect(r, ls);
                        if (intersected && r.Length >= lengthfilter)
                        {
                            ParkModule pm = new ParkModule();
                            pm.Lanes = new Line[] { r, lbl };
                            pm.LayoutMode = ((int)LayoutMode.SingleVert);
                            pmdts.Add(pm);
                        }
                    }
                }
            }

            for (int i = 0; i < mone.Count; i++)
            {
                var la = mone[i].Lanes[0];
                var lb = mone[i].Lanes[1];
                var res = SplitLaneWithNearObstacles(la);
                res = res.Where(e => e.Length >= 10).ToList();
                if (res.Count <= 1) continue;
                mone.RemoveAt(i);
                i--;
                foreach (var r in res)
                {
                    var ls = new List<Curve>(IniLanes);
                    ls.Remove(la);
                    var intersected = ClosestPointInCurves(r.StartPoint, ls) < 10 || ClosestPointInCurves(r.EndPoint, ls) < 10
    || IsIntersect(r, ls);
                    if (intersected && r.Length >= lengthfilter)
                    {
                        ParkModule pm = new ParkModule();
                        pm.Lanes = new Line[] { r, lb };
                        pm.LayoutMode = ((int)LayoutMode.SingleVert);
                        pmdts.Add(pm);
                    }
                }
            }

            Modules.Clear();
            Modules.AddRange(pmdts);


            IniLanes = IniLanes.Distinct().ToList();

            for (int i = 0; i < IniLanes.Count; i++)
            {
                var l = IniLanes[i];
                var res = SplitLaneWithNearObstacles(l);
                res = res.Where(e => e.Length >= 10).ToList();
                if (res.Count <= 1) continue;
                IniLanes.RemoveAt(i);
                i--;
                foreach (var r in res)
                {
                    var ls = new List<Curve>(IniLanes);
                    ls.Remove(l);
                    var intersected = ClosestPointInCurves(r.StartPoint, ls) < 10 || ClosestPointInCurves(r.EndPoint, ls) < 10
    || IsIntersect(r, ls);
                    if (intersected && r.Length >= lengthfilter)
                    {
                        IniLanes.Add(r);
                    }
                }
            }
            //IniLanes.AddToCurrentSpace();

        }


        /// <summary>
        /// 车位组合单元
        /// </summary>
        private class ParkModule
        {
            public Line[] Lanes;
            public int LayoutMode;
        }

        /// <summary>
        /// 车位排布模式
        /// </summary>
        enum LayoutMode : int
        {
            SingleVert = 0,//单排垂直式
            DoubleVert = 1,//双排垂直式
            SingleLying = 2//单排平行式
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

        public static void SortAlongCurve(List<Point3d> points, Curve curve)
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
            pls.Clear();
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
            //objs.Cast<Entity>().ToList().ForEach(e => pts.AddRange(curve.Intersect(e, Intersect.OnBothOperands)));
            foreach (var e in objs.Cast<Entity>().ToList())
            {
                if (e != null)
                    pts.AddRange(curve.Intersect(e, Intersect.OnBothOperands));
            }
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

        public static void ClosestPointInCurves(Point3d pt, List<Curve> crvs,
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

        public static double ClosestPointInCurves(Point3d pt, List<Curve> crvs)
        {
            if (crvs.Count == 0) return 0;
            var p = crvs[0].GetClosestPointTo(pt, false);
            var res = p.DistanceTo(pt);
            if (crvs.Count == 1) return res;
            for (int i = 1; i < crvs.Count; i++)
            {
                var pc = crvs[i].GetClosestPointTo(pt, false);
                var d = pc.DistanceTo(pt);
                if (d < res)
                {
                    res = d;
                }
            }
            return res;
        }

        public static bool IsIntersect(Curve c, List<Curve> crvs)
        {
            foreach (var crv in crvs)
            {
                if (c.Intersect(crv, Intersect.OnBothOperands).Count > 0)
                    return true;
            }
            return false;
        }

        public static Point3dCollection DivideCurveByLength(Curve crv, double length, ref DBObjectCollection segs)
        {
            Point3dCollection pts = new Point3dCollection();

            pts = new Point3dCollection(crv.GetPointsByDist(length).ToArray());

            segs = crv.GetSplitCurves(pts);
            if (segs.Count == 0) segs.Add(crv);

            return pts;
        }

        public static void AddToSpatialIndex(Entity e, ref ThCADCoreNTSSpatialIndex spatialIndex)
        {
            DBObjectCollection add = new DBObjectCollection();
            add.Add(e);
            spatialIndex.Update(add, new DBObjectCollection());
            
            return;
        }

        public static string AnalysisLine(Line a)
        {
            string s = a.StartPoint.X.ToString() + "," + a.StartPoint.Y.ToString() + "," +
                a.EndPoint.X.ToString() + "," + a.EndPoint.Y.ToString() + ",";
            return s;
        }
        public static string AnalysisLineList(List<Line> a)
        {
            string s = "";
            foreach (var e in a)
            {
                s += AnalysisLine(e);
            }
            return s;
        }
        public static string AnalysisPoly(Polyline a)
        {
            string s = "";
            var e = a.Vertices().Cast<Point3d>().ToList();
            for (int i = 0; i < e.Count; i++)
            {
                s += e[i].X.ToString() + e[i].Y.ToString() + ",";
            }
            return s;
        }
        public static string AnalysisPolyList(List<Polyline> pls)
        {
            string s = "";
            foreach (var e in pls)
            {
                s += AnalysisPoly(e) + ";";
            }
            return s;
        }

        public static double GetLengthDifferentFromParallelBofA(Line a, Line b, double buffer = 15700)
        {
            double length = 0;
            var pl = b.Buffer(buffer);
            var splited = SplitCurve(a, new DBObjectCollection() { pl });
            foreach (Line s in splited)
            {
                if (!pl.IsPointIn(s.GetCenter())) length += s.Length;
            }
            return length;
        }


    }
}
