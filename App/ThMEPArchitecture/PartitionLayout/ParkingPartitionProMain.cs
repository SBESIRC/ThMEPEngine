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
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.ViewModel;
using ThMEPEngineCore;
using ThMEPEngineCore.CAD;
using static ThMEPArchitecture.PartitionLayout.GeoUtilities;

namespace ThMEPArchitecture.PartitionLayout
{
    public partial class ParkingPartitionPro
    {
        public ParkingPartitionPro() { }
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
                PillarSpacing = vm.ColumnWidth;
                GenerateMiddlePillars = vm.MidColumnInDoubleRowModular;
                DisPillarMoveDeeplyBackBack = vm.ColumnShiftDistanceOfDoubleRowModular;
                DisPillarMoveDeeplySingle = vm.ColumnShiftDistanceOfSingleRowModular;
                PillarNetLength = vm.ColumnSizeOfParalleToRoad;
                PillarNetDepth = vm.ColumnSizeOfPerpendicularToRoad;
                ThicknessOfPillarConstruct = vm.ColumnAdditionalSize;
                LayoutMode = ((int)vm.RunMode);
                HasImpactOnDepthForPillarConstruct = vm.ColumnAdditionalInfluenceLaneWidth;
            }
            GeneratePillars = PillarSpacing < DisVertCarWidth ? false : GeneratePillars;
            DisPillarLength = PillarNetLength + ThicknessOfPillarConstruct * 2;
            DisPillarDepth = PillarNetDepth + ThicknessOfPillarConstruct * 2;
            CountPillarDist = (int)Math.Floor((PillarSpacing - PillarNetLength - ThicknessOfPillarConstruct * 2) / DisVertCarWidth);
            DisCarAndHalfLane = DisLaneWidth / 2 + DisVertCarLength;
            DisModulus = DisCarAndHalfLane * 2;
            LengthCanGIntegralModulesConnectSingle = 4 * DisVertCarWidth + DisLaneWidth / 2;
            LengthCanGIntegralModulesConnectDouble = 6 * DisVertCarWidth + DisLaneWidth;
            LengthCanGAdjLaneConnectSingle = DisLaneWidth / 2 + DisVertCarWidth * 4;
            LengthCanGAdjLaneConnectDouble = DisLaneWidth + DisVertCarWidth * 8;
            GeneratePillars = gpillars;
            Walls = walls;
            Obstacles = obstacles;
            Boundary = boundary.DPSimplify(1);
            BoundingBox = Boundary.GeometricExtents.ToRectangle();
            MaxLength = BoundingBox.Length / 2;
            InitialzeDatas(iniLanes);
            Boundary = JoinCurves(walls, iniLanes)[0];
            DisHalfCarToPillar = (PillarSpacing - CountPillarDist * DisVertCarWidth - DisPillarLength) / 2;         
        }

        public List<Polyline> Walls;
        public List<Polyline> Obstacles;
        public List<Line> OriginalLanes=new List<Line>();
        public Polyline Boundary;
        public Polyline OutBoundary;
        private Polyline BoundingBox;
        private double MaxLength;
        public ThCADCoreNTSSpatialIndex ObstaclesSpatialIndex;
        public ThCADCoreNTSSpatialIndex CarBoxesSpatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
        public ThCADCoreNTSSpatialIndex LaneSpatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
        public ThCADCoreNTSSpatialIndex LaneBufferSpatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
        public ThCADCoreNTSSpatialIndex CarSpatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
        public List<Lane> IniLanes = new List<Lane>();
        public List<Polyline> CarSpots = new List<Polyline>();
        public List<InfoCar> Cars = new List<InfoCar>();
        public List<Polyline> Pillars = new List<Polyline>();
        private List<Polyline> CarBoxes = new List<Polyline>();
        private List<Polyline> IniLaneBoxes = new List<Polyline>();
        private List<CarBoxPlus> CarBoxesPlus = new List<CarBoxPlus>();
        private List<Polyline> LaneBoxes = new List<Polyline>();
        private List<CarModule> CarModules = new List<CarModule>();
        private List<Point3d> ObstacleVertexes = new List<Point3d>();
        public List<Polyline> BuildingBoxes = new List<Polyline>();
        public List<Ramps> RampList = new List<Ramps>();

        public static bool GeneratePillars = true;
        public static bool GenerateMiddlePillars = true;
        public static bool HasImpactOnDepthForPillarConstruct = true;
        public static bool GenerateLaneForLayoutingCarsInShearWall = true;
        public static double PillarNetLength = 500;
        public static double PillarNetDepth = 500;
        public static double ThicknessOfPillarConstruct = 50;
        public static double PillarSpacing = 7800;
        public static double DisVertCarLength = 5100;
        public static double DisVertCarWidth = 2400;
        public static double DisParallelCarLength = 6000;
        public static double DisParallelCarWidth = 2400;
        public static double DisLaneWidth = 5500;
        public static double CollisionD = 300;
        public static double CollisionCT = 1400;
        public static double CollisionCM = 1500;
        public static double DisPillarLength = PillarNetLength + ThicknessOfPillarConstruct * 2;
        public static double DisPillarDepth = PillarNetDepth + ThicknessOfPillarConstruct * 2;
        public static int CountPillarDist = (int)Math.Floor((PillarSpacing - PillarNetLength - ThicknessOfPillarConstruct * 2) / DisVertCarWidth);
        public static double DisCarAndHalfLane = DisLaneWidth / 2 + DisVertCarLength;
        public static double DisModulus = DisCarAndHalfLane * 2;
        public static double DisHalfCarToPillar = (PillarSpacing - CountPillarDist * DisVertCarWidth - DisPillarLength) / 2;
        public static double DisPillarMoveDeeplyBackBack = 1000;
        public static double DisPillarMoveDeeplySingle = 550;
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

        /// <summary>
        /// 在构造类时初始化数据
        /// </summary>
        /// <param name="iniLanes"></param>
        private void InitialzeDatas(List<Line> iniLanes)
        {
            int count = 0;
            //如果柱子完成面宽度对车道间距没有影响，则在一开始便将柱子缩小为净尺寸
            if (!HasImpactOnDepthForPillarConstruct)
            {
                DisPillarLength = PillarNetLength;
                DisPillarDepth = PillarNetDepth;
            }
            //输入的车道线有可能有碎线，将能合并join的join起来
            while (true)
            {
                count++;
                if (count > 10) break;
                if (iniLanes.Count < 2) break;
                for (int i = 0; i < iniLanes.Count - 1; i++)
                {
                    var joined = false;
                    for (int j = i + 1; j < iniLanes.Count; j++)
                    {
                        if (IsParallelLine(iniLanes[i], iniLanes[j]) && (iniLanes[i].StartPoint.DistanceTo(iniLanes[j].StartPoint) == 0
                            || iniLanes[i].StartPoint.DistanceTo(iniLanes[j].EndPoint) == 0
                            || iniLanes[i].EndPoint.DistanceTo(iniLanes[j].StartPoint) == 0
                            || iniLanes[i].EndPoint.DistanceTo(iniLanes[j].EndPoint) == 0))
                        {
                            var pl = JoinCurves(new List<Polyline>(), new List<Line>() { iniLanes[i], iniLanes[j] }).Cast<Polyline>().First();
                            var line=new Line(pl.StartPoint, pl.EndPoint);
                            iniLanes.RemoveAt(j);
                            iniLanes.RemoveAt(i);
                            iniLanes.Add(line);
                            joined = true;
                            break;
                        }
                    }
                    if (joined) break;
                }
            }    
            //将车道线构造为车道线Lane类
            foreach (var e in iniLanes)
            {
                var vec = CreateVector(e).GetPerpendicularVector().GetNormal();
                var pt = e.GetCenter().TransformBy(Matrix3d.Displacement(vec));
                if (!Boundary.Contains(pt))
                    vec = -vec;
                IniLanes.Add(new Lane(e, vec));
                OriginalLanes.Add(e);
            }
            Obstacles.ForEach(e => ObstacleVertexes.AddRange(e.Vertices().Cast<Point3d>()));
            IniLaneBoxes.AddRange(IniLanes.Select(e => e.Line.Buffer(DisLaneWidth / 2)));
            //CarBoxesSpatialIndex.Update(IniLanes.Select(e => e.Line.Buffer(DisLaneWidth / 2)).ToCollection(), new DBObjectCollection());
        }

        /// <summary>
        /// 验证输入的数据是否有效
        /// 如果输入的车道线靠近墙车道宽度不够，判断不合理的长度距离和，如果大于特定值，判断该输入数据无效。
        /// </summary>
        /// <returns></returns>
        public bool Validate()
        {
            return true;
            double length_judge_lane_nextto_wall = 30000;
            var lines = IniLanes.Select(e => e.Line);
            var pls = lines.Select(l => l.Buffer(DisLaneWidth / 2));
            var segs = new List<Curve>();
            try
            {
                segs = SplitCurve(OutBoundary, pls.ToCollection()).Where(e => IsInAnyBoxes(e.GetPointAtParam(e.EndParam / 2), pls.ToList())).ToList();
            }
            catch
            {
                return true;
            }
            double length = 0;
            segs.ForEach(e => length += e.GetLength());
            if (length > length_judge_lane_nextto_wall) return false;
            return true;
        }

        public void Dispose()
        {
            //Walls?.ForEach(e => e.Dispose());
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

        public int Process(List<InfoCar> cars, List<Polyline> pillars, List<Line> lanes, string carLayerName = "AI-停车位", string columnLayerName = "AI-柱子", int carindex = 30, int columncolor = -1)
        {
            GenerateParkingSpaces();
            cars.AddRange(Cars);
            pillars.AddRange(Pillars);
            lanes.AddRange(IniLanes.Select(e => CreateLine(e.Line)));
            Dispose();
            return CarSpots.Count;
        }

        /// <summary>
        /// 数据预处理
        /// 1. 如果车道线穿过建筑物了（靠近边界的情况），分割该车道线取第一段
        /// 2. 如果区域内含有坡道，从出入点到边界生成一条车道线
        /// </summary>
        private void PreProcess()
        {
            var iniLanes = IniLanes.Select(e => e.Line).ToList();
            for (int i = 0; i < iniLanes.Count; i++)
            {
                var line = iniLanes[i];
                var pl = line.Buffer(DisLaneWidth / 2 - 1);
                var points = new List<Point3d>();
                foreach (var obj in Obstacles)
                {
                    points.AddRange(obj.Vertices().Cast<Point3d>());
                    points.AddRange(obj.Intersect(pl, Intersect.OnBothOperands));
                }
                points = points.Where(e => pl.Contains(e) || pl.GetClosestPointTo(e, false).DistanceTo(e) < 0.001)
                    .Select(e => line.GetClosestPointTo(e, false)).ToList();
                var splits = SplitLine(line, points);
                for (int j = 0; j < splits.Count; j++)
                {
                    foreach (var obj in Obstacles)
                        if (obj.Contains(splits[j].GetCenter()))
                        {
                            splits.RemoveAt(j);
                            j--;
                            break;
                        }
                }
                splits = splits.OrderByDescending(e => ClosestPointInCurves(e.GetCenter(), Walls)).ToList();
                if (splits.Count > 0)
                {
                    var lane = splits.First();
                    IniLanes[i].Line = lane;
                }
                else
                {
                    IniLanes.RemoveAt(i);
                    i--;
                }
            }
            if (RampList.Count > 0)
            {
                var ramp = RampList[0];
                var pt = ramp.InsertPt;
                var pl = ramp.Ramp;
                var segobjs = new DBObjectCollection();
                pl.Explode(segobjs);
                var seg = segobjs.Cast<Line>().OrderByDescending(t => t.Length).First();
                var vec = CreateVector(seg).GetNormal();
                var ptest = pt.TransformBy(Matrix3d.Displacement(vec));
                if (pl.Contains(ptest)) vec = -vec;
                var rampline = CreateLineFromStartPtAndVector(pt, vec, MaxLength);
                rampline = SplitLine(rampline, IniLanes.Select(e => e.Line).ToList()).OrderBy(t => t.GetClosestPointTo(pt, false).DistanceTo(pt)).First();
                var prepvec = vec.GetPerpendicularVector();
                IniLanes.Add(new Lane(rampline, prepvec));
                IniLanes.Add(new Lane(rampline, -prepvec));
                OriginalLanes.Add(rampline);
                IniLaneBoxes.Add(rampline.Buffer(DisLaneWidth / 2));
                for (int i = 0; i < IniLanes.Count; i++)
                {
                    var line = IniLanes[i].Line;
                    var nvec = IniLanes[i].Vec;
                    var splits = SplitLine(line, rampline);
                    if (splits.Count() > 1)
                    {
                        IniLanes.RemoveAt(i);
                        IniLanes.Add(new Lane(splits[0], nvec));
                        IniLanes.Add(new Lane(splits[1], nvec));
                        break;
                    }
                }
            }
        }

        public void Display(string carLayerName = "AI-停车位", string columnLayerName = "AI-柱子", int carindex = 30, int columncolor = -1)
        {
            LayoutOutput.CarLayerName = carLayerName;
            LayoutOutput.ColumnLayerName = columnLayerName;
            LayoutOutput.InitializeLayer();
            var vertcar = LayoutOutput.VCar;
            var pcar = LayoutOutput.PCar;
            LayoutOutput layout = new LayoutOutput(Cars, Pillars);
            layout.DisplayColumns();
            layout.DisplayCars();

        }

        public void GenerateParkingSpaces()
        {
            PreProcess();
            GenerateLanes();
            GeneratePerpModules();
            GenerateCarsInModules();
            GenerateCarsOnRestLanes();
            PostProcess();
        }

        /// <summary>
        /// 生成三种类别的车道线：
        /// 1. 背靠背模块偏移出来的车道
        /// 2. 从车道线尽端生长出来的车道
        /// 3. 两道方向一致的建筑物之间生成一条车道
        /// 每生成一次比较各种生成的车道线的大小，选最大的作为该轮生成的车道线，直至三种方式均不能再生成车道线为止。
        /// </summary>
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

                var length_integral_modules = ((int)GenerateIntegralModuleLanesOptimizedByRealLength(ref paras_integral_modules, true));
                var length_adj_lanes = ((int)GenerateAdjacentLanesOptimizedByRealLength(ref paras_adj_lanes));
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
                else
                {
                    break;
                }
            }
            //在一个比较复杂的凹边形边界中，出现了一处inilanes中应该记录新生成的车道线但实际没有生成的情况
            //通过读取CarModules中的数据将该车道线重新记录进去
            foreach (var module in CarModules)
            {
                var lane = module.Line;
                bool found = false;
                foreach (var line in IniLanes.Select(f => f.Line))
                {
                    if (line.GetClosestPointTo(lane.GetPointAtParam(lane.EndParam/3), false).DistanceTo(lane.GetCenter()) < 1
                        && line.GetClosestPointTo(lane.GetPointAtParam(lane.EndParam / 3*2), false).DistanceTo(lane.GetCenter()) < 1
                        && line.GetClosestPointTo(lane.GetCenter(), false).DistanceTo(lane.GetCenter()) < 1)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    var lanecopied = new Line(lane.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(lane).GetNormal())),
                        lane.EndPoint.TransformBy(Matrix3d.Displacement(-CreateVector(lane).GetNormal())));
                    var buffer = lanecopied.Buffer(DisLaneWidth);
                    if (buffer.Intersect(Boundary, Intersect.OnBothOperands).Count == 0)
                        IniLanes.Add(new Lane(lane, module.Vec));
                }
            }
        }

        private double GenerateIntegralModuleLanesOptimizedByRealLength(ref GenerateLaneParas paras, bool allow_through_build = true)
        {
            double generate_lane_length;
            double max_length = -1;
            var isCurDirection = false;
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var _paras = new GenerateLaneParas();
                var length = GenerateIntegralModuleLanesForUniqueLaneOptimizedByRealLength(ref _paras, i, true);
                switch (LayoutMode)
                {
                    case 0:
                        {
                            if (length > max_length)
                            {
                                max_length = length;
                                paras = _paras;
                            }
                            break;
                        }
                    case 1:
                        {
                            if (IsHorizontalLine(IniLanes[i].Line) && !isCurDirection)
                            {
                                max_length = length;
                                paras = _paras;
                            }
                            else if (!IsHorizontalLine(IniLanes[i].Line) && isCurDirection) { }
                            else
                            {
                                if (length > max_length)
                                {
                                    max_length = length;
                                    paras = _paras;
                                }
                            }
                            if (IsHorizontalLine(IniLanes[i].Line)) isCurDirection = true;
                            break;
                        }
                    case 2:
                        {
                            if (IsVerticalLine(IniLanes[i].Line) && !isCurDirection)
                            {
                                max_length = length;
                                paras = _paras;
                            }
                            else if (!IsVerticalLine(IniLanes[i].Line) && isCurDirection) { }
                            else
                            {
                                if (length > max_length)
                                {
                                    max_length = length;
                                    paras = _paras;
                                }
                            }
                            if(IsVerticalLine(IniLanes[i].Line))isCurDirection = true;
                            break;
                        }
                }
            }
            generate_lane_length = max_length;
            return generate_lane_length;
        }

        private double GenerateIntegralModuleLanesForUniqueLaneOptimizedByRealLength(ref GenerateLaneParas paras, int i, bool allow_through_build = true)
        {
            double generate_lane_length = -1;
            var lane = IniLanes[i].Line;
            var vec = IniLanes[i].Vec;
            if (!IniLanes[i].CanBeMoved) return generate_lane_length;
            if (lane.Length < LengthCanGIntegralModulesConnectSingle) return generate_lane_length;
            var offsetlane = CreateLine(lane);
            offsetlane.TransformBy(Matrix3d.Displacement(vec * (DisModulus + DisLaneWidth / 2)));
            offsetlane.TransformBy(Matrix3d.Scaling(20, offsetlane.GetCenter()));
            //与边界相交
            var splits = SplitBufferLineByPoly(offsetlane, DisLaneWidth / 2, Boundary);
            var linesplitbounds =/* SplitLine(offsetlane, Boundary)*/
                splits
                .Where(e =>
                {
                    var l = CreateLine(e);
                    l.TransformBy(Matrix3d.Displacement(-vec * DisLaneWidth / 2));
                    l.StartPoint = l.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(l).GetNormal() * 10));
                    l.EndPoint = l.EndPoint.TransformBy(Matrix3d.Displacement(-CreateVector(l).GetNormal() * 10));
                    var bf = l.Buffer(DisLaneWidth / 2 - 1);
                    var result = bf.Intersect(Boundary, Intersect.OnBothOperands).Count == 0;
                    l.TransformBy(Matrix3d.Displacement(vec * DisLaneWidth / 2));
                    l.StartPoint = l.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(l).GetNormal() * 10));
                    l.EndPoint = l.EndPoint.TransformBy(Matrix3d.Displacement(-CreateVector(l).GetNormal() * 10));
                    bf = l.Buffer(DisLaneWidth / 2 - 1);
                    foreach (var wl in Walls)
                    {
                        if (bf.Intersect(wl, Intersect.OnBothOperands).Count > 0)
                        {
                            result = false;
                            break;
                        }
                    }
                    bf.Dispose();
                    l.Dispose();
                    return result;
                })
                .Where(e => Boundary.Contains(e.GetCenter()))
                .Where(e => e.Length > LengthCanGIntegralModulesConnectSingle)
                .Select(e =>
                {
                    e.TransformBy(Matrix3d.Displacement(-vec * (DisLaneWidth / 2)));
                    return e;
                })
                /*.Where(e => IsConnectedToLane(e))*/;
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
                    .Where(e => !IsInAnyBoxes(e.GetCenter()/*.TransformBy(Matrix3d.Displacement(-vec.GetNormal())) * 200*/, CarBoxes, true))
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
                    plbound = CreatPolyFromLines(linesplit, offsetback);
                    plbound.Scale(plbound.GetRecCentroid(), ScareFactorForCollisionCheck);
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
                        if (((lane.GetClosestPointTo(splitori.StartPoint, false).DistanceTo(splitori.StartPoint) >/* 5000*/splitori.Length / 3
                            || lane.GetClosestPointTo(splitori.EndPoint, false).DistanceTo(splitori.EndPoint) > splitori.Length / 3)
                            && ObstaclesSpatialIndex.SelectCrossingPolygon(ploritolane).Cast<Polyline>().Where(e => Boundary.Contains(e.GetCenter()) || Boundary.Intersect(e, Intersect.OnBothOperands).Count > 0).Count() > 0)
                            || IsInAnyBoxes(splitori.GetCenter(), CarBoxes))
                        {
                            //生成模块与车道线错开且原车道线碰障碍物
                            continue;
                        }
                        var distnearbuilding = IsEssentialToCloseToBuilding(splitori, vec);
                        if (distnearbuilding != -1)
                        {
                            //贴近建筑物生成
                            bool removed=false;
                            if (splitori.Length >= generate_lane_length && generate_lane_length > 0)
                            {
                                removed = true;
                                generate_lane_length = splitori.Length;
                            }
                            else if (splitori.Length >= generate_lane_length)
                                generate_lane_length = splitori.Length;
                            else if (generate_lane_length > 0)
                                removed = true;
                            else
                                continue;
                            if(!removed)
                                paras.SetNotBeMoved = i;
                            splitori.TransformBy(Matrix3d.Displacement(vec * distnearbuilding));
                            Lane lan = new Lane(splitori, vec);
                            paras.LanesToAdd.Add(lan);
                            paras.LanesToAdd.Add(new Lane(splitori, -vec));
                            paras.CarBoxesToAdd.Add(CreatePolyFromLine(splitori));               
                            quitcycle = true;
                            generate = true;
                            break;
                        }
                        if (IsConnectedToLane(split, true) && IsConnectedToLane(split, false) && split.Length < LengthCanGIntegralModulesConnectDouble) continue;
                        if (GetCommonLengthForTwoParallelLinesOnPerpDirection(split, lane) < 1) continue;
                        paras.SetNotBeMoved = i;
                        var pl = CreatPolyFromLines(split, splitback);
                        var plback = pl.Clone() as Polyline;
                        plback.TransformBy(Matrix3d.Displacement(-vec * DisCarAndHalfLane));
                        var split_splitori_points = plback.Intersect(Boundary, Intersect.OnBothOperands).Select(e => splitori.GetClosestPointTo(e, false)).ToList();
                        var mod = new CarModule(plback, splitori, vec);
                        if (split.Length >= generate_lane_length && generate_lane_length > 0)
                        {
                            paras.SetNotBeMoved = -1;
                            generate_lane_length = split.Length;
                        }
                        else if (split.Length >= generate_lane_length)
                            generate_lane_length = split.Length;
                        else if (generate_lane_length > 0)
                            paras.SetNotBeMoved = -1;
                        else
                            continue;
                        mod.IsInBackBackModule = true;
                        paras.CarModulesToAdd.Add(mod);
                        paras.CarBoxPlusToAdd.Add(new CarBoxPlus(plback));
                        paras.CarBoxesToAdd.Add(plback);
                        generate = true;
                        //generate_lane_length = split.Length;
                        double dis_to_move = 0;
                        Line perpLine = new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 0));
                        if (HasParallelLaneForwardExisted(split, vec, 28700 - 15700, /*19000 - 15700*//*0*/3000, ref dis_to_move, ref perpLine))
                        {
                            paras.CarBoxPlusToAdd[paras.CarBoxPlusToAdd.Count - 1].IsSingleForParallelExist = true;
                            var existBoxes = CarBoxesPlus.Where(e => e.IsSingleForParallelExist).Select(e => e.Box);
                            foreach (var box in existBoxes)
                            {
                                if (perpLine.Intersect(box, Intersect.OnBothOperands).Count > 0)
                                {
                                    paras.CarModulesToAdd.RemoveAt(paras.CarModulesToAdd.Count - 1);
                                    paras.CarBoxPlusToAdd.RemoveAt(paras.CarBoxPlusToAdd.Count - 1);
                                    paras.CarBoxesToAdd.RemoveAt(paras.CarBoxesToAdd.Count - 1);
                                    generate = false;
                                    generate_lane_length = -1;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            paras.CarBoxesToAdd.Add(pl);
                            CarModule module = new CarModule(pl, split, -vec);
                            module.IsInBackBackModule = true;
                            paras.CarModulesToAdd.Add(module);
                            Lane ln = new Lane(split, vec);
                            paras.LanesToAdd.Add(ln);
                        }

                    }
                    if (quitcycle) break;
                }
                if (quitcycle) break;
            }
            return generate_lane_length;
        }

        private double GenerateAdjacentLanesOptimizedByRealLength(ref GenerateLaneParas paras)
        {
            double generate_lane_length;
            double max_length = -1;
            var isCurDirection = false;
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var _paras = new GenerateLaneParas();
                var length = GenerateAdjacentLanesForUniqueLaneOptimizedByRealLength(ref _paras, i);
                switch (LayoutMode)
                {
                    case 0:
                        {
                            if (length > max_length)
                            {
                                max_length = length;
                                paras = _paras;
                            }
                            break;
                        }
                    case 1:
                        {
                            if (IsVerticalLine(IniLanes[i].Line) && !isCurDirection)
                            {
                                max_length = length;
                                paras = _paras;
                            }
                            else if (!IsVerticalLine(IniLanes[i].Line) && isCurDirection) { }
                            else
                            {
                                if (length > max_length)
                                {
                                    max_length = length;
                                    paras = _paras;
                                }
                            }
                            if (IsVerticalLine(IniLanes[i].Line)) isCurDirection = true;
                            break;
                        }
                    case 2:
                        {
                            if (IsHorizontalLine(IniLanes[i].Line) && !isCurDirection)
                            {
                                max_length = length;
                                paras = _paras;
                            }
                            else if (!IsHorizontalLine(IniLanes[i].Line) && isCurDirection) { }
                            else
                            {
                                if (length > max_length)
                                {
                                    max_length = length;
                                    paras = _paras;
                                }
                            }
                            if (IsHorizontalLine(IniLanes[i].Line)) isCurDirection = true;
                            break;
                        }
                }
            }
            generate_lane_length = max_length;
            return generate_lane_length;
        }

        private double GenerateAdjacentLanesForUniqueLaneOptimizedByRealLength(ref GenerateLaneParas paras, int i)
        {
            double generate_lane_length = -1;
            var lane = IniLanes[i];
            if (lane.Line.Length <= LengthCanGAdjLaneConnectSingle) return generate_lane_length;
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
                    if (gline.Length < LengthCanGAdjLaneConnectSingle) continue;
                    if (!IsConnectedToLane(gline)) continue;
                    bool quit = false;
                    foreach (var box in BuildingBoxes)
                    {
                        if (gline.Intersect(box, Intersect.OnBothOperands).Count > 0)
                        {
                            quit = true;
                            break;
                        }
                    }
                    if(quit) continue;
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
            if(paras.CarBoxPlusToAdd.Count > 0)CarBoxesPlus.AddRange(paras.CarBoxPlusToAdd);
            if (paras.CarModulesToAdd.Count > 0) CarModules.AddRange(paras.CarModulesToAdd);
        }

        /// <summary>
        /// 生成从车道线上垂直方向生成的模块
        /// </summary>
        private void GeneratePerpModules()
        {
            double mindistance = DisLaneWidth / 2 + DisVertCarWidth * 4;
            var lanes = GeneratePerpModuleLanes(mindistance, DisModulus, true, null, true);
            GeneratePerpModuleBoxes(lanes);
        }

        private void GenerateCarsInModules()
        {
            var lanes = new List<Lane>();
            CarModules.ForEach(e => lanes.Add(new Lane(e.Line, e.Vec)));
            for (int i = 0; i < lanes.Count; i++)
            {
                var vl = lanes[i].Line;
                var generate_middle_pillar = CarModules[i].IsInBackBackModule;
                var isin_backback = CarModules[i].IsInBackBackModule;
                if (!GenerateMiddlePillars) generate_middle_pillar = false;
                UnifyLaneDirection(ref vl, IniLanes);
                var line = CreateLine(vl);
                line = SplitLine(line, IniLaneBoxes).OrderBy(e => e.GetCenter().DistanceTo(line.GetCenter())).First();
                if (ClosestPointInVertLines(line.StartPoint, line, IniLanes.Select(e => e.Line)) < 10)
                    line.StartPoint = line.StartPoint.TransformBy(Matrix3d.Displacement(CreateVector(line).GetNormal() * DisLaneWidth / 2));
                if (ClosestPointInVertLines(line.EndPoint, line, IniLanes.Select(e => e.Line)) < 10)
                    line.EndPoint = line.EndPoint.TransformBy(Matrix3d.Displacement(-CreateVector(line).GetNormal() * (DisLaneWidth / 2 + DisPillarLength)));
                //if (line.Intersect(Boundary, Intersect.OnBothOperands).Count > 0)
                //{
                //    var lines = SplitLine(line, Boundary).Where(e => e.Length > 1)
                //        .Where(e => Boundary.Contains(e.GetCenter()) || ClosestPointInCurves(line.GetCenter(), OriginalLanes) == 0);
                //    if (lines.Count() > 0) line = lines.First();
                //    else continue;
                //}
                line.TransformBy(Matrix3d.Displacement(lanes[i].Vec.GetNormal() * DisLaneWidth / 2));
                GenerateCarsAndPillarsForEachLane(line, lanes[i].Vec, DisVertCarWidth, DisVertCarLength, false, false, false, false, true, false, true, false, true, true, generate_middle_pillar, isin_backback,true);
            }
        }

        private void UpdateLaneBoxAndSpatialIndexForGenerateVertLanes()
        {
            LaneSpatialIndex.Update(IniLanes.Select(e => CreatePolyFromLine(e.Line)).ToCollection(), new DBObjectCollection());
            LaneBoxes.AddRange(IniLanes.Select(e =>
            {
                //e.Line.Buffer(DisLaneWidth / 2 - 10));
                var la = CreateLine(e.Line);
                var lb = CreateLine(e.Line);
                la.TransformBy(Matrix3d.Displacement(CreateVector(la).GetPerpendicularVector().GetNormal() * (DisLaneWidth / 2 - 10)));
                lb.TransformBy(Matrix3d.Displacement(-CreateVector(la).GetPerpendicularVector().GetNormal() * (DisLaneWidth / 2 - 10)));
                var py = CreatPolyFromLines(la, lb);
                la.Dispose();
                lb.Dispose();
                return py;
            }));
            LaneBoxes.AddRange(CarModules.Select(e =>
            {
                //e.Line.Buffer(DisLaneWidth / 2 - 10));
                var la = CreateLine(e.Line);
                var lb = CreateLine(e.Line);
                la.TransformBy(Matrix3d.Displacement(CreateVector(la).GetPerpendicularVector().GetNormal() * (DisLaneWidth / 2 - 10)));
                lb.TransformBy(Matrix3d.Displacement(-CreateVector(la).GetPerpendicularVector().GetNormal() * (DisLaneWidth / 2 - 10)));
                var py = CreatPolyFromLines(la, lb);
                la.Dispose();
                lb.Dispose();
                return py;
            }));
            LaneBufferSpatialIndex.Update(LaneBoxes.ToCollection(), new DBObjectCollection());
        }

        /// <summary>
        /// 在车道线剩下的空间生成车位
        /// </summary>
        private void GenerateCarsOnRestLanes()
        {
            UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
            var vertlanes = GeneratePerpModuleLanes(DisVertCarLength + DisLaneWidth / 2, DisVertCarWidth, false, null, true);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                UnifyLaneDirection(ref vl, IniLanes);
                var line = CreateLine(vl);
                line.TransformBy(Matrix3d.Displacement(k.Vec.GetNormal() * DisLaneWidth / 2));
                GenerateCarsAndPillarsForEachLane(line, k.Vec, DisVertCarWidth, DisVertCarLength
                    , true, false, false, false, true, true, false, false, true, false, false, false, true);
            }
            vertlanes = GeneratePerpModuleLanes(DisParallelCarWidth + DisLaneWidth / 2, DisParallelCarLength, false);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                UnifyLaneDirection(ref vl, IniLanes);
                var line = CreateLine(vl);
                line.TransformBy(Matrix3d.Displacement(k.Vec.GetNormal() * DisLaneWidth / 2));
                GenerateCarsAndPillarsForEachLane(line, k.Vec, DisParallelCarLength, DisParallelCarWidth
                    , true, false, false, false, true, true, false);
            }
        }

        private void PostProcess()
        {
            //可以并行化
            RemoveDuplicateCars();
            RemoveCarsIntersectedWithBoundary();
            RemoveInvalidPillars();
            ReDefinePillarDimensions();
        }
    }
}