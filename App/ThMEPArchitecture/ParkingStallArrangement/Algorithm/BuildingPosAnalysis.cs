using Dreambuild.AutoCAD;
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
using ThMEPArchitecture.MultiProcess;
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
        private Geometry CenterLaneGeo;
        private BuildingPosCalculate BPC;
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
            CenterLaneGeo = new MultiLineString(centerLanes.ToLineStrings().ToArray()).Buffer(HalfLaneWidth,MitreParam);
            //CenterLaneGeo.Get<Polygon>(false).ForEach(p => p.ToDbMPolygon().AddToCurrentSpace());
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

        public bool IsVaild(int index,Vector2D vector)
        {
            AffineTransformation transformation = new AffineTransformation();
            var bound = OInterParameter.MovingBounds[index];
            transformation.SetToTranslation(vector.X, vector.Y);
            var newBound = transformation.Transform(bound);
            if (newBound.Disjoint(CenterLaneGeo)) return true;
            else return false;
        }

        public int CalculateScore(int index,Vector2D vector)
        {
            if (BPC == null) BPC = new BuildingPosCalculate();
            return BPC.CalculateScore(index, vector);
        }
        public void UpdateBest()
        {
            var BPC = new BuildingPosCalculate();
            InitSubAreas = BPC.InitSubAreas;
            //InitSubAreas.ForEach(s => s.Display("初始小分区"));
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
