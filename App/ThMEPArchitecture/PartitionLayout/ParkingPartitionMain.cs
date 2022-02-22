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
using ThMEPArchitecture.ViewModel;
using ThMEPEngineCore.CAD;
using static ThMEPArchitecture.PartitionLayout.GeoUtilities;

namespace ThMEPArchitecture.PartitionLayout
{
    public class PartitionBoundary : IEquatable<PartitionBoundary>
    {
        public List<Point3d> BoundaryVertices = new List<Point3d>();
        public PartitionBoundary(Point3dCollection pts)
        {
            BoundaryVertices = pts.Cast<Point3d>().ToList();
        }

        public bool Equals(PartitionBoundary other)
        {
            if (this.BoundaryVertices.Count != other.BoundaryVertices.Count) return false;
            var thisVertices = this.BoundaryVertices;
            var otherVertices = other.BoundaryVertices;
            for (int i = 0; i < this.BoundaryVertices.Count; i++)
            {
                if (!thisVertices[i].IsEqualTo(otherVertices[i])) return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            var hashcode = BoundaryVertices.Count;
            var thisVertices = this.BoundaryVertices;
            foreach (var vertex in thisVertices)
            {
                hashcode ^= vertex.GetHashCode();
            }
            return hashcode;
        }
    }
    public enum LayoutDirection : int
    {
        LENGTH = 0,
        HORIZONTAL = 1,
        VERTICAL = 2
    }

    public partial class ParkingPartition : IEquatable<ParkingPartition>
    {
        public ParkingPartition()
        {

        }

        public ParkingPartition(List<Polyline> walls, List<Line> iniLanes,
        List<Polyline> obstacles, Polyline boundary, List<Polyline> buildingBox, ParkingStallArrangementViewModel vm = null)
        {
            if (vm != null)
            {
                DisParallelCarLength = vm.ParallelSpotLength > vm.ParallelSpotWidth ? vm.ParallelSpotLength : vm.ParallelSpotWidth;
                DisParallelCarWidth = vm.ParallelSpotLength > vm.ParallelSpotWidth ? vm.ParallelSpotWidth : vm.ParallelSpotLength;
                DisVertCarLength = vm.VerticalSpotLength > vm.VerticalSpotWidth ? vm.VerticalSpotLength : vm.VerticalSpotWidth;
                DisVertCarWidth = vm.VerticalSpotLength > vm.VerticalSpotWidth ? vm.VerticalSpotWidth : vm.VerticalSpotLength;
                DisLaneWidth = vm.RoadWidth;
                MaxPillarSpacing = vm.ColumnWidth;
                PillarNetLength = vm.ColumnSizeOfParalleToRoad;
                PillarNetDepth = vm.ColumnSizeOfPerpendicularToRoad;
                ThicknessOfPillarConstruct = vm.ColumnAdditionalSize;
                LayoutMode = ((int)vm.RunMode);
            }
            GeneratePillars = MaxPillarSpacing < DisVertCarWidth ? false : GeneratePillars;
            DisPillarLength = PillarNetLength + ThicknessOfPillarConstruct * 2;
            DisPillarDepth = PillarNetDepth + ThicknessOfPillarConstruct * 2;
            CountPillarDist = ((int)(Math.Floor(MaxPillarSpacing / DisVertCarWidth)));
            DisCarAndHalfLane = DisLaneWidth / 2 + DisVertCarLength;
            DisModulus = DisCarAndHalfLane * 2;
            MinCountAllowGVertCarOnLine = 4;
            LengthCanGIntegralModules = 3 * DisVertCarWidth + DisLaneWidth / 2;
            LengthCanGAdjLaneConnectedDouble = DisLaneWidth + DisVertCarWidth * 6;

            Walls = walls;
            IniLaneLines = iniLanes;
            Boundary = boundary;
            BuildingBoxes = buildingBox;
            BoundingBox = Boundary.GeometricExtents.ToRectangle();
            MaxDistance = BoundingBox.Length / 2;
            IniBoundary = boundary;
            Countinilanes = iniLanes.Count;
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
        private int Countinilanes = 0;
        private List<Polyline> FirstStageCarBoxes = new List<Polyline>();
        private List<Lane> GSingleLanes = new List<Lane>();
        private Polyline NewBound = new Polyline();
        private List<Curve> NewBoundEdges = new List<Curve>();
        private List<Polyline> PModuleBox = new List<Polyline>();
        private List<Polyline> Pillars = new List<Polyline>();
        public ThCADCoreNTSSpatialIndex ObstaclesMPolygonSpatialIndex;

        public static bool GeneratePillars = true;
        public static double PillarNetLength = 500;
        public static double PillarNetDepth = 500;
        public static double ThicknessOfPillarConstruct = 50;
        public static double MaxPillarSpacing = 8000;
        public static double DisVertCarLength = 5100;
        public static double DisVertCarWidth = 2400;
        public static double DisParallelCarLength = 6000;
        public static double DisParallelCarWidth = 2400;
        public static double DisLaneWidth = 5500;
        public static double DisPillarLength = PillarNetLength + ThicknessOfPillarConstruct * 2;
        public static double DisPillarDepth = PillarNetDepth + ThicknessOfPillarConstruct * 2;
        public static int CountPillarDist = ((int)(Math.Floor(MaxPillarSpacing / DisVertCarWidth)));
        public static double DisCarAndHalfLane = DisLaneWidth / 2 + DisVertCarLength;
        public static double DisModulus = DisCarAndHalfLane * 2;
        private static int MinCountAllowGVertCarOnLine = 4;
        public static double LengthCanGIntegralModules = 3 * DisVertCarWidth + DisLaneWidth / 2;
        public static double LengthCanGAdjLaneConnectedDouble = DisLaneWidth + DisVertCarWidth * 6;
        public static int LayoutMode = ((int)LayoutDirection.HORIZONTAL);

        const double ScareFactorForCollisionCheck = 0.99;

        public void Dispose()
        {
            Walls?.ForEach(e => e.Dispose());
            //Walls?.Clear
            IniLanes?.ForEach(e => e.Line.Dispose());
            //Obstacles?.ForEach(e => e.Dispose());
            Boundary?.Dispose();
            CarModuleBox?.ForEach(e => e.Dispose());
            MoudleSpatialIndex?.Dispose();
            IntegralModules?.ForEach(e => e.Box.Dispose());
            IntegralModules?.ForEach(e => e.Lanes.ForEach(o => o.Dispose()));
            IniLaneLines?.ForEach(e => e.Dispose());
            BuildingBoxes?.ForEach(e => e.Dispose());
            BoundingBox?.Dispose();
            CarSpatialIndex?.Dispose();
            ModuleBox?.ForEach(e => e.Dispose());
            Laneboxes?.ForEach(e => e.Dispose());
            IniBoundary?.Dispose();
            FirstStageCarBoxes?.ForEach(e => e.Dispose());
            NewBound?.Dispose();
            NewBoundEdges?.ForEach(e => e.Dispose());
            PModuleBox?.ForEach(e => e.Dispose());
            GSingleLanes?.ForEach(e => e.Line?.Dispose());
        }

        public bool Validate()
        {
            double totallength = 0;
            Walls.ForEach(e => totallength += e.Length);
            IniLanes.ForEach(e => totallength += e.Line.Length);
            if (Math.Abs(Boundary.Length - totallength) > 1) return false;
            foreach (var l in IniLaneLines)
                if (Boundary.GetClosestPointTo(l.GetCenter(), false).DistanceTo(l.GetCenter()) > 1) return false;
            foreach (var l in Walls)
                if (Boundary.GetClosestPointTo(l.GetPointAtDist(l.Length / 2), false).DistanceTo(l.GetPointAtDist(l.Length / 2)) > 1) return false;
            return true;
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
                var vec = CreateVector(e).GetPerpendicularVector().GetNormal();
                var pt = e.GetCenter().TransformBy(Matrix3d.Displacement(vec));
                if (!boundary.IsPointInFast(pt))
                {
                    vec = -vec;
                }
                IniLanes.Add(new Lane(e, vec));
            }
        }

        /// <summary>
        /// Judge if it is legal building box.
        /// </summary>
        public void CheckBuildingBoxes()
        {
            for (int i = BuildingBoxes.Count - 1; i >= 0; i--)
            {
                var box = BuildingBoxes[i];
                var selected = ObstaclesSpatialIndex.SelectCrossingPolygon(box);
                if (selected == null || selected.Count == 0)
                    BuildingBoxes.RemoveAt(i);
            }
        }

        /// <summary>
        /// Judge if it is legal obstacle.
        /// </summary>
        public void CheckObstacles()
        {
            Obstacles = ObstaclesSpatialIndex.SelectCrossingPolygon(Boundary).Cast<Polyline>().ToList();
        }

        /// <summary>
        /// Main
        /// </summary>
        public void GenerateParkingSpaces()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                GenerateLanes();
                UpdateBoundary();
                GenerateCarSpots();
                UndateDataToGenerateCarSpotsInMinPartitionUnits();
                GenerateCarSpotsInMinPartitionUnits();
                PostProcess();
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
            Pillars.ForEach(e => e.Dispose());
            Dispose();
            return count;
        }

        /// <summary>
        /// Process and display the best solutions calculated by genetic algorithm.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="colorIndex"></param>
        public int ProcessAndDisplay(string layer = "0", int colorIndex = 0)
        {
            GenerateParkingSpaces();
            Dispose();
            Display(layer, colorIndex);
            return CarSpots.Count;
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
            Pillars.Select(e =>
            {
                e.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(15, 240, 206);
                return e;
            }).AddToCurrentSpace();
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
        /// Update boundary.
        /// </summary>
        private void UpdateBoundary()
        {
            if (BuildingBoxes.Count == 0)
            {
                NewBound = Boundary;
                return;
            }
            Extents3d bigext = new Extents3d();
            BuildingBoxes.ForEach(e => bigext.AddExtents(e.GeometricExtents));
            var bigbox = bigext.ToRectangle();
            DBObjectCollection objs = new DBObjectCollection();
            bigbox.Explode(objs);
            List<Line> edges = objs.Cast<Line>().ToList();
            var disy = edges.OrderBy(e => e.Length).First().Length / 2;
            List<Curve> crvs = new List<Curve>();
            Point3d pto = new Point3d();
            List<Vector3d> vecs = new List<Vector3d>();
            UpdateBoundaryEdge(ref crvs, ref pto, ref vecs, bigbox, edges, disy);
            bigbox.Dispose();
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
            for (int i = 0; i < edges.Count; i++)
            {
                //Line l = LineSDL(pto, vecs[i], 500000);
                Line l = new Line();
                try
                {
                    l = CreateLineFromStartPtAndVector(pto, vecs[i], 500000);
                }
                catch
                {
                }
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
                NewBoundEdges.Add(crv);
                l.Dispose();
            }
            NewBoundEdges = NewBoundEdges.Clone().Cast<Curve>().ToList();
            List<Polyline> pls = new List<Polyline>();
            List<Line> ls = new List<Line>();
            foreach (var r in NewBoundEdges)
            {
                if (r is Polyline) pls.Add((Polyline)r);
                else ls.Add((Line)r);
            }
            NewBound = JoinCurves(pls, ls)[0];
        }

        /// <summary>
        /// Generate car spots which is calaulted from intergral modules. 
        /// </summary>
        private void GenerateCarSpots()
        {
            RemoveDuplicateIniLanes();
            GenerateIntegralModuleCars();
            Laneboxes.AddRange(IniLanes.Select(e => e.Line.Buffer(DisLaneWidth / 2 - 10)).Distinct());
            if (IniLanes.Count == Countinilanes) return;
            GenerateCarsInSingleVerticalDirection();
            GenerateCarsInSingleParallelDirection();
        }

        /// <summary>
        /// Undate datas to generate car spots in the smallest partition unit.
        /// </summary>
        private void UndateDataToGenerateCarSpotsInMinPartitionUnits()
        {
            if (Math.Abs(Boundary.Area - NewBound.Area) < 1)
            {
                return;
            }
            Boundary = NewBound;
            var tmplanes = new List<Line>();
            for (int i = 0; i < NewBoundEdges.Count; i++)
            {
                foreach (var j in IniLanes)
                {
                    if (j.Line.GetClosestPointTo(NewBoundEdges[i].GetCenter(), false).DistanceTo(NewBoundEdges[i].GetCenter()) < 1)
                    {
                        tmplanes.Add((Line)NewBoundEdges[i]);
                        NewBoundEdges.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
            IniLanes = new List<Lane>();
            foreach (var e in tmplanes)
            {
                var vec = CreateVector(e).GetPerpendicularVector().GetNormal();
                var pt = e.GetCenter().TransformBy(Matrix3d.Displacement(vec));
                if (!Boundary.IsPointInFast(pt))
                {
                    vec = -vec;
                }
                IniLanes.Add(new Lane(e, vec));
            }
            Walls = new List<Polyline>();
            foreach (var k in NewBoundEdges)
            {
                if (k is Polyline) Walls.Add((Polyline)k);
                else
                {
                    Walls.Add(CreatePolyFromLine((Line)k));
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
            //IniLanes = IniLanes.OrderByDescending(e => e.Line.Length).ToList();
            SortLaneByDirection(IniLanes, LayoutMode);
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var inilanelines = new List<Line>(IniLanes.Select(e => e.Line).Cast<Line>().ToList());
                inilanelines.RemoveAt(i);
                var inilane = IniLanes[i];
                var l = CreateLine(inilane.Line);
                l.TransformBy(Matrix3d.Displacement(inilane.Vec.GetNormal() * (DisLaneWidth / 2 + DisVertCarWidth * 3)));
                var crossedmodules = SplitLine(l, ModuleBox).Cast<Line>().Where(e => !IsInAnyPolys(e.GetCenter(), ModuleBox)).ToList();
                var boundobstacles = new List<Polyline>();
                boundobstacles.AddRange(IniLanes.Select(e => CreatePolyFromLine(e.Line)));
                boundobstacles.RemoveAt(i);
                boundobstacles.AddRange(Walls);
                double length_module_used = 0;
                foreach (var cslane in crossedmodules)
                {
                    var lane = CreateLine(cslane);
                    lane.TransformBy(Matrix3d.Displacement(-inilane.Vec.GetNormal() * (DisLaneWidth / 2 + DisVertCarWidth * 3)));
                    if (ClosestPointInCurves(lane.StartPoint, inilanelines) < 1)
                    {
                        if (lane.Length < DisCarAndHalfLane)
                        {
                            lane.Dispose();
                            continue;
                        }
                        else
                            lane = new Line(lane.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(lane.StartPoint, lane.EndPoint).GetNormal() * DisCarAndHalfLane)), lane.EndPoint);
                    }
                    if (ClosestPointInCurves(lane.EndPoint, inilanelines) < 1)
                    {
                        if (lane.Length < DisCarAndHalfLane)
                        {
                            lane.Dispose();
                            continue;
                        }
                        else
                            lane = new Line(lane.StartPoint, lane.EndPoint.TransformBy(Matrix3d.Displacement(CreateVector(lane.EndPoint, lane.StartPoint).GetNormal() * DisCarAndHalfLane)));
                    }
                    LayoutOneDirection(lane, inilane.Vec, Obstacles, boundobstacles, ref length_module_used);
                    lane.Dispose();
                }
                IniLanes[i].RestLength = IniLanes[i].Line.Length - length_module_used;
                l.Dispose();
            }

            var tmplanes = new List<Lane>();
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var lane = CreateLine(IniLanes[i].Line);
                var matvec = IniLanes[i].Vec.GetNormal() * (DisLaneWidth / 2 + DisPillarDepth + 500);
                lane.TransformBy(Matrix3d.Displacement(matvec));
                var lbf = lane.Buffer(1);
                lbf.Scale(lbf.GetRecCentroid(), ScareFactorForCollisionCheck);
                var crosscars = CarSpatialIndex.SelectCrossingPolygon(lbf).Cast<Polyline>();
                var segs = SplitLine(lane, crosscars.ToList());
                segs.ForEach(e => e.TransformBy(Matrix3d.Displacement(-matvec)));
                if (segs.Count() > 0)
                {
                    var vec = IniLanes[i].Vec;
                    IniLanes.RemoveAt(i);
                    i--;
                    foreach (var seg in segs)
                    {
                        Lane ln = new Lane(seg, vec);
                        tmplanes.Add(ln);
                    }
                }
                lbf.Dispose();
                lane.Dispose();
            }
            IniLanes.AddRange(tmplanes);
            IniLanes=IniLanes.OrderByDescending(e => e.Line.Length).ToList();
            //IniLanes = IniLanes.OrderByDescending(e => e.RestLength).ToList();
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var inilanelines = new List<Line>(IniLanes.Select(e => e.Line).Cast<Line>().ToList());
                inilanelines.RemoveAt(i);
                var lane = CreateLine(IniLanes[i].Line);
                if (ClosestPointInVertCurves(lane.StartPoint,lane, inilanelines) < 1)
                {
                    if (lane.Length < DisCarAndHalfLane) continue;
                    else
                        lane = new Line(lane.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(lane.StartPoint, lane.EndPoint).GetNormal() * DisLaneWidth / 2)), lane.EndPoint);
                }
                if (ClosestPointInVertCurves(lane.EndPoint, lane,inilanelines) < 1)
                {
                    if (lane.Length < DisCarAndHalfLane) continue;
                    else
                        lane = new Line(lane.StartPoint, lane.EndPoint.TransformBy(Matrix3d.Displacement(CreateVector(lane.EndPoint, lane.StartPoint).GetNormal() * DisLaneWidth / 2)));
                }

                var boundobstacles = new List<Polyline>();
                boundobstacles.AddRange(IniLanes.Select(e => CreatePolyFromLine(e.Line)));
                boundobstacles.RemoveAt(i);
                boundobstacles.AddRange(Walls);
                GenerateRestVertAndParallelSpots(new List<Line>() { lane }, IniLanes[i].Vec, boundobstacles);
                lane.Dispose();
            }

        }

        /// <summary>
        /// Precess generated car spots finally.
        /// </summary>
        private void PostProcess()
        {
            RemoveDuplicateCars();
            RemoveCarsIntersectedWithBoundary();
            RemoveInvalidPillars();
        }

        private void RemoveInvalidPillars()
        {
            CarSpatialIndex = new ThCADCoreNTSSpatialIndex(CarSpots.Select(e => e.ToNTSPolygon().ToDbMPolygon()).ToCollection());
            Pillars = Pillars.Where(t =>
              {
                  var clone = t.Clone() as Polyline;
                  clone.Scale(clone.GetRecCentroid(), 0.5);
                  var cs = CarSpatialIndex.SelectCrossingPolygon(clone);
                  if (cs.Count > 0)
                  {
                      clone.Dispose();
                      return false;
                  }

                  if (ClosestPointInCurvesFast(clone.GetRecCentroid(), CarSpots) > DisPillarLength)
                  {
                      clone.Dispose();
                      return false;
                  }
                  clone.Dispose();
                  return true;
              }).ToList();
        }

        /// <summary>
        /// Generate integral module lanes.
        /// </summary>
        /// <returns></returns>
        private bool GenerateIntegralModuleLanes()
        {
            var generate_integral_modules = false;
            //IniLanes = IniLanes.OrderByDescending(e => e.Line.Length).ToList();
            SortLaneByDirection(IniLanes, LayoutMode);
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var vec = IniLanes[i].Vec;
                if (IniLanes[i].IsAdjLane) vec = -vec;
                var lane = IniLanes[i].Line;
                bool isNotInModules = true;
                if (CarModuleBox.Count > 0)
                    isNotInModules = ClosestPointInCurves(lane.GetCenter(), CarModuleBox) > 1;
                bool skip = (!IniLanes[i].CanBeMoved) || lane.Length < LengthCanGIntegralModules
                    || (IsInAnyPolys(lane.GetCenter(), CarModuleBox));
                if (skip) continue;
                var offsetlane = CreateLine(lane);
                offsetlane.TransformBy(Matrix3d.Displacement(vec * DisModulus));
                offsetlane.TransformBy(Matrix3d.Scaling(10, offsetlane.GetCenter()));
                var splited = SplitLine(offsetlane, Boundary).Where(e => Boundary.IsPointInFast(e.GetCenter())).Where(e => e.GetLength() > 0).ToArray();
                if (splited.Length > 0) offsetlane = (Line)splited.First();
                else continue;
                splited = SplitLine(offsetlane, CarModuleBox).ToArray();
                if (splited.Length > 1) offsetlane = (Line)splited.OrderBy(e => e.GetCenter().DistanceTo(lane.GetClosestPointTo(e.GetCenter(), false))).ToArray()[0];
                if (offsetlane.Length < DisLaneWidth / 2 + DisVertCarWidth * 4) continue;
                if (IsInAnyPolys(offsetlane.GetCenter(), CarModuleBox)) continue;
                var ply = CreatePolyFromPoints(new Point3d[] { lane.StartPoint, lane.EndPoint, offsetlane.EndPoint, offsetlane.StartPoint });
                bool isConnected = false;
                var distance = Math.Min(ClosestPointInCurves(offsetlane.StartPoint, IniLaneLines),
                    ClosestPointInCurves(offsetlane.EndPoint, IniLaneLines));
                if (distance < 10) isConnected = true;
                var plrsc = ply.Clone() as Polyline;
                plrsc.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, plrsc.Centroid()));
                var hascollision = ObstaclesSpatialIndex.Intersects(plrsc, true);
                if (ClosestPointInVertCurves(offsetlane.StartPoint, offsetlane, IniLanes.Select(e => e.Line).ToList()) < 1 &&
                    ClosestPointInVertCurves(offsetlane.EndPoint, offsetlane, IniLanes.Select(e => e.Line).ToList()) < 1 &&
                    offsetlane.Length < DisLaneWidth + DisVertCarWidth * 6) continue;
                if (isConnected && (!hascollision))
                {
                    var test_l = CreateLine(offsetlane);
                    test_l.TransformBy(Matrix3d.Displacement(vec * DisLaneWidth / 2));
                    var test_pl = CreatePolyFromPoints(new Point3d[] { offsetlane.StartPoint, offsetlane.EndPoint, test_l.EndPoint, test_l.StartPoint });
                    try
                    {
                        test_pl.TransformBy(Matrix3d.Scaling(ScareFactorForCollisionCheck, test_pl.Centroid()));
                    }
                    catch { }
                    var crossed = ObstaclesSpatialIndex.Intersects(test_pl, true) /*||*/
                        /*Boundary.Intersect(test_pl, Intersect.OnBothOperands).Count > 0*/;
                    test_l.Dispose();
                    test_pl.Dispose();
                    var closest_diatance_on_offset_direction = GetClosestDistanceOnOffsetDirection(offsetlane, vec, IniLaneLines);
                    var cond_allow_offset_bydistance = closest_diatance_on_offset_direction >= DisModulus
                        || closest_diatance_on_offset_direction <= DisLaneWidth / 2;

                    if (!crossed)
                    {
                        var dis = IsUnderAndNearObstacles(BuildingBoxes, offsetlane);
                        if (dis != -1 && dis - DisModulus > DisLaneWidth / 2 + DisVertCarWidth && IsHorizantal(offsetlane))
                        {
                            generate_integral_modules = true;
                            IniLanes[i].CanBeMoved = false;
                            var underl = CreateLine(offsetlane);
                            underl.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * (dis - DisLaneWidth / 2)));
                            Lane r = new Lane(underl, -Vector3d.YAxis);
                            IniLanes.Add(r);
                            IniLaneLines.Add(underl);
                            IntegralModule undermodule = new IntegralModule();
                            var ml = CreateLine(underl);
                            ml.TransformBy(Matrix3d.Displacement(-Vector3d.YAxis * DisModulus));
                            Lane dl = new Lane(ml, -Vector3d.YAxis);
                            IniLanes.Add(dl);
                            IniLaneLines.Add(ml);
                            var py = CreatePolyFromPoints(new Point3d[] { underl.StartPoint, underl.EndPoint, ml.EndPoint, ml.StartPoint });
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
                            offsetlane.Dispose();
                            break;
                        }
                        else
                        {
                            generate_integral_modules = true;
                            IniLanes[i].CanBeMoved = false;
                            Lane res = new Lane(offsetlane, vec);
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
                splited.ForEach(e => e.Dispose());
            }
            return generate_integral_modules;
        }

        /// <summary>
        /// Generate lane which is grown from the endpoint of the exist lane. 
        /// </summary>
        /// <returns></returns>
        private bool GenerateAdjLanes()
        {
            var generate_adj_lanes = false;
            //IniLanes = IniLanes.OrderByDescending(e => e.Line.Length).ToList();
            SortLaneByDirection(IniLanes, LayoutMode);
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var lane = IniLanes[i];
                if (lane.Line.Length <= DisCarAndHalfLane) continue;
                var lanes = IniLanes.Select(e => e.Line).Where(e => !IsParallelLine(lane.Line, e)).ToList();
                var distart = ClosestPointInCurves(lane.Line.StartPoint, lanes);
                var disend = ClosestPointInCurves(lane.Line.EndPoint, lanes);
                if (distart > 1 || lanes.Count == 0)
                {
                    var generated = GenerateAdjLanesFunc(lane, true);
                    if (generated)
                    {
                        generate_adj_lanes = true;
                        break;
                    }
                }
                if (disend > 1 || lanes.Count == 0)
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
        /// Remove duplicate iniLanes.
        /// </summary>
        private void RemoveDuplicateIniLanes()
        {
            if (IniLanes.Count < 2) return;
            for (int i = 1; i < IniLanes.Count; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    var conda = IniLanes[i].Line.StartPoint.DistanceTo(IniLanes[j].Line.StartPoint) < 1
                        && IniLanes[i].Line.EndPoint.DistanceTo(IniLanes[j].Line.EndPoint) < 1;
                    var condb = IniLanes[i].Line.StartPoint.DistanceTo(IniLanes[j].Line.EndPoint) < 1
                        && IniLanes[i].Line.EndPoint.DistanceTo(IniLanes[j].Line.StartPoint) < 1;
                    if (conda || condb)
                    {
                        IniLanes.RemoveAt(i);
                        i--;
                        break;
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
                var points = IniLanes.Select(e => e.Line.StartPoint).Where(e => module.Box.IsPointInFast(e)).ToList();
                if (points.Count == 0)
                {
                    var lanea = CreateLine(module.Lanes[0]);
                    var laneb = CreateLine(module.Lanes[1]);
                    Vector3d veca = CreateVector(lanea.GetCenter(), laneb.GetClosestPointTo(lanea.GetCenter(), true)).GetNormal() * DisLaneWidth / 2;
                    lanea.TransformBy(Matrix3d.Displacement(veca));
                    laneb.TransformBy(Matrix3d.Displacement(-veca));
                    UnifyLaneDirection(ref lanea, IniLanes);
                    UnifyLaneDirection(ref laneb, IniLanes);
                    veca = veca.GetNormal() * DisVertCarLength;
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
        /// Generate cars in single vertical direction.
        /// </summary>
        private void GenerateCarsInSingleVerticalDirection()
        {
            var lanes = IniLanes.Distinct().Where(e => Boundary.GetClosestPointTo(e.Line.GetCenter(), true).DistanceTo(e.Line.GetCenter()) <= DisModulus/* / 2 + 1000*/).ToList();
            var lines = lanes.Select(e => e.Line).ToList();
            var vecs = lanes.Select(e => CreateVector(e.Line).GetPerpendicularVector()).ToList();
            //var vecs = lanes.Select(e => e.Vec).ToList();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                UnifyLaneDirection(ref line, IniLanes);
                var offset = CreateLine(line);
                bool skip = false;
                bool cont = false;
                generate_cars_in_single_dir(offset, vecs[i], ref cont, ref skip, DisCarAndHalfLane, DisVertCarWidth);
                if (cont)
                {
                    offset.Dispose();
                    continue;
                }

                offset = CreateLine(line);
                cont = false;
                skip = false;
                generate_cars_in_single_dir(offset, -vecs[i], ref cont, ref skip, DisCarAndHalfLane, DisVertCarWidth);
                if (cont)
                {
                    offset.Dispose();
                    continue;
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
            var vecs = lanes.Select(e => CreateVector(e.Line).GetPerpendicularVector()).ToList();
            //var vecs = lanes.Select(e => e.Vec).ToList();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var offset = CreateLine(line);

                bool skip = false;
                bool cont = false;
                generate_cars_in_single_dir(offset, vecs[i], ref cont, ref skip, DisParallelCarWidth + DisLaneWidth / 2, DisParallelCarLength);
                if (cont)
                {
                    offset.Dispose();
                    continue;
                }

                offset = CreateLine(line);
                cont = false;
                skip = false;
                generate_cars_in_single_dir(offset, -vecs[i], ref cont, ref skip, DisParallelCarWidth + DisLaneWidth / 2, DisParallelCarLength);
                if (cont)
                {
                    offset.Dispose();
                    continue;
                }
            }
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
                    if (CarSpots[i].GetRecCentroid().DistanceTo(CarSpots[j].GetRecCentroid()) < 100)
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
            var obspls = Obstacles.Where(e => e.Closed).Where(e => e.Area > DisVertCarLength * DisLaneWidth * 5).ToList();
            CarSpots = CarSpots.Where(e => IniBoundary.Contains(e.GetRecCentroid()) && !IsInAnyPolys(e.GetRecCentroid(), obspls)).ToList();
        }

        public bool Equals(ParkingPartition other)
        {
            if (this.Boundary.NumberOfVertices != other.Boundary.NumberOfVertices) return false;
            var thisVertices = this.Boundary.Vertices();
            var otherVertices = other.Boundary.Vertices();
            for (int i = 0; i < this.Boundary.NumberOfVertices; i++)
            {
                if (!thisVertices[i].IsEqualTo(otherVertices[i])) return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            var hashcode = Boundary.NumberOfVertices;
            var thisVertices = this.Boundary.Vertices();
            foreach (var vertex in thisVertices)
            {
                hashcode ^= vertex.GetHashCode();
            }
            return hashcode;
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
            public bool IsAdjLane = false;
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
        }
    }
}