using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
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
        }
        private List<OSubArea> OSubAreas { get; set; }
        public Polygon CalBound()
        {
            var cars = new List<InfoCar>();
            var pillars = new List<Polygon>();
            var lanes = new List<LineSegment>();
            //var obstacles = new List<Polygon>();
            var obstacles = OInterParameter.BuildingBounds;
            foreach (var subArea in OSubAreas)
            {
                cars.AddRange(subArea.obliqueMPartition.Cars);
                pillars.AddRange(subArea.obliqueMPartition.Pillars);
                lanes.AddRange(subArea.obliqueMPartition.IniLanes.Select(e => e.Line));
                //obstacles.AddRange(subArea.obliqueMPartition.Obstacles);
            }
            var newbound = MParkingPartitionPro.CalIntegralBound(pillars, lanes, obstacles, cars);
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
        public ObliqueMPartition ProcessEndLanes()
        {
            ParaWrite();
            var walls = new List<LineString>();
            var cars = new List<InfoCar>();
            var pillars = new List<Polygon>();
            var iniPillars = new List<Polygon>();
            var obsVertices = new List<Coordinate>();
            var lanes = new List<LineSegment>();
            var obstacles = new List<Polygon>();
            var Boundary = OSubAreas[0].OutBound.Clone();
            foreach (var subArea in OSubAreas)
            {
                walls.AddRange(subArea.obliqueMPartition.Walls);
                cars.AddRange(subArea.obliqueMPartition.Cars);
                pillars.AddRange(subArea.obliqueMPartition.Pillars);
                iniPillars.AddRange(subArea.obliqueMPartition.IniPillar);
                obsVertices.AddRange(subArea.obliqueMPartition.ObstacleVertexes);
                lanes.AddRange(subArea.obliqueMPartition.IniLanes.Select(e => e.Line));
                obstacles.AddRange(subArea.obliqueMPartition.Obstacles);
            }
            var ObstaclesSpacialIndex = new MNTSSpatialIndex(obstacles);
            RemoveDuplicatedLines(lanes);
            MLayoutPostProcessing.GenerateCarsOntheEndofLanesByRemoveUnnecessaryLanes(ref cars, ref pillars, ref lanes, walls, ObstaclesSpacialIndex, Boundary);
            MLayoutPostProcessing.GenerateCarsOntheEndofLanesByFillTheEndDistrict(ref cars, ref pillars, ref lanes, walls, ObstaclesSpacialIndex, Boundary);
            MLayoutPostProcessing.CheckLayoutDirectionInfoBeforePostProcessEndLanes(ref cars);
            MLayoutPostProcessing.RemoveInvalidPillars(ref pillars, cars);
            ObliqueMPartition obliqueMPartition=new ObliqueMPartition();
            obliqueMPartition.Cars = cars;
            obliqueMPartition.OutputLanes = lanes;
            obliqueMPartition.Pillars = pillars;
            return obliqueMPartition;
        }
    }
}
