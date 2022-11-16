using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.LaneDeformation;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.OInterProcess;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.ObliqueMPartitionLayout.OPostProcess
{
    public class GlobalBusiness
    {
        class UBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                Type typeToDeserialize = null;
                typeToDeserialize = Type.GetType(String.Format("{0}, {1}",
                    typeName, assemblyName));
                return typeToDeserialize;
            }
        }
        public GlobalBusiness(List<OSubArea> oSubAreas)
        {
            OSubAreas = oSubAreas;
            Init();
        }
        void Init()
        {
            BOUND = OSubAreas[0].OutBound.Clone();
            foreach (var subArea in OSubAreas)
            {
                walls.AddRange(subArea.obliqueMPartition.Walls);
                cars.AddRange(subArea.obliqueMPartition.Cars);
                pillars.AddRange(subArea.obliqueMPartition.Pillars);
                obsVertices.AddRange(subArea.obliqueMPartition.ObstacleVertexes);
                lanes.AddRange(subArea.obliqueMPartition.IniLanes.Select(e => e.Line));
                obstacles.AddRange(subArea.obliqueMPartition.Obstacles);
                VehicleLanes.AddRange(subArea.obliqueMPartition.VehicleLanes);
            }
        }
        private List<OSubArea> OSubAreas { get; set; }
        public List<InfoCar> cars = new List<InfoCar>();
        public List<Polygon> pillars = new List<Polygon>();
        public List<LineSegment> lanes = new List<LineSegment>();
        public List<Polygon> obstacles = new List<Polygon>();
        public List<LineString> walls = new List<LineString>();
        public List<Coordinate> obsVertices = new List<Coordinate>();
        public List<VehicleLane> VehicleLanes = new List<VehicleLane>();
        public static Polygon BOUND { get; set; }
        
        public DrawTmpOutPut drawTmpOutPut0 = new DrawTmpOutPut();

        /// <summary>
        /// 单根车道线生成车位
        /// </summary>
        /// <param name="_line"></param>车道线
        /// <param name="_vec"></param>车道线生成车位的方向
        /// <param name="_obstacles"></param>车道线生成方向的障碍物
        /// <param name="_cars"></param>生成的车位
        /// <param name="_columns"></param>生成的柱子
        public static void GenerateCars(LineSegment _line, Vector2D _vec, List<Polygon> _obstacles, ref List<Polygon> _cars, ref List<Polygon> _columns)
        {
            #region 构造MParkingPartitionPro调用车位生成方法
            ObliqueMPartition tmpro = new ObliqueMPartition();
            tmpro.Walls = new List<LineString>();
            tmpro.Boundary = BOUND;
            var tmpro_lane = new LineSegment(_line);
            tmpro.IniLanes.Add(new Lane(tmpro_lane, _vec.Normalize()));
            tmpro.Obstacles = _obstacles;
            tmpro.ObstaclesSpatialIndex = new MNTSSpatialIndex(_obstacles);
            var vertlanes = tmpro.GeneratePerpModuleLanes(ObliqueMPartition.DisVertCarLength + ObliqueMPartition.DisLaneWidth / 2, ObliqueMPartition.DisVertCarWidth, false, null, true);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                var line = new LineSegment(vl);
                line = line.Translation(k.Vec.Normalize() * ObliqueMPartition.DisLaneWidth / 2);
                var line_align_backback_rest = new LineSegment();
                tmpro.GenerateCarsAndPillarsForEachLane(line, k.Vec.Normalize(), ObliqueMPartition.DisVertCarWidth, ObliqueMPartition.DisVertCarLength
                    , ref line_align_backback_rest, true, false, false, false, true, true, false, false, false, true, false, false, false, true);
            }
            tmpro.UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
            vertlanes = tmpro.GeneratePerpModuleLanes(ObliqueMPartition.DisParallelCarWidth + ObliqueMPartition.DisLaneWidth / 2, ObliqueMPartition.DisParallelCarLength, false);
            ObliqueMPartition.SortLaneByDirection(vertlanes, ObliqueMPartition.LayoutMode, Vector2D.Zero);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                //UnifyLaneDirection(ref vl, IniLanes);
                var line = new LineSegment(vl);
                line = line.Translation(k.Vec.Normalize() * ObliqueMPartition.DisLaneWidth / 2);
                var line_align_backback_rest = new LineSegment();
                tmpro.GenerateCarsAndPillarsForEachLane(line, k.Vec, ObliqueMPartition.DisParallelCarLength, ObliqueMPartition.DisParallelCarWidth
                    , ref line_align_backback_rest, true, false, false, false, true, true, false);
            }
            #endregion
        }

        public void DeformLanes()
        {
            //return;
            InitLaneDeformationParas();
            var vehiclesdata = new VehicleLaneData(VehicleLanes);

            //序列化
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var local_path = dir + "\\vehiclesdata.dat";
            FileStream fileStream = new FileStream(local_path, FileMode.Create);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fileStream, vehiclesdata); //序列化 参数：流 对象
            fileStream.Close();

            //反序列化
            fileStream = new FileStream(local_path, FileMode.Open);
            var formatter = new BinaryFormatter
            {
                Binder = new UBinder()
            };
            vehiclesdata = (VehicleLaneData)formatter.Deserialize(fileStream);
            fileStream.Close();

            LaneDeformationService deformationService = new LaneDeformationService(vehiclesdata);
            deformationService.Process();
            drawTmpOutPut0 = deformationService.DrawTmpOutPut0;
            vehiclesdata = deformationService.Result;

            readFromVehicles(vehiclesdata.VehicleLanes);
        }
        void readFromVehicles(List<VehicleLane> vehicles)
        {
            cars = new List<InfoCar>();
            pillars = new List<Polygon>();
            lanes = new List<LineSegment>();
            foreach (var vehicle in vehicles)
            {
                lanes.Add(vehicle.CenterLine);
                foreach (var block in vehicle.ParkingPlaceBlockList)
                {
                    cars.AddRange(block.Cars.Select(e => new InfoCar(e.ParkingPlaceObb, e.Point, new Vector2D(e.ParkingPlaceDir.X, e.ParkingPlaceDir.Y)) { CarLayoutMode = e.Type }));
                    pillars.AddRange(block.ColunmList.Select(e => e.ColunmnObb));
                }
            }
        }
        void InitLaneDeformationParas()
        {
            VehicleLane.VehicleLaneWidth = ObliqueMPartition.DisLaneWidth / 2;
            VehicleLane.Boundary = BOUND;
            VehicleLane.Blocks = obstacles;
        }
        public Polygon CalBound()
        {
            var newbound = MParkingPartitionPro.CalIntegralBound(pillars, lanes, OInterParameter.BuildingBounds, cars);
            return newbound;
        }

        public void ParaWrite()
        {
            MParkingPartitionPro.DisModulus = ObliqueMPartition.DisModulus;
            MParkingPartitionPro.CollisionCM = ObliqueMPartition.CollisionCM;
            MParkingPartitionPro.CollisionCT = ObliqueMPartition.CollisionCT;
            MParkingPartitionPro.CollisionD = ObliqueMPartition.CollisionD;
            MParkingPartitionPro.CollisionTOP = ObliqueMPartition.CollisionTOP;
            MParkingPartitionPro.CountPillarDist = ObliqueMPartition.CountPillarDist;
            MParkingPartitionPro.DisBackBackModulus = ObliqueMPartition.DisBackBackModulus;
            MParkingPartitionPro.DisCarAndHalfLane = ObliqueMPartition.DisCarAndHalfLane;
            MParkingPartitionPro.DisCarAndHalfLaneBackBack = ObliqueMPartition.DisCarAndHalfLaneBackBack;
            MParkingPartitionPro.DisHalfCarToPillar = ObliqueMPartition.DisHalfCarToPillar;
            MParkingPartitionPro.DisLaneWidth = ObliqueMPartition.DisLaneWidth;
            MParkingPartitionPro.DisParallelCarLength = ObliqueMPartition.DisParallelCarLength;
            MParkingPartitionPro.DisParallelCarWidth = ObliqueMPartition.DisParallelCarWidth;
            MParkingPartitionPro.DisPillarDepth = ObliqueMPartition.DisPillarDepth;
            MParkingPartitionPro.DisPillarLength = ObliqueMPartition.DisPillarLength;
            MParkingPartitionPro.DisPillarMoveDeeplyBackBack = ObliqueMPartition.DisPillarMoveDeeplyBackBack;
            MParkingPartitionPro.DisPillarMoveDeeplySingle = ObliqueMPartition.DisPillarMoveDeeplySingle;
            MParkingPartitionPro.DisplayFinal = ObliqueMPartition.DisplayFinal;
            MParkingPartitionPro.DisVertCarLength = ObliqueMPartition.DisVertCarLength;
            MParkingPartitionPro.DisVertCarLengthBackBack = ObliqueMPartition.DisVertCarLengthBackBack;
            MParkingPartitionPro.DisVertCarWidth = ObliqueMPartition.DisVertCarWidth;
            MParkingPartitionPro.GenerateLaneForLayoutingCarsInShearWall = ObliqueMPartition.GenerateLaneForLayoutingCarsInShearWall;
            MParkingPartitionPro.GenerateMiddlePillars = ObliqueMPartition.GenerateMiddlePillars;
            MParkingPartitionPro.GeneratePillars = ObliqueMPartition.GeneratePillars;
            MParkingPartitionPro.HasImpactOnDepthForPillarConstruct = ObliqueMPartition.HasImpactOnDepthForPillarConstruct;
            MParkingPartitionPro.LayoutScareFactor_Adjacent = ObliqueMPartition.LayoutScareFactor_Adjacent;
            MParkingPartitionPro.LayoutScareFactor_betweenBuilds = ObliqueMPartition.LayoutScareFactor_betweenBuilds;
            MParkingPartitionPro.LayoutScareFactor_Intergral = ObliqueMPartition.LayoutScareFactor_Intergral;
            MParkingPartitionPro.LayoutScareFactor_SingleVert = ObliqueMPartition.LayoutScareFactor_SingleVert;
            MParkingPartitionPro.LengthCanGAdjLaneConnectDouble = ObliqueMPartition.LengthCanGAdjLaneConnectDouble;
            MParkingPartitionPro.LengthCanGAdjLaneConnectSingle = ObliqueMPartition.LengthCanGAdjLaneConnectSingle;
            MParkingPartitionPro.LengthCanGIntegralModulesConnectDouble = ObliqueMPartition.LengthCanGIntegralModulesConnectDouble;
            MParkingPartitionPro.LengthCanGIntegralModulesConnectSingle = ObliqueMPartition.LengthCanGIntegralModulesConnectSingle;
            MParkingPartitionPro.LoopThroughEnd = ObliqueMPartition.LoopThroughEnd;
            MParkingPartitionPro.PillarNetDepth = ObliqueMPartition.PillarNetDepth;
            MParkingPartitionPro.PillarNetLength = ObliqueMPartition.PillarNetLength;
            MParkingPartitionPro.PillarSpacing = ObliqueMPartition.PillarSpacing;
            MParkingPartitionPro.ScareEnabledForBackBackModule = ObliqueMPartition.ScareEnabledForBackBackModule;
            MParkingPartitionPro.ScareFactorForCollisionCheck = ObliqueMPartition.ScareFactorForCollisionCheck;
            MParkingPartitionPro.SingleVertModulePlacementFactor = ObliqueMPartition.SingleVertModulePlacementFactor;
            MParkingPartitionPro.STRTreeCount = ObliqueMPartition.STRTreeCount;
            MParkingPartitionPro.ThicknessOfPillarConstruct = ObliqueMPartition.ThicknessOfPillarConstruct;
        }
        /// <summary>
        /// 尽端停车的处理
        /// </summary>
        public void ProcessEndLanes()
        {
            ParaWrite();
            var _walls = walls;
            var _cars = cars;
            var _pillars = pillars;
            var _obsVertices = obsVertices;
            var _lanes = lanes;
            var _obstacles = obstacles;
            var ObstaclesSpacialIndex = new MNTSSpatialIndex(_obstacles);
            RemoveDuplicatedLines(_lanes);
            MLayoutPostProcessing.GenerateCarsOntheEndofLanesByRemoveUnnecessaryLanes(ref _cars, ref _pillars, ref _lanes, _walls, ObstaclesSpacialIndex, BOUND);
            MLayoutPostProcessing.GenerateCarsOntheEndofLanesByFillTheEndDistrict(ref _cars, ref _pillars, ref _lanes, _walls, ObstaclesSpacialIndex, BOUND);
            MLayoutPostProcessing.CheckLayoutDirectionInfoBeforePostProcessEndLanes(ref _cars);
            MLayoutPostProcessing.RemoveInvalidPillars(ref _pillars, _cars);
            walls = _walls;
            cars = _cars;
            pillars = _pillars;
            obsVertices = _obsVertices;
            lanes = _lanes;
            obstacles = _obstacles;
            return;
        }
    }


    public class DrawTmpOutPut 
    {

        //Wu
        public List<Polygon> FreeAreaRecs2 = new List<Polygon>();
        public List<Polygon> LaneNodes = new List<Polygon>();
        public List<Polygon> SpotNodes = new List<Polygon>();

        public List<LineSegment> NeighborRelations = new List<LineSegment>();
        public List<double> ToleranceResults = new List<double>();
        public List<Point> TolerancePositions = new List<Point>();

        public List<Polygon> RearrangeRegions = new List<Polygon>();


        //luoyun7
        public List<Polygon> OriginalFreeAreaList =new List<Polygon>();
        public List<Polygon> FreeAreaRecs = new List<Polygon>();

        public List<Polygon> BlockShow = new List<Polygon>();
        public List<Polygon> TmpCutRecs = new List<Polygon>();
        public List<Polygon> UpCut = new List<Polygon>();
        public List<Polygon> BoundaryRecs = new List<Polygon>();
        //增加要打印的东西
        public DrawTmpOutPut() 
        {



        }
    }
}
