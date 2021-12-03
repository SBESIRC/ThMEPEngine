using System;
using System.Linq;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using System.Collections.Generic;
using ThMEPLighting.Garage.Service;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Engine
{
    public abstract class ThArrangementEngine : IDisposable
    {
        protected ThLightArrangeParameter ArrangeParameter { get; set; }
        /// <summary>
        /// 根据一个分区的布灯点的数量，计算的回路数
        /// </summary>
        public int LoopNumber { get; protected set; }

        protected int DefaultStartNumber { get; set; }
        /// <summary>
        /// 通过布灯线生成的图
        /// </summary>
        public List<ThLightGraphService> Graphs { get; protected set; }

        public ThArrangementEngine(ThLightArrangeParameter arrangeParameter)
        {
            ArrangeParameter = arrangeParameter;
            Graphs = new List<ThLightGraphService>();
            DefaultStartNumber = arrangeParameter.DefaultStartNumber;
        }
        public void Dispose()
        {
        }
        public abstract void Arrange(ThRegionBorder regionBorder);
        protected abstract void Preprocess(ThRegionBorder regionBorder);
        
        protected virtual void Filter(ThRegionBorder regionBorder)
        {
            double tTypeBranchFilterLength = Math.Max(ArrangeParameter.MinimumEdgeLength,
                ArrangeParameter.Margin*2.0+ ArrangeParameter.Interval / 2.0);
            regionBorder.DxCenterLines = ThFilterTTypeCenterLineService.Filter(
                regionBorder.DxCenterLines, tTypeBranchFilterLength);      
        }        
        protected List<ThLightGraphService> CreateGraphs(List<ThLightEdge> lightEdges)
        {
            // 传入的Edges是
            var results = new List<ThLightGraphService>();
            while (lightEdges.Count > 0)
            {
                if (lightEdges.Where(o => o.IsDX).Count() == 0)
                {
                    break;
                }
                Point3d findSp = lightEdges.Where(o => o.IsDX).First().Edge.StartPoint;
                var priorityStart= lightEdges.Select(o => o.Edge).ToList().FindPriorityStart(ThGarageLightCommon.RepeatedPointDistance);
                if(priorityStart!=null)
                {
                    findSp = priorityStart.Item2;
                }
                //对灯线边建图,创建从findSp开始可以连通的图
                var lightGraph = new ThCdzmLightGraphService(lightEdges, findSp);
                lightGraph.Build();

                var traversedLightEdges = lightGraph.GraphEdges;

                //找到从ports中的点出发拥有最长边的图
                var centerEdges = traversedLightEdges.Select(e=>new ThLightEdge(e.Edge)).ToList();
                var centerStart = LaneServer.getMergedOrderedLane(centerEdges);

                // 使用珣若算的最优起点重新建图
                traversedLightEdges.ForEach(e => e.IsTraversed = false);
                var newLightGraph = new ThCdzmLightGraphService(traversedLightEdges, centerStart);
                newLightGraph.Build();
                //newLightGraph.Print();

                lightEdges = lightEdges.Where(o => o.IsTraversed == false).ToList();
                results.Add(newLightGraph);
            }
            return results;
        }
        public void SetDefaultStartNumber(int defaultStartNumber)
        {
            this.DefaultStartNumber = defaultStartNumber;
        }
        protected double FilterPointDistance
        {
            get
            {
                return 0.4 * ArrangeParameter.Interval;
            }
        }
    }
}
