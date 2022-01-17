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
using ThMEPEngineCore;
using ThMEPEngineCore.CAD;
using static ThMEPArchitecture.PartitionLayout.GeoUtilities;

namespace ThMEPArchitecture.PartitionLayout
{
    public partial class ParkingPartitionPro
    {
        public ParkingPartitionPro()
        {

        }
        public ParkingPartitionPro(List<Polyline> walls, List<Line> iniLanes,
        List<Polyline> obstacles, Polyline boundary, ParkingStallArrangementViewModel vm = null, bool gpillars = true)
        {
            if (vm != null)
            {
                DisParallelCarLength = vm.ParallelSpotLength > vm.ParallelSpotWidth ? vm.ParallelSpotLength : vm.ParallelSpotWidth;
                DisParallelCarWidth = vm.ParallelSpotLength > vm.ParallelSpotWidth ? vm.ParallelSpotWidth : vm.ParallelSpotLength;
                DisVertCarLength = vm.VerticalSpotLength > vm.VerticalSpotWidth ? vm.VerticalSpotLength : vm.VerticalSpotWidth;
                DisVertCarWidth = vm.VerticalSpotLength > vm.VerticalSpotWidth ? vm.VerticalSpotWidth : vm.VerticalSpotLength;
                DisLaneWidth = vm.RoadWidth;
                MaxPillarSpacing = vm.MaxColumnWidth;
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
            LengthCanGIntegralModulesConnectSingle = 3 * DisVertCarWidth + DisLaneWidth / 2;
            LengthCanGIntegralModulesConnectDouble = 6 * DisVertCarWidth + DisLaneWidth;
            LengthCanGAdjLaneConnectSingle = DisLaneWidth / 2 + DisVertCarWidth * 3;
            LengthCanGAdjLaneConnectDouble = DisLaneWidth + DisVertCarWidth * 8;
            GeneratePillars = gpillars;
            Walls = walls;
            Obstacles = obstacles;
            Boundary = boundary;
            BoundingBox = Boundary.GeometricExtents.ToRectangle();
            MaxLength = BoundingBox.Length / 2;
            InitialzeDatas(iniLanes);
        }

        public List<Polyline> Walls;
        public List<Polyline> Obstacles;
        public Polyline Boundary;
        private Polyline BoundingBox;
        private double MaxLength;
        public ThCADCoreNTSSpatialIndex ObstaclesSpatialIndex;
        public ThCADCoreNTSSpatialIndex CarBoxesSpatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
        public ThCADCoreNTSSpatialIndex LaneSpatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
        public ThCADCoreNTSSpatialIndex CarSpatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
        public List<Lane> IniLanes = new List<Lane>();
        private List<Polyline> CarSpots = new List<Polyline>();
        private List<Polyline> Pillars = new List<Polyline>();
        private List<Polyline> CarBoxes = new List<Polyline>();
        private List<CarModule> CarModules = new List<CarModule>();
        private List<Point3d> ObstacleVertexes = new List<Point3d>();
        public List<Polyline> BuildingBoxes = new List<Polyline>();

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

        public static double LengthCanGIntegralModulesConnectSingle = 3 * DisVertCarWidth + DisLaneWidth / 2;
        public static double LengthCanGIntegralModulesConnectDouble = 6 * DisVertCarWidth + DisLaneWidth;
        public static double LengthCanGAdjLaneConnectSingle = DisLaneWidth / 2 + DisVertCarWidth * 3;
        public static double LengthCanGAdjLaneConnectDouble = DisLaneWidth + DisVertCarWidth * 8;

        const double ScareFactorForCollisionCheck = 0.99;

        public static int LayoutMode = ((int)LayoutDirection.LENGTH);
        public enum LayoutDirection : int
        {
            LENGTH = 0,
            HORIZONTAL = 1,
            VERTICAL = 2
        }

        private void InitialzeDatas(List<Line> iniLanes)
        {
            foreach (var e in iniLanes)
            {
                var vec = CreateVector(e).GetPerpendicularVector().GetNormal();
                var pt = e.GetCenter().TransformBy(Matrix3d.Displacement(vec));
                if (!Boundary.Contains(pt))
                    vec = -vec;
                IniLanes.Add(new Lane(e, vec));
            }
            Obstacles.ForEach(e => ObstacleVertexes.AddRange(e.Vertices().Cast<Point3d>()));
        }

        public void Dispose()
        {
            Walls?.ForEach(e => e.Dispose());
            Boundary?.Dispose();
            BoundingBox?.Dispose();
            IniLanes?.ForEach(e => e.Line?.Dispose());
            CarBoxes?.ForEach(e => e.Dispose());
            CarBoxesSpatialIndex?.Dispose();
            CarModules?.ForEach(e => e.Box?.Dispose());
            BuildingBoxes?.ForEach(e => e.Dispose());
        }

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

        public int ProcessAndDisplay(string carLayerName = "AI-停车位", string columnLayerName = "AI-柱子", int carindex = 30, int columncolor = -1)
        {
            GenerateParkingSpaces();
            Dispose();
            Display();
            return CarSpots.Count;
        }

        public void Display(string carLayerName = "AI-停车位", string columnLayerName = "AI-柱子", int carindex = 30, int columncolor = -1)
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                if (!adb.Layers.Contains(carLayerName))
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(adb.Database, carLayerName, 0);
                }
                if (!adb.Layers.Contains(columnLayerName))
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(adb.Database, columnLayerName, 0);
                }
            }
            CarSpots.Select(e =>
            {
                e.Layer = carLayerName;
                e.ColorIndex = carindex;
                return e;
            }).AddToCurrentSpace();
            Pillars.Select(e =>
            {
                e.Layer = columnLayerName;
                if (columncolor < 0)
                    e.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(15, 240, 206);
                else e.ColorIndex = columncolor;
                return e;
            }).AddToCurrentSpace();
        }

        public void GenerateParkingSpaces()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                LogMomery("Before GenerateLanes : ");
                GenerateLanes();
                LogMomery("Before GeneratePerpModules : ");
                GeneratePerpModules();
                LogMomery("Before GenerateCarsInModules : ");
                GenerateCarsInModules();
                LogMomery("Before GenerateCarsOnRestLanes : ");
                GenerateCarsOnRestLanes();
                LogMomery("Before PostProcess : ");
                PostProcess();
                LogMomery("After PostProcess : ");
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.WaitForFullGCComplete();
                LogMomery("After GCinPartition : ");
            }
        }

        private void GenerateLanes()
        {
            int count = 0;
            while (true)
            {
                count++;
                if (count > 20) break;

                SortLaneByDirection(IniLanes, LayoutMode);
                GenerateLaneParas paras_integral_modules = new GenerateLaneParas();
                GenerateLaneParas paras_adj_lanes = new GenerateLaneParas();
                GenerateLaneParas paras_between_two_builds = new GenerateLaneParas();
                var length_integral_modules = ((int)GenerateIntegralModuleLanes(ref paras_integral_modules, true));
                var length_adj_lanes = ((int)GenerateAdjacentLanes(ref paras_adj_lanes));
                var length_between_two_builds = ((int)GenerateLaneBetweenTwoBuilds(ref paras_between_two_builds));
                var max = Math.Max(Math.Max(length_integral_modules, length_adj_lanes), Math.Max(length_adj_lanes, length_between_two_builds));
                if (max > 0)
                {
                    if (max == length_integral_modules)
                    {
                        RealizeGenerateLaneParas(paras_integral_modules);
                    }
                    else if (max == length_adj_lanes)
                    {
                        RealizeGenerateLaneParas(paras_adj_lanes);
                    }
                    else
                    {
                        RealizeGenerateLaneParas(paras_between_two_builds);
                    }
                }
                else break;
            }
        }

        private double GenerateIntegralModuleLanes(ref GenerateLaneParas paras, bool allow_through_build = true)
        {
            double generate_lane_length = -1;
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var lane = IniLanes[i].Line;
                var vec = IniLanes[i].Vec;
                if (!IniLanes[i].CanBeMoved) continue;
                if (lane.Length < LengthCanGIntegralModulesConnectSingle) continue;
                var offsetlane = CreateLine(lane);
                offsetlane.TransformBy(Matrix3d.Displacement(vec * DisModulus));
                offsetlane.TransformBy(Matrix3d.Scaling(10, offsetlane.GetCenter()));
                //与边界相交
                var linesplitbounds = SplitLine(offsetlane, Boundary)
                    .Where(e => Boundary.Contains(e.GetCenter()))
                    .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
                    .Where(e => IsConnectedToLane(e));
                bool generate = false;
                var quitcycle = false;
                foreach (var linesplitbound in linesplitbounds)
                {
                    //与车道模块相交
                    var linesplitboundback = CreateLine(linesplitbound);
                    linesplitboundback.TransformBy(Matrix3d.Displacement(-vec * (DisVertCarLength + DisLaneWidth / 2)));
                    var plcarbox = CreatPolyFromLines(linesplitbound, linesplitboundback);
                    plcarbox.Scale(plcarbox.GetRecCentroid(), ScareFactorForCollisionCheck);
                    var linesplitcarboxes = SplitLineBySpacialIndexInPoly(linesplitbound, plcarbox, CarBoxesSpatialIndex, false)
                        .Where(e => !IsInAnyBoxes(e.GetCenter()/*.TransformBy(Matrix3d.Displacement(-vec.GetNormal())) * 200*/, CarBoxes,true))
                        //.Where(e =>
                        //{
                        //    return !IsInAnyBoxes(AveragePoint(e.GetCenter(), linesplitboundback.GetClosestPointTo(e.GetCenter(), true)), CarBoxes);
                        //})
                        .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
                        .Where(e => IsConnectedToLane(e));
                    //解决车道线与车道模块短边平行长度不够的情况
                    var fixlinesplitcarboxes = new List<Line>();
                    foreach (var tmplinesplitcarboxes in linesplitcarboxes)
                    {
                        var k = CreateLine(tmplinesplitcarboxes);
                        k.TransformBy(Matrix3d.Displacement(vec * DisLaneWidth / 2));
                        var boxs = CarBoxes.Where(f =>
                          {
                              var segs = new DBObjectCollection();
                              f.Explode(segs);
                              var seg = segs.Cast<Line>().Where(s => Math.Abs(s.Length - DisCarAndHalfLane) > 1).First();
                              if (IsPerpLine(seg, k))
                              {
                                  segs.Dispose();
                                  return true;
                              }
                              else
                              {
                                  segs.Dispose();
                                  return false;
                              }
                          }).Select(box => box.Clone() as Polyline).ToList();
                        var spindex = new ThCADCoreNTSSpatialIndex(boxs.ToCollection());
                        var plcarboxfix = CreatPolyFromLines(k, linesplitboundback);
                        plcarboxfix.Scale(plcarbox.GetRecCentroid(), ScareFactorForCollisionCheck);
                        fixlinesplitcarboxes.AddRange(SplitLineBySpacialIndexInPoly(k, plcarboxfix, spindex, false)
                            .Where(e => !IsInAnyBoxes(e.GetCenter(), boxs, true))
                            .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
                            .Where(e =>
                            {
                                var ep = CreateLine(e);
                                ep.TransformBy(Matrix3d.Displacement(-vec * DisLaneWidth / 2));
                                if (IsConnectedToLane(ep))
                                {
                                    ep.Dispose();
                                    return true;
                                }
                                else
                                {
                                    ep.Dispose();
                                    return false;
                                }
                            }).Select(e =>
                            {
                                e.StartPoint = e.StartPoint.TransformBy(Matrix3d.Displacement(-vec * DisLaneWidth / 2));
                                e.EndPoint = e.EndPoint.TransformBy(Matrix3d.Displacement(-vec * DisLaneWidth / 2));
                                return e;
                            })
                            );
                        spindex.Dispose();
                        boxs.ForEach(e => e.Dispose());
                        plcarboxfix.Dispose();
                    }
                    foreach (var linesplit in fixlinesplitcarboxes)
                    {
                        var offsetback = CreateLine(linesplit);
                        offsetback.TransformBy(Matrix3d.Displacement(-vec * (DisVertCarLength + DisLaneWidth / 2)));
                        var plbound = CreatPolyFromLines(linesplit, offsetback);
                        plbound.Scale(plbound.GetRecCentroid(), ScareFactorForCollisionCheck);
                        if (!allow_through_build)
                        {
                            if (SplitLineBySpacialIndexInPoly(linesplit, plbound, ObstaclesSpatialIndex, false).Count > 1) continue;
                        }
                        //与障碍物相交
                        linesplit.TransformBy(Matrix3d.Displacement(vec * DisLaneWidth / 2));
                        var obsplits = SplitLineBySpacialIndexInPoly(linesplit, plbound, ObstaclesSpatialIndex, false)
                            .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
                            .Where(e => !IsInAnyPolys(e.GetCenter(), Obstacles))
                            .Where(e =>
                            {
                                //与原始车道线模块不相接
                                var tmpline = CreateLine(e);
                                tmpline.TransformBy(Matrix3d.Displacement(-vec * DisLaneWidth / 2));
                                if (!IsConnectedToLane(tmpline)) return false;
                                var ptonlane = lane.GetClosestPointTo(e.GetCenter(), false);
                                var ptone = e.GetClosestPointTo(ptonlane, false);
                                if (ptonlane.DistanceTo(ptone) - DisModulus - DisLaneWidth / 2 > 1) return false;
                                else return true;
                            });

                        foreach (var split in obsplits)
                        {
                            var splitback = CreateLine(split);
                            split.TransformBy(Matrix3d.Displacement(-vec * DisLaneWidth / 2));
                            splitback.TransformBy(Matrix3d.Displacement(-vec * (DisVertCarLength + DisLaneWidth)));
                            var splitori = CreateLine(splitback);
                            splitori.TransformBy(Matrix3d.Displacement(-vec * (DisVertCarLength + DisLaneWidth)));
                            var ploritolane = CreatPolyFromLines(splitback, splitori);
                            splitori.TransformBy(Matrix3d.Displacement(vec * DisLaneWidth / 2));
                            if ((lane.GetClosestPointTo(splitori.StartPoint, false).DistanceTo(splitori.StartPoint) > 5000
                                || lane.GetClosestPointTo(splitori.EndPoint, false).DistanceTo(splitori.EndPoint) > 5000)
                                && ObstaclesSpatialIndex.SelectCrossingPolygon(ploritolane).Count > 0)
                            {
                                //生成模块与车道线错开且原车道线碰障碍物
                                continue;
                            }
                            var distnearbuilding = IsEssentialToCloseToBuilding(splitori, vec);
                            if (distnearbuilding != -1)
                            {
                                //贴近建筑物生成
                                paras.SetNotBeMoved = i;
                                splitori.TransformBy(Matrix3d.Displacement(vec * distnearbuilding));
                                Lane lan = new Lane(splitori, vec);
                                paras.LanesToAdd.Add(lan);
                                paras.LanesToAdd.Add(new Lane(splitori, -vec));
                                paras.CarBoxesToAdd.Add(CreatePolyFromLine(splitori));
                                generate_lane_length = splitori.Length;
                                quitcycle = true;
                                generate = true;
                                break;
                            }
                            if (HasParallelLaneForwardExisted(split, vec, DisVertCarWidth + DisLaneWidth, 6000)) continue;
                            if (IsConnectedToLane(split, true) && IsConnectedToLane(split, false) && split.Length < LengthCanGIntegralModulesConnectDouble) continue;
                            paras.SetNotBeMoved = i;
                            var pl = CreatPolyFromLines(split, splitback);
                            var plback = pl.Clone() as Polyline;
                            plback.TransformBy(Matrix3d.Displacement(-vec * DisCarAndHalfLane));
                            paras.CarBoxesToAdd.Add(pl);
                            CarModule module = new CarModule(pl, split, -vec);
                            paras.CarModulesToAdd.Add(module);
                            paras.CarModulesToAdd.Add(new CarModule(plback, splitori, vec));
                            paras.CarBoxesToAdd.Add(plback);
                            Lane ln = new Lane(split, vec);
                            paras.LanesToAdd.Add(ln);
                            generate = true;
                            generate_lane_length = split.Length;
                        }
                        if (quitcycle) break;
                    }
                    if (quitcycle) break;
                }
                if (generate) break;
            }
            return generate_lane_length;
        }

        private double GenerateAdjacentLanes(ref GenerateLaneParas paras)
        {
            double generate_lane_length = -1;
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var lane = IniLanes[i];
                if (lane.Line.Length <= LengthCanGAdjLaneConnectSingle) continue;
                if (CloseToWall(lane.Line.StartPoint) && !lane.GStartAdjLine)
                {
                    var generated = GenerateAdjacentLanesFunc(ref paras, lane, i, true);
                    if (generated != -1)
                    {
                        return generated;
                    }
                }
                else if (CloseToWall(lane.Line.EndPoint) && !lane.GEndAdjLine)
                {
                    var generated = GenerateAdjacentLanesFunc(ref paras, lane, i, false);
                    if (generated != -1)
                    {
                        return generated;
                    }
                }
            }
            return generate_lane_length;
        }

        private double GenerateLaneBetweenTwoBuilds(ref GenerateLaneParas paras)
        {
            double generate_lane_length = -1;
            if (BuildingBoxes.Count <= 1) return generate_lane_length;
            for (int i = 0; i < BuildingBoxes.Count - 1; i++)
            {
                for (int j = i + 1; j < BuildingBoxes.Count; j++)
                {
                    var pcenter_i = BuildingBoxes[i].GetRecCentroid();
                    var pcenter_j = BuildingBoxes[j].GetRecCentroid();
                    var line_ij = new Line(pcenter_i, pcenter_j);
                    var lines = SplitLine(line_ij, BuildingBoxes).Where(e => !IsInAnyBoxes(e.GetCenter(), BuildingBoxes));
                    line_ij = ChangeLineToBeOrthogonal(line_ij);
                    if (BuildingBoxes.Count > 2)
                    {
                        bool quitcycle = false;
                        for (int k = 0; k < BuildingBoxes.Count; k++)
                        {
                            if (k != i && k != j)
                            {
                                var p = line_ij.GetCenter();
                                var lt = new Line(p.TransformBy(Matrix3d.Displacement(CreateVector(line_ij).GetNormal().GetPerpendicularVector() * MaxLength)),
                                    p.TransformBy(Matrix3d.Displacement(-CreateVector(line_ij).GetNormal().GetPerpendicularVector() * MaxLength)));
                                var bf = lt.Buffer(line_ij.Length / 2);
                                if (bf.Intersect(BuildingBoxes[k], Intersect.OnBothOperands).Count > 0 || bf.Contains(BuildingBoxes[k].GetRecCentroid()))
                                {
                                    quitcycle = true;
                                    break;
                                }
                            }
                        }
                        if (quitcycle) continue;
                    }
                    if (lines.Count() == 0) continue;
                    var line = lines.First();
                    line = ChangeLineToBeOrthogonal(line);
                    if (line.Length < DisCarAndHalfLane) continue;
                    Point3d ps = new Point3d();
                    if (Math.Abs(line.StartPoint.X - line.EndPoint.X) > 1)
                    {
                        if (line.StartPoint.X < line.EndPoint.X) line.ReverseCurve();
                        ps = line.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(line).GetNormal() * DisCarAndHalfLane));
                    }
                    else if (Math.Abs(line.StartPoint.Y - line.EndPoint.Y) > 1)
                    {
                        if (line.StartPoint.Y < line.EndPoint.Y) line.ReverseCurve();
                        ps = line.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(line).GetNormal() * DisLaneWidth / 2));
                    }
                    var vec = CreateVector(line).GetPerpendicularVector().GetNormal();
                    var gline = new Line(ps.TransformBy(Matrix3d.Displacement(vec * MaxLength)), ps.TransformBy(Matrix3d.Displacement(-vec * MaxLength)));
                    var glines = SplitLine(gline, Boundary).Where(e => Boundary.Contains(e.GetCenter()))
                        .Where(e => e.Length > 1)
                        .OrderBy(e => e.GetClosestPointTo(ps, false).DistanceTo(ps));
                    if (glines.Count() == 0) continue;
                    gline = glines.First();
                    glines = SplitLine(gline, CarBoxes).Where(e => !IsInAnyBoxes(e.GetCenter(), CarBoxes))
                        .Where(e => e.Length > 1)
                        .OrderBy(e => e.GetClosestPointTo(ps, false).DistanceTo(ps));
                    if (glines.Count() == 0) continue;
                    gline = glines.First();
                    if (ClosestPointInCurves(gline.GetCenter(), IniLanes.Select(e => e.Line).ToList()) < 1)
                        continue;
                    paras.LanesToAdd.Add(new Lane(gline, CreateVector(line).GetNormal()));
                    paras.LanesToAdd.Add(new Lane(gline, -CreateVector(line).GetNormal()));
                    paras.CarBoxesToAdd.Add(CreatePolyFromLine(gline));
                    generate_lane_length = gline.Length;
                }
            }
            return generate_lane_length;
        }

        private void RealizeGenerateLaneParas(GenerateLaneParas paras)
        {
            if (paras.SetNotBeMoved != -1) IniLanes[paras.SetNotBeMoved].CanBeMoved = false;
            if (paras.SetGStartAdjLane != -1) IniLanes[paras.SetGStartAdjLane].GStartAdjLine = true;
            if (paras.SetGEndAdjLane != -1) IniLanes[paras.SetGEndAdjLane].GEndAdjLine = true;
            if (paras.LanesToAdd.Count > 0) IniLanes.AddRange(paras.LanesToAdd);
            if (paras.CarBoxesToAdd.Count > 0)
            {
                CarBoxes.AddRange(paras.CarBoxesToAdd);
                CarBoxesSpatialIndex.Update(paras.CarBoxesToAdd.ToCollection(), new DBObjectCollection());
            }
            if (paras.CarModulesToAdd.Count > 0) CarModules.AddRange(paras.CarModulesToAdd);
        }

        private void GeneratePerpModules()
        {
            double mindistance = DisLaneWidth / 2 + DisVertCarWidth * 4;
            var lanes = GeneratePerpModuleLanes(mindistance, DisModulus);
            GeneratePerpModuleBoxes(lanes);
        }

        private void GenerateCarsInModules()
        {
            var lanes = new List<Lane>();
            CarModules.ForEach(e => lanes.Add(new Lane(e.Line, e.Vec)));
            foreach (var k in lanes)
            {
                var vl = k.Line;
                UnifyLaneDirection(ref vl, IniLanes);
                var line = CreateLine(vl);
                if (ClosestPointInVertLines(line.StartPoint, line, IniLanes.Select(e => e.Line)) < 10)
                    line.StartPoint = line.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(line).GetNormal() * DisLaneWidth / 2));
                if (ClosestPointInVertLines(line.EndPoint, line, IniLanes.Select(e => e.Line)) < 10)
                    line.EndPoint = line.EndPoint.TransformBy(Matrix3d.Displacement(-CreateVector(line).GetNormal() * DisLaneWidth / 2));
                line.TransformBy(Matrix3d.Displacement(k.Vec.GetNormal() * DisLaneWidth / 2));
                GenerateCarsAndPillarsForEachLane(line, k.Vec, DisVertCarWidth, DisVertCarLength, false, false);
            }
        }

        private void GenerateCarsOnRestLanes()
        {
            LaneSpatialIndex.Update(IniLanes.Select(e => CreatePolyFromLine(e.Line)).ToCollection(), new DBObjectCollection());
            var vertlanes = GeneratePerpModuleLanes(DisVertCarLength + DisLaneWidth / 2, DisVertCarWidth);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                UnifyLaneDirection(ref vl, IniLanes);
                var line = CreateLine(vl);
                line.TransformBy(Matrix3d.Displacement(k.Vec.GetNormal() * DisLaneWidth / 2));
                GenerateCarsAndPillarsForEachLane(line, k.Vec, DisVertCarWidth, DisVertCarLength);
            }
            vertlanes = GeneratePerpModuleLanes(DisParallelCarWidth + DisLaneWidth / 2, DisParallelCarLength);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                UnifyLaneDirection(ref vl, IniLanes);
                var line = CreateLine(vl);
                line.TransformBy(Matrix3d.Displacement(k.Vec.GetNormal() * DisLaneWidth / 2));
                GenerateCarsAndPillarsForEachLane(line, k.Vec, DisParallelCarLength, DisParallelCarWidth);
            }
        }

        private void PostProcess()
        {
            RemoveDuplicateCars();
            RemoveCarsIntersectedWithBoundary();
            RemoveInvalidPillars();
        }

    }

    public class GenerateLaneParas
    {
        public int SetNotBeMoved = -1;
        public int SetGStartAdjLane = -1;
        public int SetGEndAdjLane = -1;
        public List<Lane> LanesToAdd = new List<Lane>();
        public List<Polyline> CarBoxesToAdd = new List<Polyline>();
        public List<CarModule> CarModulesToAdd = new List<CarModule>();
        public void Dispose()
        {
            LanesToAdd.ForEach(e => e.Line.Dispose());
            CarBoxesToAdd.ForEach(e => e.Dispose());
            CarModulesToAdd.ForEach(e => e.Line.Dispose());
            CarModulesToAdd.ForEach(e => e.Box.Dispose());
        }
    }

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
        public bool GStartAdjLine = false;
        public bool GEndAdjLine = false;
    }

    public class PerpModlues
    {
        public List<Line> Lanes;
        public int Mminindex;
        public int Count;
        public Vector3d Vec;
        public List<Polyline> Bounds;
    }

    public class CarModule
    {
        public CarModule() { }
        public CarModule(Polyline box, Line line, Vector3d vec)
        {
            Box = box;
            Line = line;
            Vec = vec;
        }
        public bool GenerateCars = true;
        public Polyline Box;
        public Line Line;
        public Vector3d Vec;
    }
}