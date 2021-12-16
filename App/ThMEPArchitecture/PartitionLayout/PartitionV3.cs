using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using static ThMEPArchitecture.PartitionLayout.GeoUtilities;
using static ThMEPArchitecture.PartitionLayout.Utitlties;

namespace ThMEPArchitecture.PartitionLayout
{
    public class PartitionV3
    {
        public PartitionV3(List<Polyline> walls, List<Line> iniLanes,
        List<Polyline> obstacles, Polyline boundary, List<Polyline> buildingBox)
        {
            Walls = walls;
            IniLaneLines = iniLanes;
            Obstacles = obstacles;
            Boundary = boundary;
            BuildingBoxes = buildingBox;
            BoundingBox = Boundary.GeometricExtents.ToRectangle();
            MaxDistance = BoundingBox.Length / 2;
            IniBoundary = boundary;
            InitialzeLanes(iniLanes, boundary);
        }
        public List<Polyline> Walls;
        public List<Lane> IniLanes = new List<Lane>();
        public List<Polyline> Obstacles;
        public Polyline Boundary;
        public ThCADCoreNTSSpatialIndex ObstaclesSpatialIndex;
        private List<Polyline> CarSpots = new List<Polyline>();
        private List<Polyline> CarModuleBox = new List<Polyline>();
        public ThCADCoreNTSSpatialIndex MoudleSpatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
        private List<IntegralModule> IntegralModules = new List<IntegralModule>();
        private List<Line> IniLaneLines;
        private List<Polyline> BuildingBoxes;
        private Polyline BoundingBox;
        private double MaxDistance;
        private ThCADCoreNTSSpatialIndex CarSpatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
        private List<Polyline> ModuleBox = new List<Polyline>();
        private List<Polyline> Laneboxes = new List<Polyline>();
        private Polyline IniBoundary = new Polyline();

        const double DisLaneWidth = 5500;
        const double DisCarLength = 5100;
        const double DisCarWidth = 2400;
        const double DisCarAndHalfLane = DisLaneWidth / 2 + DisCarLength;
        const double DisModulus = DisCarAndHalfLane * 2;

        const double LengthCanGIntegralModules = 3 * DisCarWidth + DisLaneWidth / 2;
        const double ScareFactorForCollisionCheck = 0.99;

        /// <summary>
        /// Main
        /// </summary>
        public void GenerateParkingSpaces()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                GenerateLanes();
                GenerateCarSpots();
                UndateDataToGenerateCarSpotsInMinPartitionUnits();
                GenerateCarSpotsInMinPartitionUnits();
                PostProcessCarSpots();
            }
        }

        /// <summary>
        /// Calaulate the total count of car spots in the specific partition.
        /// </summary>
        /// <returns></returns>
        public int CalNumOfParkingSpaces()
        {
            int count = 0;
            GenerateParkingSpaces();
            count = CarSpots.Count;
            CarSpots.ForEach(e => e.Dispose());
            return count;
        }

        /// <summary>
        /// Process and display the best solutions calculated by genetic algorithm.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="colorIndex"></param>
        public void ProcessAndDisplay(string layer = "0", int colorIndex = 0)
        {
            GenerateParkingSpaces();
            Display(layer, colorIndex);
        }

        /// <summary>
        /// Display the car spots finally. 
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="colorindex"></param>
        public void Display(string layer = "0", int colorindex = 0)
        {
            CarSpots.Select(e =>
            {
                e.Layer = layer;
                e.ColorIndex = colorindex;
                return e;
            }).AddToCurrentSpace();

        }

        /// <summary>
        /// Construct lane class from lines infos input.
        /// </summary>
        /// <param name="iniLanes"></param>
        /// <param name="boundary"></param>
        private void InitialzeLanes(List<Line> iniLanes, Polyline boundary)
        {
            foreach (var e in iniLanes)
            {
                var vec = Vector(e).GetPerpendicularVector().GetNormal();
                var pt = e.GetCenter().TransformBy(Matrix3d.Displacement(vec));
                if (!boundary.IsPointIn(pt))
                {
                    vec = -vec;
                }
                IniLanes.Add(new Lane(e, vec));
            }
        }

        /// <summary>
        /// Judge if the car is legal(collision).
        /// </summary>
        /// <param name="car"></param>
        /// <returns></returns>
        private bool CheckCarLegal(Polyline car)
        {
            var pl = car.Clone() as Polyline;
            pl.Scale(pl.Centroid(), ScareFactorForCollisionCheck);
            if (pl.Intersect(Boundary, Intersect.OnBothOperands).Count > 0)
            {
                pl.Dispose();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Undate datas to generate car spots in the smallest partition unit.
        /// </summary>
        private void UndateDataToGenerateCarSpotsInMinPartitionUnits()
        {
            Extents3d bigext = new Extents3d();
            BuildingBoxes.ForEach(e => bigext.AddExtents(e.GeometricExtents));
            var bigbox = bigext.ToRectangle();
            DBObjectCollection objs = new DBObjectCollection();        
            bigbox.Explode(objs);
            List<Line> edges = objs.Cast<Line>().ToList();
            var disy = edges.OrderBy(e => e.Length).First().Length / 2;
            Matrix3d mat = Matrix3d.Displacement(Vector3d.YAxis * (disy - 10));
            var pto = bigbox.Centroid().TransformBy(mat);
            List<Vector3d> vecs = edges.Select(e => Vector(pto, Vector(e).X == 0 ? e.GetCenter().TransformBy(mat) : e.GetCenter()).GetNormal()).ToList();
            List<Curve> crvs = new List<Curve>();
            List<Curve> inis = new List<Curve>();
            IniLanes.ForEach(e => inis.Add(e.Line));
            inis.AddRange(Walls);
            for (int i = 0; i < edges.Count; i++)
            {
                Line l = LineSDL(pto, vecs[i], 500000);
                double mindis = 9999999;
                Curve crv = new Line();
                foreach (var c in inis)
                {
                    var pss = l.Intersect(c, Intersect.OnBothOperands);
                    if (pss.Count > 0 && pss[0].DistanceTo(l.StartPoint) < mindis)
                    {
                        mindis = pss[0].DistanceTo(l.StartPoint);
                        crv = c;
                    }
                }
                crvs.Add(crv);
            }
            crvs= crvs.Distinct().ToList();
            List<Curve> res = new List<Curve>();
            for (int i = 0; i < crvs.Count; i++)
            {
                var ccs = new List<Curve>(crvs);
                ccs.RemoveAt(i);
                List<Point3d> points = new List<Point3d>();
                foreach (var cs in ccs)
                {
                    points.AddRange(crvs[i].Intersect(cs, Intersect.OnBothOperands));
                }
                res.AddRange(SplitCurve(crvs[i], points));
            }
            List<Curve> rs = new List<Curve>();
            for (int i = 0; i < edges.Count; i++)
            {
                Line l = LineSDL(pto, vecs[i], 500000);
                double mindis = 9999999;
                Curve crv = new Line();
                foreach (var c in res)
                {
                    var pss = l.Intersect(c, Intersect.OnBothOperands);
                    if (pss.Count > 0 && pss[0].DistanceTo(l.StartPoint) < mindis)
                    {
                        mindis = pss[0].DistanceTo(l.StartPoint);
                        crv = c;
                    }
                }
                rs.Add(crv);
            }

            rs = rs.Clone().Cast<Curve>().ToList();
            List<Polyline> pls = new List<Polyline>();
            List<Line> ls = new List<Line>();
            foreach (var r in rs)
            {
                if (r is Polyline) pls.Add((Polyline)r);
                else ls.Add((Line)r);
            }
            Boundary = JoinCurves(pls, ls)[0];
            var tmplanes = new List<Line>();
            for (int i = 0; i < rs.Count; i++)
            {
                foreach (var j in IniLanes)
                {
                    if (j.Line.GetClosestPointTo(rs[i].GetCenter(), false).DistanceTo(rs[i].GetCenter()) < 1)
                    {

                        tmplanes.Add((Line)rs[i]);
                        rs.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
            IniLanes = new List<Lane>();
            foreach (var e in tmplanes)
            {
                var vec = Vector(e).GetPerpendicularVector().GetNormal();
                var pt = e.GetCenter().TransformBy(Matrix3d.Displacement(vec));
                if (!Boundary.IsPointIn(pt))
                {
                    vec = -vec;
                }
                IniLanes.Add(new Lane(e, vec));
            }
            Walls = new List<Polyline>();
            foreach (var k in rs)
            {
                if (k is Polyline) Walls.Add((Polyline)k);
                else
                {
                    Walls.Add(PolyFromLine((Line)k));
                }
            }
            BoundingBox = Boundary.GeometricExtents.ToRectangle();
            MaxDistance = BoundingBox.Length / 2;
            IniLaneLines = IniLanes.Select(e => e.Line).ToList();
        }

        /// <summary>
        /// Generate car spots in the smallest partition unit.
        /// </summary>
        private void GenerateCarSpotsInMinPartitionUnits()
        {
            IniLanes = IniLanes.OrderByDescending(e => e.Line.Length).ToList();          
            for (int i=0;i<IniLanes.Count;i++)
            {
                var inilanelines = new List<Line>(IniLanes.Select(e => e.Line).Cast<Line>().ToList());
                inilanelines.RemoveAt(i);
                var inilane = IniLanes[i];
                var l = Line(inilane.Line);
                l.TransformBy(Matrix3d.Displacement(inilane.Vec.GetNormal() * (DisLaneWidth / 2 + DisCarWidth * 3)));
                var crossedmodules = SplitLine(l, ModuleBox).Cast<Line>().Where(e => !IsInAnyPolys(e.GetCenter(), ModuleBox)).ToList();
                var boundobstacles = new List<Polyline>();
                boundobstacles.AddRange(IniLanes.Select(e => PolyFromLine(e.Line)));
                boundobstacles.RemoveAt(i);
                boundobstacles.AddRange(Walls);
                double length_module_used = 0;
                foreach (var cslane in crossedmodules)
                {
                    var lane = Line(cslane);
                    lane.TransformBy(Matrix3d.Displacement(-inilane.Vec.GetNormal() * (DisLaneWidth / 2 + DisCarWidth * 3)));
                    if (ClosestPointInCurves(lane.StartPoint, inilanelines) < 1)
                    {
                        if (lane.Length < DisCarAndHalfLane) continue;
                        else
                            lane = new Line(lane.StartPoint.TransformBy(Matrix3d.Displacement(Vector(lane.StartPoint, lane.EndPoint).GetNormal() * DisCarAndHalfLane)), lane.EndPoint);
                    }
                    if (ClosestPointInCurves(lane.EndPoint, inilanelines) < 1)
                    {
                        if (lane.Length < DisCarAndHalfLane) continue;
                        else
                            lane = new Line(lane.StartPoint, lane.EndPoint.TransformBy(Matrix3d.Displacement(Vector(lane.EndPoint, lane.StartPoint).GetNormal() * DisCarAndHalfLane)));
                    }
                    LayoutOneDirection(lane, inilane.Vec, Obstacles, boundobstacles, ref length_module_used);
                }
                IniLanes[i].RestLength = IniLanes[i].Line.Length - length_module_used;
            }

            IniLanes = IniLanes.OrderByDescending(e => e.RestLength).ToList();
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var inilanelines = new List<Line>(IniLanes.Select(e => e.Line).Cast<Line>().ToList());
                inilanelines.RemoveAt(i);
                var lane = Line(IniLanes[i].Line);
                if (ClosestPointInCurves(lane.StartPoint, inilanelines) < 1)
                {
                    if (lane.Length < DisCarAndHalfLane) continue;
                    else
                        lane = new Line(lane.StartPoint.TransformBy(Matrix3d.Displacement(Vector(lane.StartPoint, lane.EndPoint).GetNormal() * DisLaneWidth / 2)), lane.EndPoint);
                }
                if (ClosestPointInCurves(lane.EndPoint, inilanelines) < 1)
                {
                    if (lane.Length < DisCarAndHalfLane) continue;
                    else
                        lane = new Line(lane.StartPoint, lane.EndPoint.TransformBy(Matrix3d.Displacement(Vector(lane.EndPoint, lane.StartPoint).GetNormal() * DisLaneWidth / 2)));
                }

                var boundobstacles = new List<Polyline>();
                boundobstacles.AddRange(IniLanes.Select(e => PolyFromLine(e.Line)));
                boundobstacles.RemoveAt(i);
                boundobstacles.AddRange(Walls);
                List<Polyline> respots = GenerateRestVertAndParallelSpots(new List<Line>() { lane }, IniLanes[i].Vec, boundobstacles);
                CarSpots.AddRange(respots);
            }
        }

        /// <summary>
        /// Generate car spots unidirectionally in the smallest partition unit.
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="vec"></param>
        /// <param name="obstacles"></param>
        /// <param name="boundobstacles"></param>
        /// <param name="length_module_used"></param>
        private void LayoutOneDirection(Line lane, Vector3d vec, List<Polyline> obstacles, List<Polyline> boundobstacles,ref double length_module_used)
        {
            int modulecount = ((int)Math.Floor(lane.Length / DisModulus));
            int vertcount = ((int)Math.Floor((lane.Length - modulecount * DisModulus) / DisCarWidth));
            List<Line> ilanes = new List<Line>();
            for (int i = 0; i < modulecount; i++)
            {
                var ptbasestart = lane.StartPoint.TransformBy(Matrix3d.Displacement(Vector(lane).GetNormal() * DisModulus * i));
                var ptbaseend = lane.StartPoint.TransformBy(Matrix3d.Displacement(Vector(lane).GetNormal() * DisModulus * (i + 1)));
                Line unitbase = new Line(ptbasestart, ptbaseend);
                ilanes.Add(unitbase);
            }
            PMModlues pMModlues = ConstructPMModules(vec, ilanes, boundobstacles);
            for (int i = 0; i < vertcount; i++)
            {
                var test = UpdataPMModlues(pMModlues, boundobstacles);
                if (test.Count >= pMModlues.Count) pMModlues = test;
                else break;
            }
            List<Line> restsegs = new List<Line>();
            if (pMModlues.Bounds.Count > 0)
                restsegs = SplitLine(lane, pMModlues.Bounds).Cast<Line>().Where(e => ClosestPointInCurves(e.GetCenter(), pMModlues.Bounds) > 1).ToList();
            else restsegs.Add(lane);
            restsegs = restsegs.Where(e => e.Length >= DisCarWidth).ToList();
            List<Polyline> respots = GenerateRestVertAndParallelSpots(restsegs, vec, boundobstacles);
            CarSpots.AddRange(respots);
            CarSpots.AddRange(pMModlues.GetCarSpots(ref CarSpatialIndex,ObstaclesSpatialIndex, ModuleBox,Boundary));
            length_module_used = pMModlues.Bounds.Count * DisModulus;
        }

        /// <summary>
        /// Generate the rest car spots of intergral modules in the smallest partition unit. 
        /// </summary>
        /// <param name="restsegs"></param>
        /// <param name="vec"></param>
        /// <param name="boundobstacles"></param>
        /// <returns></returns>
        private List<Polyline> GenerateRestVertAndParallelSpots(List<Line> restsegs, Vector3d vec, List<Polyline> boundobstacles)
        {
            List<Polyline> respots = new List<Polyline>();
            foreach (var e in restsegs)
            {
                e.TransformBy(Matrix3d.Displacement(vec.GetNormal() * DisLaneWidth / 2));
                var l = Line(e);
                l.TransformBy(Matrix3d.Displacement(vec.GetNormal() * DisCarLength));
                var validvertlines = SplitLineByObstacles(l, e);
                foreach (var vl in validvertlines)
                {
                    var es = DivideLineByLength(vl, DisCarWidth).Where(o => Math.Abs(o.Length - DisCarWidth) < 1).ToList();
                    foreach (var s in es)
                    {
                        var ps = e.GetClosestPointTo(s.StartPoint, true);
                        var pe = e.GetClosestPointTo(s.EndPoint, true);
                        var pl = PolyFromPoints(new Point3d[] { ps, s.StartPoint, s.EndPoint, pe });
                        var plsc = pl.Clone() as Polyline;
                        plsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, pl.Centroid()));
                        var conda =CarSpatialIndex.SelectCrossingPolygon(plsc).Count == 0;
                        var condb = !ObstaclesSpatialIndex.Intersects(plsc, true);
                        var condc = !IsInAnyPolys(plsc.Centroid(), ModuleBox);
                        if (conda && condb && condc && CheckCarLegal(pl))
                        {
                            AddToSpatialIndex(pl, ref CarSpatialIndex);
                            respots.Add(pl);
                        }
                    }
                }
            }
            return respots;
        }

        /// <summary>
        /// Undate the class pmmodudle.
        /// </summary>
        /// <param name="pMModlues"></param>
        /// <param name="boundobstacles"></param>
        /// <returns></returns>
        private PMModlues UpdataPMModlues(PMModlues pMModlues, List<Polyline> boundobstacles)
        {
            PMModlues result;
            int minindex = pMModlues.Mminindex;
            List<Line> ilanes = new List<Line>(pMModlues.Lanes);
            if (pMModlues.Lanes.Count == 0) return pMModlues;
            var vec_move = Vector(pMModlues.Lanes[0]).GetNormal() * DisCarWidth;
            for (int i = 0; i < ilanes.Count; i++)
            {
                if (i >= minindex)
                {
                    ilanes[i].TransformBy(Matrix3d.Displacement(vec_move));
                }
            }
            result= ConstructPMModules(pMModlues.Vec, ilanes, boundobstacles);
            return result;
        }

        /// <summary>
        /// Construct the class pmmodule.
        /// </summary>
        /// <param name="vec"></param>
        /// <param name="ilanes"></param>
        /// <param name="boundobstacles"></param>
        /// <returns></returns>
        private PMModlues ConstructPMModules(Vector3d vec,List<Line>ilanes, List<Polyline> boundobstacles)
        {
            PMModlues result = new PMModlues();
            int count = 0;
            int minindex = 0;
            int mincount = 9999;
            List<Polyline> plys = new List<Polyline>();
            vec = vec.GetNormal() * MaxDistance;
            for (int i = 0; i < ilanes.Count; i++)
            {
                var unitbase = ilanes[i];
                var curcount = GenerateUsefulModules(unitbase, vec, boundobstacles, plys);
                if (curcount < 7)
                {
                    ilanes.RemoveAt(i);
                    i--;
                    plys.RemoveAt(plys.Count - 1);
                    plys.RemoveAt(plys.Count - 1);
                    continue;
                }
                if (curcount < mincount)
                {
                    mincount = curcount;
                    minindex = i;
                }
                count += curcount;
            }      
            result.Bounds = plys;
            result.Count = count;
            result.Lanes = ilanes;
            result.Mminindex = minindex;
            result.Vec = vec;
            return result;
        }

        /// <summary>
        /// Split line by obstacles in the smallest partition unit.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="oriline"></param>
        /// <returns></returns>
        private List<Line> SplitLineByObstacles(Line line, Line oriline)
        {
            List<Line> results = new List<Line>();
            var pl = PolyFromPoints(new Point3d[] { line.StartPoint, line.EndPoint, oriline.EndPoint, oriline.StartPoint });
            pl.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, pl.Centroid()));
            List<Point3d> points = new List<Point3d>();
            var crossedobjs = ObstaclesSpatialIndex.SelectCrossingPolygon(pl).Cast<Polyline>().ToList();
            crossedobjs.AddRange(Obstacles.Where(e => pl.GeometricExtents.IsPointIn(e.Centroid())));
            foreach (var obj in crossedobjs)
            {
                points.AddRange(obj.Vertices().Cast<Point3d>().Where(e => pl.IsPointIn(e)));
                points.AddRange(obj.Intersect(pl, Intersect.OnBothOperands));
            }
            points = points.Select(e => line.GetClosestPointTo(e, false)).ToList();
            RemoveDuplicatePts(points);
            return GetSplitLine(line, points).Where(e => e.Length >= DisCarWidth)
                .Where(e => !IsInAnyPolys(e.GetCenter(), Obstacles)).ToList();
        }

        /// <summary>
        /// Generate pmmodules in the smallest partition unit and return the total count of car spots in the pmmodules.
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="vec"></param>
        /// <param name="boundobstacles"></param>
        /// <param name="plys"></param>
        /// <returns></returns>
        private int GenerateUsefulModules(Line lane, Vector3d vec, List<Polyline> boundobstacles,List<Polyline> plys)
        {
            int count = 0;
            Line unittest = Line(lane);
            unittest.TransformBy(Matrix3d.Displacement(vec.GetNormal() * MaxDistance));
            var pltest = PolyFromPoints(new Point3d[] { lane.StartPoint, lane.EndPoint, unittest.EndPoint, unittest.StartPoint });
            ThCADCoreNTSSpatialIndex sindexwalls = new ThCADCoreNTSSpatialIndex(boundobstacles.ToCollection());
            var pltestsc = pltest.Clone() as Polyline;         
            try
            {
                pltestsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, pltestsc.Centroid()));
            }
            catch
            {
            }
            var crossed = sindexwalls.SelectCrossingPolygon(pltestsc).Cast<Polyline>().ToList();
            crossed.AddRange(ObstaclesSpatialIndex.SelectCrossingPolygon(pltestsc).Cast<Polyline>());
            crossed.AddRange(CarSpatialIndex.SelectCrossingPolygon(pltestsc).Cast<Polyline>());
            List<Point3d> points = new List<Point3d>();
            foreach (var o in crossed)
            {
                points.AddRange(o.Vertices().Cast<Point3d>().ToArray());
                points.AddRange(o.Intersect(pltest, Intersect.OnBothOperands));
            }
            points = RemoveDuplicatePts(points);
            points = points.Where(e => pltest.IsPointIn(e) || pltest.GetClosePoint(e).DistanceTo(e) < 1).ToList();
            Line edgea = new Line(lane.StartPoint, unittest.StartPoint);
            Line edgeb = new Line(lane.EndPoint, unittest.EndPoint);
            var pointsa = points.Where(e => edgea.GetClosestPointTo(e, false).DistanceTo(e) <=
              edgeb.GetClosestPointTo(e, false).DistanceTo(e))
                .Select(e => edgea.GetClosestPointTo(e, false));
            var pointsb = points.Where(e => edgea.GetClosestPointTo(e, false).DistanceTo(e) >
              edgeb.GetClosestPointTo(e, false).DistanceTo(e))
                .Select(e => edgeb.GetClosestPointTo(e, false));
            Point3d pta;
            Point3d ptb;
            if (pointsa.ToArray().Length == 0) pta = lane.StartPoint;
            else pta = pointsa.OrderBy(e => e.DistanceTo(lane.StartPoint)).ToArray().First();
            if (pointsb.ToArray().Length == 0) ptb = lane.StartPoint;
            else ptb = pointsb.OrderBy(e => e.DistanceTo(lane.EndPoint)).ToArray().First();
            foreach (var la in IniLanes)
            {
                var disa = la.Line.GetClosestPointTo(pta, false).DistanceTo(pta);
                if (disa < DisLaneWidth / 2)
                {
                    pta = pta.TransformBy(Matrix3d.Displacement(Vector(pta, lane.StartPoint).GetNormal() * (DisLaneWidth / 2 - disa)));
                }
                var disb = la.Line.GetClosestPointTo(ptb, false).DistanceTo(ptb);
                if (disb < DisLaneWidth / 2)
                {
                    ptb = ptb.TransformBy(Matrix3d.Displacement(Vector(ptb, lane.EndPoint).GetNormal() * (DisLaneWidth / 2 - disb)));
                }
            }
            var pl = PolyFromPoints(new Point3d[] { lane.StartPoint, lane.EndPoint, ptb, pta });
            Line eb = new Line(lane.EndPoint, ptb);
            Line ea = new Line(lane.StartPoint, pta);
            count += ((int)Math.Floor((ea.Length - DisLaneWidth / 2) / DisCarWidth));
            count += ((int)Math.Floor((eb.Length - DisLaneWidth / 2) / DisCarWidth));
            plys.Add(PolyFromPoints(new Point3d[] { lane.StartPoint, lane.StartPoint.TransformBy(Matrix3d.Displacement(Vector(lane.StartPoint,lane.EndPoint).GetNormal()*DisCarAndHalfLane)),
                pta.TransformBy(Matrix3d.Displacement(Vector(lane.StartPoint,lane.EndPoint).GetNormal()*DisCarAndHalfLane)), pta }));
            plys.Add(PolyFromPoints(new Point3d[] { lane.EndPoint, lane.EndPoint.TransformBy(Matrix3d.Displacement(-Vector(lane.StartPoint,lane.EndPoint).GetNormal()*DisCarAndHalfLane)),
                 ptb.TransformBy(Matrix3d.Displacement(-Vector(lane.StartPoint,lane.EndPoint).GetNormal()*DisCarAndHalfLane)),ptb}));
            return count;
        }

        /// <summary>
        /// Generate lanes which could split the boundary to generate integral modules.
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

                    var generate_integral_modules = GenerateIntegralModuleLanes();
                    if (generate_integral_modules) continue;

                    var generate_adj_lanes = GenerateAdjLanes();
                    if (generate_adj_lanes) continue;

                    if (lanecount == IniLanes.Count) break;
                }
            }
        }

        /// <summary>
        /// Generate lane which is grown from the endpoint of the exist lane. 
        /// </summary>
        /// <returns></returns>
        private bool GenerateAdjLanes()
        {
            var generate_adj_lanes = false;
            IniLanes = IniLanes.OrderByDescending(e => e.Line.Length).ToList();
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var lane = IniLanes[i];
                if (lane.Line.Length <= DisCarAndHalfLane) continue;
                var lanes = IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(lane.Line, e)).ToList();
                var distart = ClosestPointInCurves(lane.Line.StartPoint, lanes);
                var disend = ClosestPointInCurves(lane.Line.EndPoint, lanes);
                if (distart > 1)
                {
                    var generated = GenerateAdjLanesFunc(lane,true);
                    if (generated)
                    {
                        generate_adj_lanes = true;
                        break;
                    }
                }
                if (disend > 1)
                {
                    var generated = GenerateAdjLanesFunc(lane, false);
                    if (generated)
                    {
                        generate_adj_lanes = true;
                        break;
                    }
                }
            }
            return generate_adj_lanes;
        }

        /// <summary>
        /// Function to implement the method GenerateAdjLanes.
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="isStartpt"></param>
        /// <returns></returns>
        private bool GenerateAdjLanesFunc(Lane lane,bool isStartpt)
        {
            var generate_adj_lanes = false;
            Point3d pt;
            Point3d ps;
            if (isStartpt)
            {
                pt = lane.Line.StartPoint;
                ps = pt.TransformBy(Matrix3d.Displacement(Vector(lane.Line).GetNormal() * DisCarAndHalfLane));
            }
            else
            {
                pt = lane.Line.EndPoint;
                ps = pt.TransformBy(Matrix3d.Displacement(-Vector(lane.Line).GetNormal() * DisCarAndHalfLane));
            }
            
            var lt = LineSDL(ps, lane.Vec, MaxDistance);
            var cutters = new List<Polyline>() { Boundary};
            IniLanes.ForEach(e => cutters.Add(PolyFromLine(e.Line)));

            List<Line> segs = new List<Line>();
            Polyline pl = new Polyline();
            Line r = new Line();
            var disu = IsTheAdjLaneUnderAndNearObstacles(BuildingBoxes, lt);
            bool under = false;
            if (disu != -1 && IsHorizantal(lt))
            {
                under = true;
                var underl = Line(lt);
                underl.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * (disu - DisLaneWidth / 2)));
                segs = SplitLine(underl, cutters).Cast<Line>().Where(e => e.Length > 1).ToList();
                segs.OrderBy(e => e.GetCenter().DistanceTo(pt));
                r = segs[0];
                pl = r.Buffer(DisLaneWidth / 2);
                pl.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, pl.Centroid()));
                var crossedunder = ObstaclesSpatialIndex.SelectCrossingPolygon(pl);
                if (crossedunder.Count >0) return false;
            }
            else
            {
                segs = SplitLine(lt, cutters).Cast<Line>().Where(e => e.Length > 1).ToList();
                segs.OrderBy(e => e.GetCenter().DistanceTo(pt));
                r = segs[0];
                pl = r.Buffer(DisLaneWidth / 2);
                var crossed = ObstaclesSpatialIndex.SelectCrossingPolygon(pl);
                if (crossed.Count > 0) return false;
            }

            var v = Vector(r).GetPerpendicularVector().GetNormal();
            var p = r.StartPoint.TransformBy(Matrix3d.Displacement(v));
            if (lane.Line.GetClosestPointTo(p, true).DistanceTo(pt) < DisCarAndHalfLane) v = -v;
            var planes = IniLanes.Select(e => e.Line).Where(e => IsParallelLine(lane.Line, e)).ToList();
            var dis = ClosestPointInCurves(r.GetCenter(), planes);
            var distmp = ClosestPointInCurves(r.GetCenter(), IniLanes.Select(e => e.Line).ToList());
            if (dis >= DisModulus / 2 && distmp > 1 && !IsInAnyPolys(r.GetCenter(), IntegralModules.Select(e => e.Box).ToList()))
            {
                Vector3d vec = v;
                if (under) vec = -Vector3d.YAxis;
                Lane lanec = new Lane(r, vec);
                IniLanes.Add(lanec);
                generate_adj_lanes = true;
                IniLaneLines.Add(r);
                return generate_adj_lanes;
            }
            return generate_adj_lanes;
        }

        /// <summary>
        /// Generate car spots which is calaulted from intergral modules. 
        /// </summary>
        private void GenerateCarSpots()
        {
            GenerateIntegralModuleCars();
            Laneboxes.AddRange(IniLanes.Select(e => e.Line.Buffer(DisLaneWidth / 2-10)).Distinct());
            if (CarSpots.Count == 0) return;
            GenerateCarsInSingleVerticalDirection();
            GenerateCarsInSingleParallelDirection();
        }

        /// <summary>
        /// Generate cars in single vertical direction.
        /// </summary>
        private void GenerateCarsInSingleVerticalDirection()
        {
            var lanes = IniLanes.Distinct().Where(e => Boundary.GetClosestPointTo(e.Line.GetCenter(), true).DistanceTo(e.Line.GetCenter()) <= DisModulus / 2+1000).ToList();
            var lines = lanes.Select(e => e.Line).ToList();
            var vecs = lanes.Select(e => Vector(e.Line).GetPerpendicularVector()).ToList();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var offset = Line(line);
                offset.TransformBy(Matrix3d.Displacement(vecs[i].GetNormal() * DisCarAndHalfLane));
                var pl = PolyFromPoints(new Point3d[] { line.StartPoint, line.EndPoint, offset.EndPoint, offset.StartPoint });
                var plsc = pl.Clone() as Polyline;
                plsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, plsc.Centroid()));
                if (Boundary.IsPointIn(pl.Centroid()) && pl.Area > DisCarWidth * DisCarAndHalfLane * 2
                    && plsc.Intersect(Boundary, Intersect.OnBothOperands).Count == 0)
                {
                    var crossed = CarSpatialIndex.SelectCrossingPolygon(plsc).Cast<Polyline>().ToList();
                    crossed.AddRange(ObstaclesSpatialIndex.SelectCrossingPolygon(plsc).Cast<Polyline>().ToList());
                    if (true)
                    {
                        var lt = Line(line);
                        lt.TransformBy(Matrix3d.Displacement(vecs[i].GetNormal() * DisLaneWidth / 2));
                        var es = DivideLineByLength(lt, DisCarWidth).Where(o => Math.Abs(o.Length - DisCarWidth) < 1).ToList();
                        foreach (var s in es)
                        {
                            var ps = offset.GetClosestPointTo(s.StartPoint, true);
                            var pe = offset.GetClosestPointTo(s.EndPoint, true);
                            var plcar = PolyFromPoints(new Point3d[] { ps, s.StartPoint, s.EndPoint, pe });
                            var plcarsc = plcar.Clone() as Polyline;
                            plcarsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, plcarsc.Centroid()));
                            bool condbox = false;
                            foreach (var box in Laneboxes)
                            {
                                if (plcarsc.Intersect(box, Intersect.OnBothOperands).Count > 0)
                                {
                                    condbox = true;
                                    break;
                                }
                            }
                            if (condbox) continue;
                            if (CarSpatialIndex.SelectCrossingPolygon(plcarsc).Count == 0
                                && ObstaclesSpatialIndex.SelectCrossingPolygon(plcarsc).Count == 0
                                && (!IsInAnyPolys(plcarsc.Centroid(), Laneboxes))
                                && CheckCarLegal(plcar))
                            {
                                AddToSpatialIndex(plcar, ref CarSpatialIndex);
                                CarSpots.Add(plcar);
                            }
                        }
                    }
                }

                offset = Line(line);
                offset.TransformBy(Matrix3d.Displacement(-vecs[i].GetNormal() * DisCarAndHalfLane));
                pl = PolyFromPoints(new Point3d[] { line.StartPoint, line.EndPoint, offset.EndPoint, offset.StartPoint });
                plsc = pl.Clone() as Polyline;
                plsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, plsc.Centroid()));
                if (Boundary.IsPointIn(pl.Centroid()) && pl.Area > DisCarWidth * DisCarAndHalfLane * 2
                    && plsc.Intersect(Boundary, Intersect.OnBothOperands).Count == 0)
                {
                    var crossed = CarSpatialIndex.SelectCrossingPolygon(plsc).Cast<Polyline>().ToList();
                    crossed.AddRange(ObstaclesSpatialIndex.SelectCrossingPolygon(plsc).Cast<Polyline>().ToList());
                    if (true)
                    {
                        var lt = Line(line);
                        lt.TransformBy(Matrix3d.Displacement(-vecs[i].GetNormal() * DisLaneWidth / 2));
                        var es = DivideLineByLength(lt, DisCarWidth).Where(o => Math.Abs(o.Length - DisCarWidth) < 1).ToList();
                        foreach (var s in es)
                        {
                            var ps = offset.GetClosestPointTo(s.StartPoint, true);
                            var pe = offset.GetClosestPointTo(s.EndPoint, true);
                            var plcar = PolyFromPoints(new Point3d[] { ps, s.StartPoint, s.EndPoint, pe });
                            var plcarsc = plcar.Clone() as Polyline;
                            plcarsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, plcarsc.Centroid()));
                            bool condbox = false;
                            foreach (var box in Laneboxes)
                            {
                                if (plcarsc.Intersect(box, Intersect.OnBothOperands).Count > 0)
                                {
                                    condbox = true;
                                    break;
                                }
                            }
                            if (condbox) continue;
                            if (CarSpatialIndex.SelectCrossingPolygon(plcarsc).Count == 0
                                && ObstaclesSpatialIndex.SelectCrossingPolygon(plcarsc).Count == 0
                                && (!IsInAnyPolys(plcarsc.Centroid(), Laneboxes))
                                && CheckCarLegal(plcar))
                            {
                                AddToSpatialIndex(plcar, ref CarSpatialIndex);
                                CarSpots.Add(plcar);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generate cars in single parallel direction.
        /// </summary>
        private void GenerateCarsInSingleParallelDirection()
        {
            var lanes = IniLanes.Distinct().Where(e => Boundary.GetClosestPointTo(e.Line.GetCenter(), true).DistanceTo(e.Line.GetCenter()) <= DisModulus / 2 + 1000).ToList();
            var lines = lanes.Select(e => e.Line).ToList();         
            var vecs = lanes.Select(e => Vector(e.Line).GetPerpendicularVector()).ToList();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var offset = Line(line);
                offset.TransformBy(Matrix3d.Displacement(vecs[i].GetNormal() * (DisCarWidth+DisLaneWidth/2)));
                var pl = PolyFromPoints(new Point3d[] { line.StartPoint, line.EndPoint, offset.EndPoint, offset.StartPoint });
                var plsc = pl.Clone() as Polyline;
                plsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, plsc.Centroid()));
                if (Boundary.IsPointIn(pl.Centroid()) && pl.Area > DisCarWidth * DisCarAndHalfLane * 2)
                {
                    var crossed = CarSpatialIndex.SelectCrossingPolygon(plsc).Cast<Polyline>().ToList();
                    crossed.AddRange(ObstaclesSpatialIndex.SelectCrossingPolygon(plsc).Cast<Polyline>().ToList());
                    if (true)
                    {
                        var lt = Line(line);
                        lt.TransformBy(Matrix3d.Displacement(vecs[i].GetNormal() * DisLaneWidth / 2));
                        var es = DivideLineByLength(lt, DisCarLength).Where(o => Math.Abs(o.Length - DisCarLength) < 1).ToList();
                        foreach (var s in es)
                        {
                            var ps = offset.GetClosestPointTo(s.StartPoint, true);
                            var pe = offset.GetClosestPointTo(s.EndPoint, true);
                            var plcar = PolyFromPoints(new Point3d[] { ps, s.StartPoint, s.EndPoint, pe });
                            var plcarsc = plcar.Clone() as Polyline;
                            plcarsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, plcarsc.Centroid()));
                            bool condbox = false;
                            foreach (var box in Laneboxes)
                            {
                                if (plcarsc.Intersect(box, Intersect.OnBothOperands).Count > 0)
                                {
                                    condbox = true;
                                    break;
                                }
                            }
                            if (condbox) continue;
                            if (CarSpatialIndex.SelectCrossingPolygon(plcarsc).Count == 0
                                && ObstaclesSpatialIndex.SelectCrossingPolygon(plcarsc).Count == 0
                                && (!IsInAnyPolys(plcarsc.Centroid(), Laneboxes))
                                && CheckCarLegal(plcar))
                            {
                                AddToSpatialIndex(plcar, ref CarSpatialIndex);
                                CarSpots.Add(plcar);
                            }
                        }
                    }
                }

                offset = Line(line);
                offset.TransformBy(Matrix3d.Displacement(-vecs[i].GetNormal() * (DisCarWidth + DisLaneWidth / 2)));
                pl = PolyFromPoints(new Point3d[] { line.StartPoint, line.EndPoint, offset.EndPoint, offset.StartPoint });
                plsc = pl.Clone() as Polyline;
                plsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, plsc.Centroid()));
                if (Boundary.IsPointIn(pl.Centroid()) && pl.Area > DisCarWidth * DisCarAndHalfLane * 2)
                {
                    var crossed = CarSpatialIndex.SelectCrossingPolygon(plsc).Cast<Polyline>().ToList();
                    crossed.AddRange(ObstaclesSpatialIndex.SelectCrossingPolygon(plsc).Cast<Polyline>().ToList());
                    if (true)
                    {
                        var lt = Line(line);
                        lt.TransformBy(Matrix3d.Displacement(-vecs[i].GetNormal() * DisLaneWidth / 2));
                        var es = DivideLineByLength(lt, DisCarLength).Where(o => Math.Abs(o.Length - DisCarLength) < 1).ToList();
                        foreach (var s in es)
                        {
                            var ps = offset.GetClosestPointTo(s.StartPoint, true);
                            var pe = offset.GetClosestPointTo(s.EndPoint, true);
                            var plcar = PolyFromPoints(new Point3d[] { ps, s.StartPoint, s.EndPoint, pe });
                            var plcarsc = plcar.Clone() as Polyline;
                            plcarsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, plcarsc.Centroid()));
                            bool condbox = false;
                            foreach (var box in Laneboxes)
                            {
                                if (plcarsc.Intersect(box, Intersect.OnBothOperands).Count > 0)
                                {
                                    condbox = true;
                                    break;
                                }
                            }
                            if (condbox) continue;
                            if (CarSpatialIndex.SelectCrossingPolygon(plcarsc).Count == 0
                                && ObstaclesSpatialIndex.SelectCrossingPolygon(plcarsc).Count == 0
                                && (!IsInAnyPolys(plcarsc.Centroid(), Laneboxes))
                                && CheckCarLegal(plcar))
                            {
                                AddToSpatialIndex(plcar, ref CarSpatialIndex);
                                CarSpots.Add(plcar);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generate integral modules.
        /// </summary>
        private void GenerateIntegralModuleCars()
        {
            foreach (var module in IntegralModules)
            {
                var points = IniLanes.Select(e => e.Line.StartPoint).Where(e => module.Box.IsPointIn(e)).ToList();
                if (points.Count == 0)
                {
                    var lanea = Line(module.Lanes[0]);
                    var laneb = Line(module.Lanes[1]);
                    Vector3d veca = Vector(lanea.GetCenter(), laneb.GetClosestPointTo(lanea.GetCenter(), true)).GetNormal() * DisLaneWidth / 2;
                    lanea.TransformBy(Matrix3d.Displacement(veca));
                    laneb.TransformBy(Matrix3d.Displacement(-veca));
                    UnifyLaneDirection(ref lanea);
                    UnifyLaneDirection(ref laneb);
                    veca = veca.GetNormal() * DisCarLength;
                    GenerateVertCars(lanea, veca);
                    GenerateVertCars(laneb, -veca);
                    lanea.Dispose();
                    laneb.Dispose();
                }
                else //Case for concave polygon
                {
                }
            }
        }

        /// <summary>
        /// Generate car spots on the single vertical direction on the first stage.
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="vec"></param>
        private void GenerateVertCars(Line lane, Vector3d vec)
        {
            var disstart = ClosestPointInCurves(lane.StartPoint, IniLaneLines);
            var disend = ClosestPointInCurves(lane.EndPoint, IniLaneLines);
            if (disstart < DisLaneWidth / 2)
                lane = new Line(lane.StartPoint.TransformBy(Matrix3d.Displacement(Vector(lane.StartPoint, lane.EndPoint).GetNormal() * (DisLaneWidth / 2 - disstart))), lane.EndPoint);
            if (disend < DisLaneWidth / 2)
                lane = new Line(lane.StartPoint, lane.EndPoint.TransformBy(Matrix3d.Displacement(Vector(lane.EndPoint, lane.StartPoint).GetNormal() * (DisLaneWidth / 2 - disend))));
            var segobjs = new DBObjectCollection();
            DivideCurveByLength(lane, DisCarWidth, ref segobjs);
            var segs = segobjs.Cast<Line>().Where(e => Math.Abs(e.Length - DisCarWidth) < 1).ToArray();
            foreach (var seg in segs)
            {
                var s = Line(seg);
                s.TransformBy(Matrix3d.Displacement(vec));
                var car = PolyFromPoints(new Point3d[] { seg.StartPoint, seg.EndPoint, s.EndPoint, s.StartPoint });
                var carsc = car.Clone() as Polyline;
                carsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, carsc.Centroid()));
                if (CarSpatialIndex.SelectCrossingPolygon(carsc).Count == 0
                              && ObstaclesSpatialIndex.SelectCrossingPolygon(carsc).Count == 0
                              && (!IsInAnyPolys(carsc.Centroid(), Laneboxes))
                              && CheckCarLegal(car))
                {
                    AddToSpatialIndex(car, ref CarSpatialIndex);
                    CarSpots.Add(car);
                }
                carsc.Dispose();
                seg.Dispose();
                s.Dispose();
            }
            segobjs.Dispose();
        }

        /// <summary>
        /// Generate integral module lanes.
        /// </summary>
        /// <returns></returns>
        private bool GenerateIntegralModuleLanes()
        {        
            var generate_integral_modules = false;
            IniLanes = IniLanes.OrderByDescending(e => e.Line.Length).ToList();
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var lane = IniLanes[i].Line;
                bool isNotInModules = true;
                if (CarModuleBox.Count > 0)
                    isNotInModules = ClosestPointInCurves(lane.GetCenter(), CarModuleBox) > 1;
                bool skip = (!IniLanes[i].CanBeMoved) || lane.Length < LengthCanGIntegralModules
                    || (IsInAnyPolys(lane.GetCenter(), CarModuleBox));
                if (skip) continue;
                var offsetlane = Line(lane);
                offsetlane.TransformBy(Matrix3d.Displacement(IniLanes[i].Vec * DisModulus));
                offsetlane.TransformBy(Matrix3d.Scaling(10, offsetlane.GetCenter()));
                var splited = SplitLine(offsetlane, Boundary).Where(e => Boundary.IsPointIn(e.GetCenter())).Where(e=>e.GetLength()>0).ToArray();
                if (splited.Length > 0) offsetlane = (Line)splited.First();
                else continue;
                splited = SplitLine(offsetlane, CarModuleBox).ToArray();
                if (splited.Length > 1) offsetlane = (Line)splited.OrderBy(e => e.GetCenter().DistanceTo(lane.GetClosestPointTo(e.GetCenter(), false))).ToArray()[0];
                if (IsInAnyPolys(offsetlane.GetCenter(), CarModuleBox)) continue;
                var ply = PolyFromPoints(new Point3d[] { lane.StartPoint, lane.EndPoint, offsetlane.EndPoint, offsetlane.StartPoint });
                bool isConnected = false;
                var distance = Math.Min(ClosestPointInCurves(offsetlane.StartPoint, IniLaneLines),
                    ClosestPointInCurves(offsetlane.EndPoint, IniLaneLines));
                if (distance < 10) isConnected = true;
                var plrsc = ply.Clone() as Polyline;
                plrsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, plrsc.Centroid()));
                var hascollision = ObstaclesSpatialIndex.Intersects(plrsc, true);
                if (isConnected && (!hascollision))
                {
                    var test_l = Line(offsetlane);
                    test_l.TransformBy(Matrix3d.Displacement(IniLanes[i].Vec * DisLaneWidth / 2));
                    var test_pl = PolyFromPoints(new Point3d[] { offsetlane.StartPoint, offsetlane.EndPoint, test_l.EndPoint, test_l.StartPoint });
                    try
                    {
                        test_pl.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, test_pl.Centroid()));
                    }
                    catch { }
                    var crossed = ObstaclesSpatialIndex.Intersects(test_pl, true);
                    test_l.Dispose();
                    test_pl.Dispose();
                    var closest_diatance_on_offset_direction = GetClosestDistanceOnOffsetDirection(offsetlane, IniLanes[i].Vec, IniLaneLines);
                    var cond_allow_offset_bydistance = closest_diatance_on_offset_direction >= DisModulus
                        || closest_diatance_on_offset_direction <= DisLaneWidth / 2;

                    if (!crossed)
                    {
                        var dis = IsUnderAndNearObstacles(BuildingBoxes, offsetlane);
                        if (dis != -1 && IsHorizantal(offsetlane))
                        {
                            generate_integral_modules = true;
                            IniLanes[i].CanBeMoved = false;
                            var underl = Line(offsetlane);
                            underl.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * (dis - DisLaneWidth / 2)));
                            Lane r = new Lane(underl, -Vector3d.YAxis);
                            IniLanes.Add(r);
                            IniLaneLines.Add(underl);
                            IntegralModule undermodule = new IntegralModule();
                            var ml = Line(underl);
                            ml.TransformBy(Matrix3d.Displacement(-Vector3d.YAxis * DisModulus));
                            Lane dl = new Lane(ml, -Vector3d.YAxis);
                            IniLanes.Add(dl);
                            IniLaneLines.Add(ml);
                            var py = PolyFromPoints(new Point3d[] { underl.StartPoint, underl.EndPoint, ml.EndPoint, ml.StartPoint });
                            CarModuleBox.Add(py);
                            var plsc = py.Clone() as Polyline;
                            plsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, plsc.Centroid()));
                            undermodule.Box = plsc;
                            undermodule.Lanes = new Line[] { underl, ml };
                            IntegralModules.Add(undermodule);
                            for (int j = 0; j < IniLanes.Count; j++)
                            {
                                var splitedlanes = SplitCurve(IniLanes[j].Line, py);
                                if (splitedlanes.Length > 1 && IniLanes[j].CanBeMoved)
                                {
                                    var vecpre = IniLanes[j].Vec;
                                    IniLanes.RemoveAt(j);
                                    foreach (var l in splitedlanes)
                                    {
                                        IniLanes.Add(new Lane((Line)l, vecpre));
                                    }
                                    j--;
                                }
                            }
                            break;
                        }
                        else
                        {
                            generate_integral_modules = true;
                            IniLanes[i].CanBeMoved = false;
                            Lane res = new Lane(offsetlane, IniLanes[i].Vec);
                            IniLanes.Add(res);
                            CarModuleBox.Add(ply);
                            IniLaneLines.Add(offsetlane);
                            IntegralModule module = new IntegralModule();
                            module.Box = plrsc;
                            module.Lanes = new Line[] { lane, offsetlane };
                            IntegralModules.Add(module);
                            for (int j = 0; j < IniLanes.Count; j++)
                            {
                                var splitedlanes = SplitCurve(IniLanes[j].Line, ply);
                                if (splitedlanes.Length > 1 && IniLanes[j].CanBeMoved)
                                {
                                    var vecpre = IniLanes[j].Vec;
                                    IniLanes.RemoveAt(j);
                                    foreach (var l in splitedlanes)
                                    {
                                        IniLanes.Add(new Lane((Line)l, vecpre));
                                    }
                                    j--;
                                }
                            }
                            break;
                        }                
                    }
                }
            }
            return generate_integral_modules;
        }

        /// <summary>
        /// Precess generated car spots finally.
        /// </summary>
        private void PostProcessCarSpots()
        {
            RemoveDuplicateCars();
            RemoveCarsIntersectedWithBoundary();
        }

        /// <summary>
        /// Remove repeated car spots.
        /// </summary>
        private void RemoveDuplicateCars()
        {
            for (int i = 1; i < CarSpots.Count; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (CarSpots[i].Centroid().DistanceTo(CarSpots[j].Centroid()) < 1)
                    {
                        CarSpots.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Remove cars intersected with boundary.
        /// </summary>
        private void RemoveCarsIntersectedWithBoundary()
        {
            for (int i = 1; i < CarSpots.Count; i++)
            {
                var pl = CarSpots[i].Clone() as Polyline;
                pl.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, pl.Centroid()));
                for (int j = 0; j < i; j++)
                {
                    if (pl.Intersect(IniBoundary, Intersect.OnBothOperands).Count > 0)
                    {
                        pl.Dispose();
                        CarSpots.RemoveAt(i);
                        i--;
                        break;
                    }
                }
                pl.Dispose();
            }
        }

        /// <summary>
        /// Judge if the lane to be generated is nececcary to move to the specific place near the building and return the distance.
        /// </summary>
        /// <param name="boxes"></param>
        /// <param name="lane"></param>
        /// <returns></returns>
        private double IsUnderAndNearObstacles(List<Polyline> boxes, Line lane)
        {
            double distance = 9999999;
            foreach (var box in boxes)
            {
                if (box.Centroid().Y > lane.GetCenter().Y)
                {
                    var p_on_lane = lane.GetClosestPointTo(box.Centroid(), true);
                    var p_on_box = box.GetClosePoint(p_on_lane);
                    double d = p_on_lane.DistanceTo(p_on_box);
                    if (d < DisModulus + DisLaneWidth / 2 - 10 && d < distance) distance = d;
                }
            }
            if (distance == 9999999) return -1;
            return distance;
        }

        /// <summary>
        /// Judge if the adjacent lane to be generated is nececcary to move to the specific place near the building and return the distance.
        /// </summary>
        /// <param name="boxes"></param>
        /// <param name="lane"></param>
        /// <returns></returns>
        private double IsTheAdjLaneUnderAndNearObstacles(List<Polyline> boxes, Line lane)
        {
            double distance = 9999999;
            foreach (var box in boxes)
            {
                if (box.Centroid().Y > lane.GetCenter().Y)
                {
                    var p_on_lane = lane.GetClosestPointTo(box.Centroid(), true);
                    var p_on_box = box.GetClosePoint(p_on_lane);
                    double d = p_on_lane.DistanceTo(p_on_box);
                    if (d < distance) distance = d;
                }
            }
            if (distance == 9999999) return -1;
            return distance;
        }

        /// <summary>
        /// Judge if the line is horizontal(not precisely).
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool IsHorizantal(Line line)
        {
            Vector3d vec = Vector(line);
            double x = Math.Abs(vec.X);
            double y =Math.Abs( vec.Y);
            if (x < y) x = 0;
            else y = 0;
            if (y == 0) return true;
            return false;

        }

        /// <summary>
        /// Class lane.
        /// </summary>
        public class Lane
        {
            public Lane(Line line, Vector3d vec, bool canBeMoved = true)
            {
                Line = line;
                Vec = vec;
                CanBeMoved = canBeMoved;
            }
            public Line Line;
            public bool CanBeMoved;
            public Vector3d Vec;
            public double RestLength;
        }

        /// <summary>
        /// Class integral module.
        /// </summary>
        public class IntegralModule
        {
            public Polyline Box;
            public Line[] Lanes;
        }

        /// <summary>
        /// Class pmmodule.
        /// </summary>
        private class PMModlues
        {
            public List<Line> Lanes;
            public int Mminindex;
            public int Count;
            public Vector3d Vec;
            public List<Polyline> Bounds;

            /// <summary>
            /// Generate car spots in the pmmodule.
            /// </summary>
            /// <param name="carSpacilaIndex"></param>
            /// <param name="obsSpacilaIndex"></param>
            /// <param name="ModuleBox"></param>
            /// <param name="boundary"></param>
            /// <returns></returns>
            public List<Polyline> GetCarSpots(ref ThCADCoreNTSSpatialIndex carSpacilaIndex , ThCADCoreNTSSpatialIndex obsSpacilaIndex, List<Polyline> ModuleBox,Polyline boundary)
            {
                List<Polyline> spots = new List<Polyline>();
                foreach (var pl in Bounds)
                {
                    var objs = new DBObjectCollection();
                    pl.Explode(objs);
                    var segs = objs.Cast<Line>().ToArray();
                    var a = segs[1];
                    var b = segs[3];
                    b.ReverseCurve();
                    List<Line> lines = new List<Line>() {  b };
                    var vec = Vector(a.GetClosestPointTo(b.GetCenter(), true), b.GetCenter()).GetNormal() * DisCarLength;
                    List<Vector3d> vecs = new List<Vector3d>() { -vec };
                    for (int i = 0; i < lines.Count; i++)
                    {
                        int count = ((int)Math.Floor((lines[i].Length - DisLaneWidth / 2) / DisCarWidth));
                        Line ea = LineSDL(lines[i].StartPoint.TransformBy(Matrix3d.Displacement(Vector(lines[i]).GetNormal() * DisLaneWidth / 2)), Vector(lines[i]), DisCarWidth);
                        for (int j = 0; j < count; j++)
                        {
                            Line tmp = Line(ea);
                            tmp.TransformBy(Matrix3d.Displacement(Vector(ea).GetNormal() * DisCarWidth * j));
                            var offset = Line(tmp);
                            offset.TransformBy(Matrix3d.Displacement(vecs[i]));
                            var plcar = PolyFromPoints(new Point3d[] { tmp.StartPoint, tmp.EndPoint, offset.EndPoint, offset.StartPoint });
                            var plcarsc = plcar.Clone() as Polyline;
                            plcarsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, plcarsc.Centroid()));
                            if (carSpacilaIndex.SelectCrossingPolygon(plcarsc).Count == 0
                             && obsSpacilaIndex.SelectCrossingPolygon(plcarsc).Count == 0
                             && plcarsc.Intersect(boundary, Intersect.OnBothOperands).Count == 0)
                            {
                                spots.Add(plcar);
                            }
                            plcarsc.Dispose();
                        }
                    }
                    var plb = Line(b);
                    plb.TransformBy(Matrix3d.Displacement(-vec.GetNormal() * DisCarAndHalfLane));
                    var plbp = PolyFromPoints(new Point3d[] { b.StartPoint, b.EndPoint, plb.EndPoint, plb.StartPoint });
                    AddToSpatialIndex(plbp, ref carSpacilaIndex);
                    ModuleBox.Add(plbp);
                }
                return spots;
            }
        }
    }

    /// <summary>
    /// Class business utilities.
    /// </summary>
    public static class Utitlties
    {
        /// <summary>
        /// Unify lane direction.
        /// </summary>
        /// <param name="lane"></param>
        public static void UnifyLaneDirection(ref Line lane)
        {
            if (lane.StartPoint.X > lane.EndPoint.X) lane.ReverseCurve();
            else if (lane.StartPoint.Y > lane.EndPoint.Y) lane.ReverseCurve();
        }

        /// <summary>
        /// Get closest distance on the offset direction.
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="vec"></param>
        /// <param name="lanes"></param>
        /// <returns></returns>
        public static double GetClosestDistanceOnOffsetDirection(Line lane, Vector3d vec, List<Line> lanes)
        {
            lanes = lanes.Where(e => IsParallelLine(lane, e)).ToList();
            var pt = lane.GetCenter();
            Line sdl = LineSDL(pt, vec, 100000);
            var points = new List<Point3d>();
            lanes.Select(e => sdl.Intersect(e, Intersect.OnBothOperands)).ForEach(f => points.AddRange(f));
            points = points.OrderBy(e => e.DistanceTo(pt)).ToList();
            return points.Count > 0 ? pt.DistanceTo(points.First()) : 0;
        }
    }
}