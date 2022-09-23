﻿using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPArchitecture.ViewModel;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.OInterProcess;
using ThParkingStall.Core.OTools;
using ThParkingStall.Core.Tools;

namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{
    public class BuildingPosAnalysis
    {
        public List<List<Vector2D>> PotentialMovingVectors = new List<List<Vector2D>>();//i,j 代表第i个建筑的第j个移动方案
        private List<List<int>> ParkingCnts;//i,j代表第i个建筑的第j个车位个数
        private List<Vector2D> BestVectors = new List<Vector2D>();
        private List<OSubArea> InitSubAreas;//初始子区域

        private int BuildingMoveDistance;//建筑横纵偏移最大距离
        private int SampleDistance;//采样间距

        private double HalfLaneWidth = -0.1 + VMStock.RoadWidth / 2;

        //1.获取所有可能的移动方案
        //网格+特殊点 
        //筛选合理解
        //确保一个分区只有一个建筑，且移动不会跨区域
        //2.可移动方案分配到子进程
        //3.每个建筑筛选最多个数的移动方案
        //4.按最多个数进行排布，返回排布方案
        static BufferParameters MitreParam = new BufferParameters(8, EndCapStyle.Flat, JoinStyle.Mitre, 5.0);
        public BuildingPosAnalysis(ParkingStallArrangementViewModel parameterViewModel)
        {
            //BuildingMoveDistance = parameterViewModel.BuildingMoveDistance;
            //SampleDistance = parameterViewModel.SampleDistance;
            BuildingMoveDistance = 500;
            SampleDistance = 500;
            UpdateMovingVector();
            InitSubAreas = OInterParameter.GetOSubAreas(null);
        }
        private void _UpdateMovingVector()
        {
            var lanes = OInterParameter.GetBoundLanes();
            var halfLaneWidth = -0.1 + VMStock.RoadWidth / 2;
            lanes = lanes.Buffer(halfLaneWidth, MitreParam);
            var StepCnts = BuildingMoveDistance / SampleDistance;
            AffineTransformation transformation = new AffineTransformation();
            for (int k = 0;k<OInterParameter.MovingBounds.Count;k++)
            {
                var bound = OInterParameter.MovingBounds[k];
                PotentialMovingVectors.Add(new List<Vector2D>());
                if (bound.Intersects(lanes)) throw new Exception("Bound Intsects With Initial Bound Lines!");
                for(int i = -StepCnts; i < StepCnts; i++)
                {
                    for(int j = -StepCnts; j < StepCnts; j++)
                    {
                        var x = i * SampleDistance;
                        var y = j * SampleDistance;
                        var vector = new Vector2D(x, y);
                        transformation.SetToTranslation(x, y);
                        var newBound = transformation.Transform(bound);
                        if (newBound.Disjoint(lanes))
                        {
                            PotentialMovingVectors[k].Add(vector);
                        }
                    }
                }
            }
            ;
        }

        private void UpdateMovingVector()
        {
            var lanes = OInterParameter.InitSegLines.Select(l =>l.VaildLane.OExtend(0.1)).ToList();
            var centerLanes = new List<LineSegment>();
            for(int i = 0; i < lanes.Count; i++)
            {
                var currentLane = lanes[i];
                if(currentLane == null) continue;
                var IntSecPts = new List<Coordinate>();
                for(int j = 0; j < lanes.Count; j++)
                {
                    if (i == j) continue;
                    var nextLane = lanes[j];
                    if(nextLane == null) continue;
                    var intSecPt = currentLane.Intersection(nextLane);
                    if(intSecPt != null) IntSecPts.Add(intSecPt);
                }
                if(IntSecPts.Count > 1)
                {
                    var ordered = IntSecPts.PositiveOrder();
                    centerLanes.Add(new LineSegment(ordered.First(), ordered.Last()));
                }
            }
            var CenterLaneGeo = new MultiLineString(centerLanes.ToLineStrings().ToArray()).Buffer(HalfLaneWidth,MitreParam);
            CenterLaneGeo.Get<Polygon>(false).ForEach(p => p.ToDbMPolygon().AddToCurrentSpace());
            var StepCnts = BuildingMoveDistance / SampleDistance;
            AffineTransformation transformation = new AffineTransformation();
            for (int k = 0; k < OInterParameter.MovingBounds.Count; k++)
            {
                var bound = OInterParameter.MovingBounds[k];
                PotentialMovingVectors.Add(new List<Vector2D>());
                //if (bound.Intersects(CenterLaneGeo)) throw new Exception("建筑物与核心车道相交!");
                for (int i = -StepCnts; i < StepCnts+1; i++)
                {
                    for (int j = -StepCnts; j < StepCnts+1; j++)
                    {
                        var x = i * SampleDistance;
                        var y = j * SampleDistance;
                        var vector = new Vector2D(x, y);
                        transformation.SetToTranslation(x, y);
                        var newBound = transformation.Transform(bound);
                        if (newBound.Disjoint(CenterLaneGeo))
                        {
                            PotentialMovingVectors[k].Add(vector);
                        }
                    }
                }
            }
        }


        //单进程更新车位个数
        public void UpdateParkingCntSP()
        {
            AffineTransformation transformation = new AffineTransformation();
            var halfLaneWidth =-0.1+ VMStock.RoadWidth / 2;
            for (int k = 0; k < OInterParameter.MovingBounds.Count; k++)
            {
                var bound = OInterParameter.MovingBounds[k];
                var selected = OInterParameter.dynamicSubAreas.Where(s => s.Region.Intersects(bound)).ToList();
                if (selected.Count != 1) throw new Exception("Building cross two Areas");
                var subArea = selected[0];
                var buildings = subArea.Buildings;
                var ramps = subArea.Ramps;
                var buildingBounds = subArea.BuildingBounds;
                var lanes = subArea.VaildLanes;
                var walls = subArea.Walls;

                var objs = new GeometryCollection(walls.Cast<Geometry>().ToList().Concat(buildings.Cast<Geometry>()).ToArray());
                var ConnectionList = new List<(bool, bool)>();
                foreach(var lane in lanes)
                {
                    var posLane = lane.Positivize();
                    bool negConnect = posLane.P0.ToPoint().Distance(objs) < (halfLaneWidth-0.1);
                    bool posConnect = posLane.P1.ToPoint().Distance(objs) < (halfLaneWidth-0.1);
                    ConnectionList.Add((negConnect, posConnect));
                }

                var buildingsToMove = new List<Polygon>();
                var buildingsNotMove = new List<Polygon>();
                
                var rampsToMove = new List<ORamp>();
                var rampsNotMove = new List<ORamp>();
                
                var BBsToMove = new List<Polygon>();
                var BBsNotMove = new List<Polygon>();
                foreach (var building in buildings)
                {
                    if (bound.Intersects(building))buildingsToMove.Add(building);
                    else buildingsNotMove.Add(building);
                }
                foreach (var ramp in ramps)
                {
                    if (bound.Intersects(ramp.Area)) rampsToMove.Add(ramp);
                    else rampsNotMove.Add(ramp);
                }
                foreach (var BB in buildingBounds)
                {
                    if (bound.Intersects(BB)) BBsToMove.Add(BB);
                    else BBsNotMove.Add(BB);
                }
                int maxCnt = 0;
                Vector2D BestVector = new Vector2D();
                foreach (var vec in PotentialMovingVectors[k])
                {
                    transformation.SetToTranslation(vec.X, vec.Y);
                    var new_builds = buildingsToMove.Select(b => transformation.Transform(b) as Polygon).ToList();
                    var new_ramps = rampsToMove.Select(r => r.Transform(vec)).ToList();
                    var new_BBs = BBsToMove.Select(b => transformation.Transform(b) as Polygon).ToList();
                    new_builds.AddRange(buildingsNotMove);
                    new_ramps.AddRange(rampsNotMove);
                    new_BBs.AddRange(BBsNotMove);

                    var boundarySPIdx = new MNTSSpatialIndex(new_builds.Cast<Geometry>().ToList().Concat(walls));
                    var newLanes = new List<LineSegment>();

                    for(int i=0;i< lanes.Count; i++)
                    {
                        var lane = lanes[i];
                        var connection = ConnectionList[i];
                        newLanes.Add(lane.GetVaildLane(connection, boundarySPIdx));
                    }

                    var new_SubArea = new OSubArea(subArea.Region, newLanes,subArea.Walls,new_builds,new_ramps,new_BBs);
                    new_SubArea.UpdateParkingCnts(true);
                    if (new_SubArea.Count > maxCnt)
                    {
                        maxCnt = new_SubArea.Count;
                        BestVector = vec;
                    }
                }
                BestVectors.Add(BestVector);
            }
        }

        public void UpdateSolution()
        {
            AffineTransformation transformation = new AffineTransformation();
            var halfLaneWidth = VMStock.RoadWidth / 2;
            for (int k = 0; k < OInterParameter.MovingBounds.Count; k++)
            {
                var bound = OInterParameter.MovingBounds[k];
                var bestVector = BestVectors[k];
                OSubArea subArea = OInterParameter.dynamicSubAreas.First();
                int idx = 0;
                foreach(var osub in OInterParameter.dynamicSubAreas)
                {
                    if (osub.Region.Intersects(bound))
                    {
                        subArea = osub; break;
                    }
                    idx++;
                }
                //var selected = OInterParameter.dynamicSubAreas.Where(s => s.Region.Intersects(bound)).ToList();
                //if (selected.Count != 1) throw new Exception("Building cross two Areas");
                //var subArea = selected[0];
                var buildings = subArea.Buildings;
                var ramps = subArea.Ramps;
                var buildingBounds = subArea.BuildingBounds;
                var lanes = subArea.VaildLanes;
                var walls = subArea.Walls;

                var objs = new GeometryCollection(walls.Cast<Geometry>().ToList().Concat(buildings.Cast<Geometry>()).ToArray());
                var ConnectionList = new List<(bool, bool)>();
                foreach (var lane in lanes)
                {
                    var posLane = lane.Positivize();
                    bool negConnect = posLane.P0.ToPoint().Distance(objs) < (halfLaneWidth - 0.1);
                    bool posConnect = posLane.P1.ToPoint().Distance(objs) < (halfLaneWidth - 0.1);
                    ConnectionList.Add((negConnect, posConnect));
                }
                var buildingsToMove = new List<Polygon>();
                var buildingsNotMove = new List<Polygon>();

                var rampsToMove = new List<ORamp>();
                var rampsNotMove = new List<ORamp>();

                var BBsToMove = new List<Polygon>();
                var BBsNotMove = new List<Polygon>();
                foreach (var building in buildings)
                {
                    if (bound.Intersects(building)) buildingsToMove.Add(building);
                    else buildingsNotMove.Add(building);
                }
                foreach (var ramp in ramps)
                {
                    if (bound.Intersects(ramp.Area)) rampsToMove.Add(ramp);
                    else rampsNotMove.Add(ramp);
                }
                foreach (var BB in buildingBounds)
                {
                    if (bound.Intersects(BB)) BBsToMove.Add(BB);
                    else BBsNotMove.Add(BB);
                }
                transformation.SetToTranslation(bestVector.X, bestVector.Y);
                var new_builds = buildingsToMove.Select(b => transformation.Transform(b) as Polygon).ToList();
                var new_ramps = rampsToMove.Select(r => r.Transform(bestVector)).ToList();
                var new_BBs = BBsToMove.Select(b => transformation.Transform(b) as Polygon).ToList();
                new_builds.AddRange(buildingsNotMove);
                new_ramps.AddRange(rampsNotMove);
                new_BBs.AddRange(BBsNotMove);

                var boundarySPIdx = new MNTSSpatialIndex(new_builds.Cast<Geometry>().ToList().Concat(walls));
                var newLanes = new List<LineSegment>();

                for (int i = 0; i < lanes.Count; i++)
                {
                    var lane = lanes[i];
                    var connection = ConnectionList[i];
                    newLanes.Add(lane.GetVaildLane(connection, boundarySPIdx));
                }
                OInterParameter.dynamicSubAreas[idx] = new OSubArea(subArea.Region, newLanes, subArea.Walls, new_builds, new_ramps, new_BBs);
            }
        }

        public void UpdateBest()
        {
            var BPC = new BuildingPosCalculate();
            InitSubAreas = BPC.InitSubAreas;
            var scoresList = BPC.CalculateScore(PotentialMovingVectors);
            var bestVectors = new List<Vector2D>();
            for (int i = 0; i < PotentialMovingVectors.Count; i++)
            {
                bestVectors.Add(new Vector2D());
                var scores = scoresList[i];
 
                if (scores.Count == 0) continue;//不移动，跳过

                var bestScore = scores.Max();
                var idx = scores.IndexOf(bestScore);
                var initScore = BPC.DynamicSubAreas[i].Sum(s =>s.InitParkingCnt);
                if (initScore >= bestScore) continue;//初始分数较好

                var bestVector = PotentialMovingVectors[i][idx];

                bestVectors[bestVectors .Count- 1] = bestVector;

                ////这块不对（多个bound在一个分区的case）
                //var movedSubAreas = BPC.DynamicSubAreas[idx].Select(s =>s.GetMovedArea(bestVector));

                //foreach(var DSubArea in BPC.DynamicSubAreas[idx])
                //{
                //    BPC.InitSubAreas[DSubArea.InitIndex] = DSubArea.GetMovedArea(bestVector);
                //}
            }
            OInterParameter.UpdateBuildings(bestVectors);
            //var subAreas = OInterParameter.GetOSubAreas(null);
        }
    }
}
