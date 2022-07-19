using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
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
            DisVertCarLength = 5300;
            DisVertCarWidth = VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotWidth : VMStock.VerticalSpotLength;
            DisLaneWidth = VMStock.RoadWidth;
            PillarSpacing = VMStock.ColumnWidth;
            GenerateMiddlePillars = VMStock.MidColumnInDoubleRowModular;
            DisPillarMoveDeeplyBackBack = VMStock.ColumnShiftDistanceOfDoubleRowModular;
            DisPillarMoveDeeplySingle = VMStock.ColumnShiftDistanceOfSingleRowModular;
            PillarNetLength = VMStock.ColumnSizeOfParalleToRoad;
            PillarNetDepth = VMStock.ColumnSizeOfPerpendicularToRoad;
            ThicknessOfPillarConstruct = VMStock.ColumnAdditionalSize;
            //VMStock缺少RunMode参数
            //LayoutMode = ((int)VMStock.RunMode);
            HasImpactOnDepthForPillarConstruct = VMStock.ColumnAdditionalInfluenceLaneWidth;
            //viewmodel参数赋值完毕
            GeneratePillars = PillarSpacing < DisVertCarWidth ? false : GeneratePillars;
            DisPillarLength = PillarNetLength + ThicknessOfPillarConstruct * 2;
            DisPillarDepth = PillarNetDepth + ThicknessOfPillarConstruct * 2;
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
                DisVertCarLengthBackBack = 5100;
                DisCarAndHalfLaneBackBack = DisLaneWidth / 2 + DisVertCarLengthBackBack;
                DisBackBackModulus = DisVertCarLengthBackBack * 2 + DisLaneWidth;
            }
        }
        public List<LineString> Walls;
        public List<Polygon> Obstacles;
        public List<LineSegment> OriginalLanes = new List<LineSegment>();
        public Polygon Boundary;
        public Polygon OutBoundary;
        public List<LineSegment> OutputLanes;
        private Polygon BoundingBox;
        private double MaxLength;
        public MNTSSpatialIndex ObstaclesSpatialIndex;
        public MNTSSpatialIndex CarBoxesSpatialIndex = new MNTSSpatialIndex(new List<Geometry>());
        public MNTSSpatialIndex LaneSpatialIndex = new MNTSSpatialIndex(new List<Geometry>());
        public MNTSSpatialIndex LaneBufferSpatialIndex = new MNTSSpatialIndex(new List<Geometry>());
        public MNTSSpatialIndex CarSpatialIndex = new MNTSSpatialIndex(new List<Geometry>());
        public List<Lane> IniLanes = new List<Lane>();
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
        public List<Ramp> RampList = new List<Ramp>();
        public List<Polygon> IniPillar = new List<Polygon>();

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
        public static double LengthCanGAdjLaneConnectSingle = DisLaneWidth / 2 + DisVertCarWidth * 3.5 + DisPillarLength;
        public static double LengthCanGAdjLaneConnectDouble = DisLaneWidth + DisVertCarWidth * 7 + DisPillarLength * 2;
        public static double STRTreeCount = 10;
        public List<LineSegment> OutEnsuredLanes = new List<LineSegment>();
        public List<LineSegment> OutUnsuredLanes = new List<LineSegment>();
        public static bool DisplayFinal = false;
        public static int LayoutMode = ((int)LayoutDirection.LENGTH);
        public static double LayoutScareFactor_Intergral = 0.7;
        public static double LayoutScareFactor_Adjacent = 0.7;
        public static double LayoutScareFactor_betweenBuilds = 0.7;
        public static double LayoutScareFactor_SingleVert = 0.7;
        //孤立的单排垂直式模块生成条件控制_非单排模块车位预计数与孤立单排车位的比值.单排车位数大于para*非单排，排单排
        public static double SingleVertModulePlacementFactor = 1.0;
        public enum LayoutDirection : int
        {
            LENGTH = 0,
            HORIZONTAL = 1,
            VERTICAL = 2
        }
        public int Process()
        {
            GenerateParkingSpaces();
            return CarSpots.Count;
        }
        public void GenerateParkingSpaces()
        {
            PreProcess();
            GenerateLanes();
            GeneratePerpModules();
            GenerateCarsInModules();
            //ProcessLanes(ref IniLanes);
            GenerateCarsOnRestLanes();
            PostProcess();
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
