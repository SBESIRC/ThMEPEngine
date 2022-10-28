using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public Polygon BOUND { get; set; }
        public void DeformLanes()
        {
            return;
            InitLaneDeformationParas();
            var vehicles = VehicleLanes;
            func(vehicles);
            readFromVehicles(vehicles);
        }
        void func(List<VehicleLane> vehicles)
        {

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
                    cars.AddRange(block.Cars.Select(e => new InfoCar(e.ParkingPlaceObb, e.Point, e.ParkingPlaceDir) { CarLayoutMode = e.Type }));
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
}
