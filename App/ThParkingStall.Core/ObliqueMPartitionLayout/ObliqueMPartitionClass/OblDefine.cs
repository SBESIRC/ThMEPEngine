using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.LaneDeformation;
using ThParkingStall.Core.MPartitionLayout;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.ObliqueMPartitionLayout
{
    public partial class ObliqueMPartition
    {
        public ObliqueMPartition()
        {

        }
        public ObliqueMPartition(List<LineString> walls, List<LineSegment> inilanes,
            List<Polygon> obstacles, Polygon boundary, bool gpillars = true)
        {
            //viewmodel
            DisParallelCarLength = VMStock.ParallelSpotLength > VMStock.ParallelSpotWidth ? VMStock.ParallelSpotLength : VMStock.ParallelSpotWidth;
            DisParallelCarWidth = VMStock.ParallelSpotLength > VMStock.ParallelSpotWidth ? VMStock.ParallelSpotWidth : VMStock.ParallelSpotLength;
            //DisVertCarLength = VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotLength + 200 : VMStock.VerticalSpotWidth + 200;
            DisVertCarLength = Math.Max(VMStock.VerticalSpotLength, VMStock.VerticalSpotWidth);
            DisVertCarWidth = VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotWidth : VMStock.VerticalSpotLength;
            DisLaneWidth = VMStock.RoadWidth;
            PillarSpacing = VMStock.ColumnWidth;
            GenerateMiddlePillars = VMStock.MidColumnInDoubleRowModular;
            DisPillarMoveDeeplyBackBack = VMStock.ColumnShiftDistanceOfDoubleRowModular;
            DisPillarMoveDeeplySingle = VMStock.ColumnShiftDistanceOfSingleRowModular;
            PillarNetLength = VMStock.ColumnSizeOfParalleToRoad;
            PillarNetDepth = VMStock.ColumnSizeOfPerpendicularToRoad;
            ThicknessOfPillarConstruct = VMStock.ColumnAdditionalSize;
            AllowCompactedLane = VMStock.BoundaryShrink;
            //VMStock缺少RunMode参数
            LayoutMode = ((int)VMStock.RunMode);
            if (LayoutMode == 0)
                LayoutMode = ((int)LayoutDirection.FOLLOWPREVIOUS);
            HasImpactOnDepthForPillarConstruct = VMStock.ColumnAdditionalInfluenceLaneWidth;
            //viewmodel参数赋值完毕
            GeneratePillars = PillarSpacing < DisVertCarWidth ? false : GeneratePillars;
            DisPillarLength = PillarNetLength + ThicknessOfPillarConstruct * 2;
            DisPillarDepth = PillarNetDepth + ThicknessOfPillarConstruct * 2;
            DisPillarMoveDeeplyBackBack = DisPillarMoveDeeplyBackBack > DisPillarDepth / 2 ? DisPillarMoveDeeplyBackBack : DisPillarDepth / 2;
            DisPillarMoveDeeplySingle = DisPillarMoveDeeplySingle > DisPillarDepth / 2 ? DisPillarMoveDeeplySingle : DisPillarDepth / 2;
            CountPillarDist = (int)Math.Floor((PillarSpacing - PillarNetLength - ThicknessOfPillarConstruct * 2) / DisVertCarWidth);
            DisCarAndHalfLane = DisLaneWidth / 2 + DisVertCarLength;
            DisCarAndHalfLaneBackBack = DisLaneWidth / 2 + DisVertCarLengthBackBack;
            DisModulus = DisCarAndHalfLane * 2;
            DisBackBackModulus = DisVertCarLengthBackBack * 2 + DisLaneWidth;
            LengthCanGIntegralModulesConnectSingle = 4 * DisVertCarWidth + DisLaneWidth / 2 + DisPillarLength * 2;
            LengthCanGIntegralModulesConnectDouble = 6 * DisVertCarWidth + DisLaneWidth + DisPillarLength * 2;
            LengthCanGAdjLaneConnectSingle = DisLaneWidth / 2 + DisVertCarWidth * 4 + DisPillarLength * 2;
            LengthCanGAdjLaneConnectDouble = DisLaneWidth + DisVertCarWidth * 7 + DisPillarLength * 2;
            GeneratePillars = gpillars;
            Walls = walls;
            Obstacles = obstacles;
            foreach (var ob in Obstacles)
            {
                ObstacleVertexes.AddRange(ob.Coordinates);
            }
            Boundary = boundary;
            BoundingBox = (Polygon)boundary.Envelope;
            MaxLength = BoundingBox.Length / 2;
            InitialzeDatas(inilanes);
            //var boundcoords = JoinCurves(walls, inilanes)[0].Coordinates;
            //boundcoords = boundcoords.Append(boundcoords.First()).ToArray();
            //Boundary = new Polygon(new LinearRing(boundcoords));
            DisHalfCarToPillar = (PillarSpacing - CountPillarDist * DisVertCarWidth - DisPillarLength) / 2;
            if (!ScareEnabledForBackBackModule)
            {
                DisBackBackModulus = DisModulus;
                DisVertCarLengthBackBack = DisVertCarLength;
                DisCarAndHalfLaneBackBack = DisCarAndHalfLane;
            }
            else
            {
                DisVertCarLengthBackBack = DisVertCarLength-DifferenceFromBackBack;
                DisCarAndHalfLaneBackBack = DisLaneWidth / 2 + DisVertCarLengthBackBack;
                DisBackBackModulus = DisVertCarLengthBackBack * 2 + DisLaneWidth;
            }
            SingleVertModulePlacementFactor = 1.5;
            //AllowCompactedLane = false;
        }
        public List<LineString> Walls;
        public List<Polygon> Obstacles;
        public List<LineSegment> OriginalLanes = new List<LineSegment>();
        public Polygon Boundary;
        public Polygon OutBoundary;
        public List<LineSegment> OutputLanes;
        private Polygon BoundingBox;
        public static double MaxLength;
        public MNTSSpatialIndex ObstaclesSpatialIndex;
        public MNTSSpatialIndex CarBoxesSpatialIndex = new MNTSSpatialIndex(new List<Geometry>());
        public MNTSSpatialIndex LaneSpatialIndex = new MNTSSpatialIndex(new List<Geometry>());
        public MNTSSpatialIndex LaneBufferSpatialIndex = new MNTSSpatialIndex(new List<Geometry>());
        public MNTSSpatialIndex CarSpatialIndex = new MNTSSpatialIndex(new List<Geometry>());
        public List<Lane> IniLanes = new List<Lane>();
        public List<Lane> InitialLanes = new List<Lane>();
        public List<Polygon> CarSpots = new List<Polygon>();
        public List<Polygon> Pillars = new List<Polygon>();
        private List<Polygon> CarBoxes = new List<Polygon>();
        public List<InfoCar> Cars = new List<InfoCar>();
        private List<Polygon> IniLaneBoxes = new List<Polygon>();
        private List<CarBoxPlus> CarBoxesPlus = new List<CarBoxPlus>();
        private List<Polygon> LaneBoxes = new List<Polygon>();
        private List<CarModule> CarModules = new List<CarModule>();
        public List<Coordinate> ObstacleVertexes = new List<Coordinate>();
        public List<Polygon> BuildingBoxes = new List<Polygon>();
        public List<OblRamp> RampList = new List<OblRamp>();
        public List<Polygon> IniPillar = new List<Polygon>();
        public Polygon CaledBound { get; set; }
        public List<VehicleLane> VehicleLanes = new List<VehicleLane>();
        public List<ParkingPlaceBlock> ParkingPlaceBlocks = new List<ParkingPlaceBlock>();
        /// <summary>
        /// 在判断生成车道的优先级时记载上一条生成车道的方向，法向，用于提高相同车道方向生成的优先级
        /// </summary>
        private Vector2D ParentDir = Vector2D.Zero;

        public static bool AllowLaneDeformation=false;//允许车道微动
        public bool AccurateCalculate = true;
        public static double DifferenceFromBackBack = 200;
        public static double ScareFactorForCollisionCheck = 0.999999;
        public static bool ScareEnabledForBackBackModule = true;
        public static bool GeneratePillars = true;
        public static bool GenerateMiddlePillars = true;
        public static bool HasImpactOnDepthForPillarConstruct = true;
        public static bool GenerateLaneForLayoutingCarsInShearWall = true;
        public static double PillarNetLength = 500;
        public static double PillarNetDepth = 500;
        public static double ThicknessOfPillarConstruct = 50;
        public static double PillarSpacing = 7800;
        public static double DisVertCarLength = 5300;
        public static double DisVertCarLengthBackBack = 5100;
        public static double DisVertCarWidth = 2400;
        public static double DisParallelCarLength = 6000;
        public static double DisParallelCarWidth = 2400;
        public static double DisLaneWidth = 5500;
        public static double CollisionD = /*300;*/0;
        public static double CollisionTOP = /*100;*/0;
        public static double CollisionCT = 1400;
        public static double CollisionCM = 1500;
        public static double DisPillarLength = PillarNetLength + ThicknessOfPillarConstruct * 2;
        public static double DisPillarDepth = PillarNetDepth + ThicknessOfPillarConstruct * 2;
        public static int CountPillarDist = (int)Math.Floor((PillarSpacing - PillarNetLength - ThicknessOfPillarConstruct * 2) / DisVertCarWidth);
        public static double DisCarAndHalfLane = DisLaneWidth / 2 + DisVertCarLength;
        public static double DisCarAndHalfLaneBackBack = DisLaneWidth / 2 + DisVertCarLengthBackBack;
        public static double DisModulus = DisCarAndHalfLane * 2;
        public static double DisBackBackModulus = DisVertCarLengthBackBack * 2 + DisLaneWidth;
        public static double DisHalfCarToPillar = (PillarSpacing - CountPillarDist * DisVertCarWidth - DisPillarLength) / 2;
        public static double DisPillarMoveDeeplyBackBack = 1000;
        public static double DisPillarMoveDeeplySingle = 550;
        public static double LengthCanGIntegralModulesConnectSingle = 3.5 * DisVertCarWidth + DisLaneWidth / 2 + DisPillarLength;
        public static double LengthCanGIntegralModulesConnectDouble = 6 * DisVertCarWidth + DisLaneWidth + DisPillarLength * 2;
        public static double LengthCanGAdjLaneConnectSingle = DisLaneWidth / 2 + DisVertCarWidth * 3.1 + DisPillarLength;
        public static double LengthCanGAdjLaneConnectDouble = DisLaneWidth + DisVertCarWidth * 7 + DisPillarLength * 2;
        public static double STRTreeCount = 10;
        public List<LineSegment> OutEnsuredLanes = new List<LineSegment>();
        public List<LineSegment> OutUnsuredLanes = new List<LineSegment>();
        public static bool DisplayFinal = false;
        public static int LayoutMode = ((int)LayoutDirection.FOLLOWPREVIOUS);
        public static double LayoutScareFactor_Intergral = 0.7;
        public static double LayoutScareFactor_Adjacent = 0.7;
        public static double LayoutScareFactor_betweenBuilds = 0.7;
        public static double LayoutScareFactor_SingleVert = 0.7;
        public static double LayoutScareFactor_ParentDir = 3;

        public static bool AllowProcessEndLanes = true;//尽端停车
        public static bool LoopThroughEnd = true;//尽端环通
        public double DisAllowMaxLaneLength = 50000;//允许生成车道最大长度
        public double DisConsideringLoopThroughEnd = 50000;//尽端环通车道条件判断长度
        public bool AllowCompactedLane = false;
        public bool hasCompactedLane = false;
        private int CalCompactLaneCount = 0;
        /// <summary>
        /// 如果生成车道线与上一根方向一致，则乘以比例权重系数
        /// </summary>
        //孤立的单排垂直式模块生成条件控制_非单排模块车位预计数与孤立单排车位的比值.单排车位数大于para*非单排，排单排
        public static double SingleVertModulePlacementFactor = 1.5;
        public bool QuickCalculate = false;//快速计算

        public enum LayoutDirection : int
        {
            LENGTH = 0,
            HORIZONTAL = 1,
            VERTICAL = 2,
            /// <summary>
            /// 与生成的车道线方向一致
            /// </summary>
            FOLLOWPREVIOUS = 3
        }
        public int Process(bool accurate)
        {
            AccurateCalculate = accurate;
            GenerateParkingSpaces();
            CaledBound =MParkingPartitionPro.CalBoundary(Boundary, Pillars, IniLanes, BuildingBoxes, Cars);
            return CarSpots.Count;
        }
        public void GenerateParkingSpaces()
        {
            if (!hasCompactedLane)
                PreProcess();
            if (!QuickCalculate)
            {
                GenerateLanes();
            }
            else
            {
                GenerateLanesSuperFast();
            }
            GeneratePerpModules();
            GenerateCarsInModules();
            //ProcessLanes(ref IniLanes);
            GenerateCarsOnRestLanes();
            PostProcess();
            if (!hasCompactedLane && AllowCompactedLane && !QuickCalculate)
            {
                //CompactLane();
                CompactPro();
            }

        }

        private void InitialzeDatas(List<LineSegment> iniLanes)
        {
            //如果柱子完成面宽度对车道间距没有影响，则在一开始便将柱子缩小为净尺寸
            if (!HasImpactOnDepthForPillarConstruct)
            {
                DisPillarLength = PillarNetLength;
                DisPillarDepth = PillarNetDepth;
            }
            //输入的车道线有可能有碎线，将能合并join的join起来
            int count = 0;
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
                        if (IsParallelLine(iniLanes[i], iniLanes[j]) && (iniLanes[i].P0.Distance(iniLanes[j].P0) == 0
                            || iniLanes[i].P0.Distance(iniLanes[j].P1) == 0
                            || iniLanes[i].P1.Distance(iniLanes[j].P0) == 0
                            || iniLanes[i].P1.Distance(iniLanes[j].P1) == 0))
                        {
                            var pl = JoinCurves(new List<LineString>(), new List<LineSegment>() { iniLanes[i], iniLanes[j] }).First();
                            var line = new LineSegment(pl.StartPoint.Coordinate, pl.EndPoint.Coordinate);
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
                var vec = Vector(e).GetPerpendicularVector().Normalize();
                var pt = e.MidPoint.Translation(vec);
                if (!Boundary.Contains(new Point(pt)))
                    vec = -vec;
                IniLanes.Add(new Lane(e, vec));
                OriginalLanes.Add(e);
            }
            Obstacles.ForEach(e => ObstacleVertexes.AddRange(e.Coordinates));
            IniLaneBoxes.AddRange(IniLanes.Select(e => e.Line.Buffer(DisLaneWidth / 2)));
            //有的车道线不满足车道宽被剪掉了，但是墙线又没有，把它加到墙线里面去
            //var laneLines_buffer = IniLanes.Select(e => e.Line.Buffer(1)).ToList();
            //var splits = SplitCurve(new LineString(Boundary.Coordinates), laneLines_buffer.Select(e => new LineString(e.Coordinates)).ToList()).ToList();
            //splits = splits.Where(e =>
            //{
            //    foreach (var ln in IniLanes.Select(t => t.Line))
            //    {
            //        if (ln.ClosestPoint(e.GetMidPoint()).Distance(e.GetMidPoint()) < 1)
            //            return false;
            //    }
            //    return true;
            //}).ToList();
            //Walls = JoinCurves(splits, new List<LineSegment>());
        }
        private void ClearNecessaryElements()
        {
            ParentDir = Vector2D.Zero;
            IniLanes = new List<Lane>();
            OutputLanes = new List<LineSegment>();
            OutEnsuredLanes = new List<LineSegment>();
            OutUnsuredLanes = new List<LineSegment>();
            CarSpatialIndex = new MNTSSpatialIndex(new List<Geometry>());
            CarSpots = new List<Polygon>();
            Pillars = new List<Polygon>();
            Cars = new List<InfoCar>();
            IniPillar = new List<Polygon>();
            LaneBufferSpatialIndex = new MNTSSpatialIndex(new List<Geometry>());
            LaneBoxes = new List<Polygon>();
            LaneSpatialIndex = new MNTSSpatialIndex(new List<Geometry>());

            //CarBoxesSpatialIndex = new MNTSSpatialIndex(new List<Geometry>());
            //CarBoxes = new List<Polygon>();
            //CarBoxesPlus = new List<CarBoxPlus>();
            //CarModules = new List<CarModule>();
        }

        private void Update(List<Lane> newlanes, List<Lane> eldlanes)
        {
            IniLanes = new List<Lane>(newlanes);

            var add_carBoxes = new List<Polygon>();
            var add_carBoxes_plus = new List<CarBoxPlus>();
            var add_modules = new List<CarModule>();

            CarBoxes = add_carBoxes.Distinct().ToList();
            CarBoxesPlus = add_carBoxes_plus.Distinct().ToList();
            CarModules = add_modules.Distinct().ToList();
            CarBoxesSpatialIndex = new MNTSSpatialIndex(CarBoxes);
            //CarBoxesSpatialIndex.Update(IniLanes.Select(e => PolyFromLine(e.Line)).ToArray(), new List<Polygon>());
            CarBoxesSpatialIndex.Update(IniLanes.Where(e => Boundary.ClosestPoint(e.Line.MidPoint).Distance(e.Line.MidPoint) > 10).
                Select(e => PolyFromLines(e.Line, e.Line.Translation(e.Vec.Normalize() * DisCarAndHalfLaneBackBack))).ToArray(), new List<Polygon>());
            foreach (var ln in IniLanes)
            {
                ln.CanExtend = true;
                ln.CanBeMoved = true;
                //ln.IsGeneratedForLoopThrough = false;
                //ln.IsAdjLaneForProcessLoopThroughEnd = false;
                ln.GEndAdjLine = false;
                ln.GStartAdjLine = false;
            }
        }
        public MParkingPartitionPro ConvertToMParkingPartitionPro()
        {
            var mParkingPartitionPro = new MParkingPartitionPro();
            mParkingPartitionPro.Cars = Cars;
            mParkingPartitionPro.Pillars = Pillars;
            mParkingPartitionPro.OutputLanes = OutputLanes;
            return mParkingPartitionPro;
        }
    }
}
