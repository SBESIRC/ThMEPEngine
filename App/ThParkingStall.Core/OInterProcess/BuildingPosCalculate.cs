using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.OTools;
using ThParkingStall.Core.Tools;

namespace ThParkingStall.Core.OInterProcess
{
    public class _BuildingPosCalculate
    {
        private List<List<Vector2D>> PotentialMovingVectors = new List<List<Vector2D>>();//i,j 代表第i个建筑的第j个移动方案
        private List<List<int>> ParkingCnts;//i,j代表第i个建筑的第j个车位个数

        private List<Polygon> buildingsToMove = new List<Polygon>();
        private List<Polygon> buildingsNotMove = new List<Polygon>();

        private List<ORamp> rampsToMove = new List<ORamp>();
        private List<ORamp> rampsNotMove = new List<ORamp>();

        private List<Polygon> BBsToMove = new List<Polygon>();
        private List<Polygon> BBsNotMove = new List<Polygon>();

        private AffineTransformation Transformation = new AffineTransformation();
        private List<(bool, bool)> ConnectionList = new List<(bool, bool)>();
        private OSubArea initSubArea;

        private double halfLaneWidth =-0.1+ VMStock.RoadWidth / 2;

        //1.获取所有可能的移动方案
        //网格+特殊点 
        //筛选合理解
        //确保一个分区只有一个建筑，且移动不会跨区域
        //2.可移动方案分配到子进程
        //3.每个建筑筛选最多个数的移动方案
        //4.按最多个数进行排布，返回排布方案
        static BufferParameters MitreParam = new BufferParameters(8, EndCapStyle.Flat, JoinStyle.Mitre, 5.0);
        public _BuildingPosCalculate(List<List<Vector2D>> potentialMovingVectors)
        {
            PotentialMovingVectors = potentialMovingVectors;
        }

        public _BuildingPosCalculate(List<Vector2D> movingVectors)
        {
            movingVectors.ForEach(vec => PotentialMovingVectors.Add(new List<Vector2D> { vec }));
        }

        //计算输入的向量位置的
        public (List<Vector2D>,List<int>) CalculateBest(bool Update = false)
        {
            var BestVectors = new List<Vector2D>();
            var BestParkingCnts = new List<int>();
            for (int k = 0; k < OInterParameter.MovingBounds.Count; k++)
            {
                var bound = OInterParameter.MovingBounds[k];
                UpdateRecord(bound);
                int maxCnt = initSubArea.Count;
                Vector2D BestVector = new Vector2D();
                var BestSubArea = initSubArea;
                foreach (var vec in PotentialMovingVectors[k])
                {
                    if (vec.Length() == 0) continue;
                    var new_SubArea = MoveBuilding(vec);
                    new_SubArea.UpdateParkingCnts(true);
                    if (new_SubArea.Count > maxCnt)
                    {
                        maxCnt = new_SubArea.Count;
                        BestVector = vec;
                        BestSubArea = new_SubArea;
                    }
                }
                BestVectors.Add(BestVector);
                BestParkingCnts.Add(maxCnt);
                if (Update)
                {
                    for(int i = 0; i < OInterParameter.dynamicSubAreas.Count; i++)
                    {
                        var tempArea = OInterParameter.dynamicSubAreas[i];
                        if(tempArea.Region.Centroid.Distance(BestSubArea.Region.Centroid) < 1)
                        {
                            OInterParameter.dynamicSubAreas[i] = BestSubArea;
                            break;
                        }
                    }
                }
                ClearRecord();
            }

            return (BestVectors, BestParkingCnts);
        }
        //根据输入的框线，选择OsubArea(有且只有一个）,更新连接表，更新可动以及不可动建筑
        private void UpdateRecord(Polygon bound)
        {
            var selected = OInterParameter.dynamicSubAreas.Where(s => s.Region.Intersects(bound)).ToList();
            if (selected.Count != 1) throw new Exception("Building cross two Areas");
            initSubArea = selected[0];
            var buildings = initSubArea.Buildings;
            var ramps = initSubArea.Ramps;
            var buildingBounds = initSubArea.BuildingBounds;
            var lanes = initSubArea.VaildLanes;
            var walls = initSubArea.Walls;
            var objs = new GeometryCollection(walls.Cast<Geometry>().ToList().Concat(buildings.Cast<Geometry>()).ToArray());
            foreach (var lane in lanes)
            {
                var posLane = lane.Positivize();
                bool negConnect = posLane.P0.ToPoint().Distance(objs) < (halfLaneWidth - 0.1);
                bool posConnect = posLane.P1.ToPoint().Distance(objs) < (halfLaneWidth - 0.1);
                ConnectionList.Add((negConnect, posConnect));
            }
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
        }

        //按记录移动可动建筑，返回移动后的SubArea
        private OSubArea MoveBuilding(Vector2D vec)
        {
            var buildings = initSubArea.Buildings;
            var ramps = initSubArea.Ramps;
            var buildingBounds = initSubArea.BuildingBounds;
            var lanes = initSubArea.VaildLanes;
            var walls = initSubArea.Walls;
            Transformation.SetToTranslation(vec.X, vec.Y);
            var new_builds = buildingsToMove.Select(b => Transformation.Transform(b) as Polygon).ToList();
            var new_ramps = rampsToMove.Select(r => r.Transform(vec)).ToList();
            var new_BBs = BBsToMove.Select(b => Transformation.Transform(b) as Polygon).ToList();
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
            var new_SubArea = new OSubArea(initSubArea.Region, newLanes, initSubArea.Walls, new_builds, new_ramps, new_BBs);
            return new_SubArea;
        }

        private void ClearRecord()
        {
            buildingsToMove.Clear();
            buildingsNotMove.Clear();

            rampsToMove.Clear();
            rampsNotMove.Clear();

            BBsToMove.Clear();
            BBsNotMove.Clear();

            ConnectionList.Clear();

            initSubArea = null;
        }
    }


    public class BuildingPosCalculate
    {
        public List<OSubArea> InitSubAreas;//原始分区

        private List<Polygon> MovingBounds;//可动框线

        public List<List<MSubArea>> DynamicSubAreas = new List<List<MSubArea>>();

        private double halfLaneWidth = -0.1 + VMStock.RoadWidth / 2;

        //1.获取所有可能的移动方案
        //网格+特殊点 
        //筛选合理解
        //确保一个分区只有一个建筑，且移动不会跨区域
        //2.可移动方案分配到子进程
        //3.每个建筑筛选最多个数的移动方案
        //4.按最多个数进行排布，返回排布方案
        public BuildingPosCalculate()
        {
            var model =  new BuildingPosCalculate(OInterParameter.MovingBounds);
            InitSubAreas = model.InitSubAreas;
            MovingBounds = model.MovingBounds;
            DynamicSubAreas = model.DynamicSubAreas;
        }

        public BuildingPosCalculate(List<Polygon> movingBounds)
        {
            InitSubAreas = OInterParameter.GetMovingOsubAreas();
            InitSubAreas.ForEach(s =>s.UpdateParkingCnts(true));
            MovingBounds = movingBounds;
            foreach(var bound in MovingBounds)
            {
                DynamicSubAreas.Add(new List<MSubArea>());
                for (int i = 0;i < InitSubAreas.Count; i++)
                {
                    var tempArea = InitSubAreas[i];
                    if (tempArea.Region.Intersects(bound))
                    {
                        var mSubArea = new MSubArea(tempArea, bound,i);
                        if (mSubArea.IsVaild) DynamicSubAreas.Last().Add(mSubArea);
                    }
                }
            }
        }
        public  List<List<int>> CalculateScore(List<List<Vector2D>> potentialMovingVectors,bool Update = false)
        {
            var BestVectors = new List<Vector2D>();
            var BestParkingCnts = new List<int>();
            if (potentialMovingVectors.Count != MovingBounds.Count) throw new ArgumentException("Vectors have different length with moving bounds!");
            var Scores = new List<List<int>>();
            for(int i = 0; i < MovingBounds.Count; i++)
            {
                var bound = MovingBounds[i];
                var DMsubAreas = DynamicSubAreas[i];
                //var maxCnt = DMsubAreas.Sum(s => s.InitParkingCnt);
                var vectors = potentialMovingVectors[i];
                Scores.Add(new List<int>());
                foreach (var vector in vectors)
                { 
                    if (vector.Length() == 0)
                    {
                        Scores.Last().Add(-1);
                        continue;
                    }
                    var movedAreas = DMsubAreas.Select(s => s.GetMovedArea(vector)).ToList();
                    movedAreas.ForEach(a =>a.UpdateParkingCnts(true));
                    Scores.Last().Add(movedAreas.Sum(a =>a.Count));
                }
            }
            return Scores;
        }
    }
    //动态子区域，包含可动建筑，可动坡道，动态车道(与可动建筑距离<最大可动距离)的尽端车道
    //每个可动建筑对应一个动态子区域
    public class MSubArea
    {
        //终于在眼泪中明白
        //有些人 一旦错过就不在
        public bool IsVaild = false;
        public int InitParkingCnt;
        public int InitIndex;
        private List<LineString> Walls;
        private Polygon Region;

        private List<Polygon> buildingsToMove = new List<Polygon>();
        private List<Polygon> buildingsNotMove = new List<Polygon>();

        private List<ORamp> rampsToMove = new List<ORamp>();
        private List<ORamp> rampsNotMove = new List<ORamp>();

        private List<Polygon> BBsToMove = new List<Polygon>();
        private List<Polygon> BBsNotMove = new List<Polygon>();

        private List<LineSegment> LanesToMove = new List<LineSegment>();
        private List<LineSegment> LanesNotMove = new List<LineSegment>();

        private AffineTransformation Transformation = new AffineTransformation();

        private List<(bool, bool)> ConnectionList = new List<(bool, bool)>();
        private double halfLaneWidth = -0.1 + VMStock.RoadWidth / 2;
        private double maxMoveDistance = VMStock.BuildingMoveDistance;
        static BufferParameters MitreParam = new BufferParameters(8, EndCapStyle.Flat, JoinStyle.Mitre, 5.0);
        public MSubArea(OSubArea oSubArea,Polygon bound,int initIndex)
        {
            InitParkingCnt = oSubArea.Count;
            InitIndex = initIndex;
            var buildings = oSubArea.Buildings;
            var ramps = oSubArea.Ramps;
            var buildingBounds = oSubArea.BuildingBounds;
            var lanes = oSubArea.VaildLanes;
            Walls = oSubArea.Walls;
            Region = oSubArea.Region;
            //筛选动态建筑
            foreach (var building in buildings)
            {
                if (bound.Intersects(building)) buildingsToMove.Add(building);
                else buildingsNotMove.Add(building);
            }
            if(buildingsToMove.Count == 0) return;//分区不包含可动建筑。该分区不需要考虑
            //筛选可动坡道
            foreach (var ramp in ramps)
            {
                if (bound.Intersects(ramp.Area)) rampsToMove.Add(ramp);
                else rampsNotMove.Add(ramp);
            }
            // 筛选可动bounding
            foreach (var BB in buildingBounds)
            {
                if (bound.Intersects(BB)) BBsToMove.Add(BB);
                else BBsNotMove.Add(BB);
            }
            var objs = new GeometryCollection(Walls.Cast<Geometry>().ToList().Concat(buildings.Cast<Geometry>()).ToArray());
            var maxBound = bound.Buffer(maxMoveDistance,MitreParam);
            // 筛选动态车道
            foreach (var lane in lanes)
            {
                var Rect = lane.GetRect(halfLaneWidth);
                if(Rect.Intersects(maxBound))
                {
                    var posLane = lane.Positivize();
                    bool negConnect = posLane.P0.ToPoint().Distance(objs) < (halfLaneWidth - 0.1);
                    bool posConnect = posLane.P1.ToPoint().Distance(objs) < (halfLaneWidth - 0.1);
                    ConnectionList.Add((negConnect, posConnect));
                    LanesToMove.Add(lane);
                }
                else LanesNotMove.Add(lane);
            }
            IsVaild = true;
        }

        public OSubArea GetMovedArea(Vector2D vector)
        {
            Transformation.SetToTranslation(vector.X, vector.Y);
            var new_builds = buildingsToMove.Select(b => Transformation.Transform(b) as Polygon).ToList();
            var new_ramps = rampsToMove.Select(r => r.Transform(vector)).ToList();
            var new_BBs = BBsToMove.Select(b => Transformation.Transform(b) as Polygon).ToList();
            new_builds.AddRange(buildingsNotMove);
            new_ramps.AddRange(rampsNotMove);
            new_BBs.AddRange(BBsNotMove);
            var boundarySPIdx = new MNTSSpatialIndex(new_builds.Cast<Geometry>().ToList().Concat(Walls));
            var newLanes = new List<LineSegment>();
            for (int i = 0; i < LanesToMove.Count; i++)
            {
                var lane = LanesToMove[i];
                var connection = ConnectionList[i];
                newLanes.Add(lane.GetVaildLane(connection, boundarySPIdx));
            }
            newLanes.AddRange(LanesNotMove);
            var new_SubArea = new OSubArea(Region, newLanes, Walls, new_builds, new_ramps, new_BBs);
            return new_SubArea;
        }
    }
}
