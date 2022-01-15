using System;
using NFox.Cad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service.Number;

namespace ThMEPLighting.Garage.Service.Arrange
{
    public abstract class ThArrangeService
    {
        #region --------- input ---------
        protected ThRegionBorder RegionBorder { get; set; }
        protected ThLightArrangeParameter ArrangeParameter { get; set; }
        #endregion
        #region ---------- output ----------
        public int LoopNumber { get; protected set; }
        /// <summary>
        /// 中心线对应的边线
        /// </summary>
        public Dictionary<Line, Tuple<List<Line>, List<Line>>> CenterSideDicts { get; private set; }
        /// <summary>
        /// 对中心线建图，把连通的线分组
        /// </summary>
        public List<Tuple<Point3d, Dictionary<Line, Vector3d>>> CenterGroupLines { get; protected set; }
        /// <summary>
        /// 通过布灯线生成的图
        /// </summary>
        public List<ThLightGraphService> Graphs { get; protected set; }
        #endregion
        public ThArrangeService(
            ThRegionBorder regionBorder, 
            ThLightArrangeParameter arrangeParameter)
        {
            RegionBorder = regionBorder;
            ArrangeParameter = arrangeParameter;
            Graphs = new List<ThLightGraphService>();
            CenterSideDicts = new Dictionary<Line, Tuple<List<Line>, List<Line>>>();
            CenterGroupLines = new List<Tuple<Point3d, Dictionary<Line, Vector3d>>>();
        }
        public abstract void Arrange();
        protected List<Tuple<Point3d, Dictionary<Line, Vector3d>>> FindConnectedLine(List<Line> lines)
        {
            var results = new List<Tuple<Point3d, Dictionary<Line, Vector3d>>>();
            var centerEdges = lines.Select(o => new ThLightEdge(o)).ToList();
            var graphs = centerEdges.CreateGraphs();
            graphs.ForEach(g =>
            {
                var dict = new Dictionary<Line, Vector3d>();
                g.GraphEdges.ForEach(o => dict.Add(o.Edge, o.Direction));
                results.Add(Tuple.Create(g.StartPoint, dict));
            });
            return results;
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
           if( ArrangeParameter.LayoutMode == LayoutMode.SpanBeam || 
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

        protected void PassNumber(List<ThLightEdge> firstEdges,List<ThLightEdge> secondEdges)
        {
            // 把1号线的编号传递到2号线
            var passNumberService = new ThPassNumberService(
                firstEdges,secondEdges,LoopNumber,
                ArrangeParameter.DoubleRowOffsetDis);
            passNumberService.Pass();
        }
        protected void ReNumberSecondEdges(List<ThLightEdge> secondEdges)
        {
            var secondNumberService = new ThSecondNumberService(
                secondEdges, LoopNumber, ArrangeParameter.DefaultStartNumber + 1);
            secondNumberService.Number();
        }
        protected List<ThLightEdge> BuildEdges(List<Line> lines, EdgePattern edgePattern)
        {
            var edges = new List<ThLightEdge>();
            lines.ForEach(o => edges.Add(new ThLightEdge(o) { EdgePattern = edgePattern }));
            return edges;
        }
    }
}
