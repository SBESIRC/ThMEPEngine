using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using QuickGraph;
using QuickGraph.Algorithms.Search;
using System;
using System.Collections.Generic;
using ThMEPElectrical.SystemDiagram.Engine;

namespace ThMEPElectrical.SystemDiagram.Model
{
    public class ThWireCircuitGraphModel
    {
        public DataSummary Data { get; set; }

        /// <summary>
        /// 回路编号
        /// </summary>
        public int WireCircuitName { get; set; } = 0;

        /// <summary>
        /// 回路坐标
        /// </summary>
        public Point3d NamePoint { get; set; }

        /// <summary>
        /// 是否绘画回路编号
        /// </summary>
        public bool DrawWireCircuitNameText { get; set; } = false;

        /// <summary>
        /// 是否绘画回路
        /// </summary>
        public bool DrawWireCircuit { get; set; } = true;

        /// <summary>
        /// 路径图
        /// 以块抽象成图的节点，List<Curve>抽象成边
        /// </summary>
        public AdjacencyGraph<ThAFASVertex, ThAFASEdge<ThAFASVertex>> WireCircuitGraph = new AdjacencyGraph<ThAFASVertex, ThAFASEdge<ThAFASVertex>>();

        public void test()
        {
            
        }
    }
}
