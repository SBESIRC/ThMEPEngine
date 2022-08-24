using NFox.Cad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service.Number;

namespace ThMEPLighting.Garage.Service.Arrange
{
    /// <summary>
    /// 在传入的灯线上布灯点，
    /// 创建灯编号
    /// </summary>
    public class ThLightingArrangeService
    {
        #region --------- input ---------
        protected ThRegionBorder RegionBorder { get; set; }
        protected ThLightArrangeParameter ArrangeParameter { get; set; }
        #endregion
        #region ---------- output ----------
        public int LoopNumber { get; protected set; }
        /// <summary>
        /// 通过布灯线生成的图
        /// </summary>
        public List<ThLightGraphService> Graphs { get; protected set; }
        #endregion
        public ThLightingArrangeService(
            ThRegionBorder regionBorder,
            ThLightArrangeParameter arrangeParameter)
        {
            RegionBorder = regionBorder;
            ArrangeParameter = arrangeParameter;
            Graphs = new List<ThLightGraphService>();
        }
        public void Arrange()
        {
            // 创建边(边上有布灯的点，且方向是跟随中心线方向的)
            var firstLightEdges = BuildEdges(RegionBorder.FirstLightingLines, EdgePattern.First);
            var secondLightEdges = BuildEdges(RegionBorder.SecondLightingLines, EdgePattern.Second);

            // 布点
            CreateDistributePointEdges(firstLightEdges, secondLightEdges);

            // 计算回路数量
            // 计算此regionBorder中的回路数（此值要返回）
            var lightNumber = firstLightEdges.CalculateLightNumber() + secondLightEdges.CalculateLightNumber();
            this.LoopNumber = ArrangeParameter.GetLoopNumber(lightNumber);

            // 编号
            var firstGraphs = firstLightEdges.CreateGraphs();
            // 为1号线编号
            firstGraphs.ForEach(f => f.Number1(this.LoopNumber, false, ArrangeParameter.DefaultStartNumber, ArrangeParameter.IsDoubleRow));
            // 把1号线编号,传递到2号线
            PassNumber(firstGraphs.SelectMany(g => g.GraphEdges).ToList(), secondLightEdges);

            // 对2号线建图
            var secondGraphs = secondLightEdges.CreateGraphs();
            Graphs.AddRange(firstGraphs);
            Graphs.AddRange(secondGraphs);
        }
        protected void CreateDistributePointEdges(
            List<ThLightEdge> firstLightEdges,
            List<ThLightEdge> secondLightEdges)
        {
            // 创建带布点的边n
            var lightEdgeDistribute = new ThLightEdgeDistributePointService()
            {
                FirstLightEdges = firstLightEdges,
                SecondLightEdges = secondLightEdges,
                ArrangeParameter = this.ArrangeParameter,
                Beams = GetBeams(),
                Columns = GetColumns(),
            };
            lightEdgeDistribute.Distribute();
        }
        private DBObjectCollection GetBeams()
        {
            if (ArrangeParameter.LayoutMode == LayoutMode.SpanBeam ||
                 ArrangeParameter.LayoutMode == LayoutMode.AvoidBeam)
            {
                return RegionBorder.Beams.Select(o => o.Outline).ToCollection();
            }
            else
            {
                return new DBObjectCollection();
            }
        }

        private DBObjectCollection GetColumns()
        {
            if (ArrangeParameter.LayoutMode == LayoutMode.ColumnSpan)
            {
                return RegionBorder.Columns.Select(o => o.Outline).ToCollection();
            }
            else
            {
                return new DBObjectCollection();
            }
        }

        protected void PassNumber(List<ThLightEdge> firstEdges, List<ThLightEdge> secondEdges)
        {
            // 把1号线的编号传递到2号线
            var passNumberService = new ThPassNumberService(firstEdges, secondEdges, LoopNumber, ArrangeParameter.DoubleRowOffsetDis,
                ArrangeParameter.IsDoubleRow);
            passNumberService.Pass();
        }

        protected List<ThLightEdge> BuildEdges(List<Line> lines, EdgePattern edgePattern)
        {
            var edges = new List<ThLightEdge>();
            lines.ForEach(o => edges.Add(new ThLightEdge(o) { EdgePattern = edgePattern }));
            return edges;
        }
    }
}
