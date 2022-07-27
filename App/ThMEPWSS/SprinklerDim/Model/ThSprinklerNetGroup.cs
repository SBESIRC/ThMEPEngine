using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.SprinklerDim.Service;

namespace ThMEPWSS.SprinklerDim.Model
{
    public class ThSprinklerNetGroup
    {
        public List<Point3d> Pts { get; private set; } = new List<Point3d>();//在图里的喷头点，不在图里的散点，在图里的和支干管相交的虚拟点

        //public List<Point3d> PtsVirtual { get; private set; } = new List<Point3d>();//在图里的和支干管相交的虚拟点
        public List<ThSprinklerGraph> PtsGraph { get; private set; } = new List<ThSprinklerGraph>();//图列表

        //public List<Line> Lines { get; private set; } = new List<Line>();//所有的线列表，包括和支干管相交的打断线
        public double Angle { get; set; } = 0;//组角度

        public Matrix3d transformer { get; set; } = new Matrix3d();//转换矩阵

        public ThSprinklerNetGroup()
        {

        }

        public int AddPt(Point3d pt)
        {
            var tol = new Tolerance(10, 10);
            var idx = -1;
            var alreadyIn = Pts.Where(x => x.IsEqualTo(pt, tol));
            if (alreadyIn.Count() > 0)
            {
                idx = Pts.IndexOf(alreadyIn.First());
            }
            else
            {
                Pts.Add(pt);
                idx = Pts.Count - 1;
            }
            return idx;
        }

        /// <summary>
        /// 获取图
        /// </summary>
        /// <param name="graphIdx"></param>
        /// <returns></returns>
        public List<Point3d> GetGraphPts(int graphIdx)
        {
            var ptsInGraph = new List<Point3d>();
            for (int i = 0; i < PtsGraph[graphIdx].SprinklerVertexNodeList.Count; i++)
            {
                var p = Pts[PtsGraph[graphIdx].SprinklerVertexNodeList[i].NodeIndex];
                ptsInGraph.Add(p);
            }

            return ptsInGraph;
        }

        //public List<Line> GetGraphLines(int graphIdx)
        //{
        //    var linesInGraph = new List<Line>();
        //    var linesInGraphHash = new HashSet<Line>();
        //    var graph = PtsGraph[graphIdx];

        //    for (int i = 0; i < graph.SprinklerVertexNodeList.Count; i++)
        //    {
        //        var node = graph.SprinklerVertexNodeList[i].FirstEdge;
        //        while (node != null)
        //        {
        //            var sp = Pts[graph.SprinklerVertexNodeList[i].NodeIndex];
        //            var ep = Pts[graph.SprinklerVertexNodeList[node.EdgeIndex].NodeIndex];

        //            var lineTemp = ThSprinklerLineService.GetLineFromList(Lines, sp, ep);
        //            lineTemp.ForEach(x => linesInGraphHash.Add(x));
        //            node = node.Next;
        //        }
        //    }

        //    linesInGraph = linesInGraphHash.ToList();
        //    return linesInGraph;

        //}

        ///// 寻找图中的所有虚拟点
        ///// </summary>
        ///// <param name="graphIdx"></param>
        ///// <returns></returns>
        //public List<Point3d> GetVirtualPts(int graphIdx)
        //{
        //    return GetGraphPts(graphIdx).Where(pt => PtsVirtual.Contains(pt)).ToList();
        //}

        ///// <summary>
        ///// 搜索图中所有的虚拟点在pts中的索引
        ///// </summary>
        ///// <param name="graphIdx"></param>
        ///// <returns></returns>
        //public List<int> GetVirtualPtsIndex(int graphIdx)
        //{
        //    var ptsIndex = new List<int>();
        //    var tol = new Tolerance(10, 10);

        //    GetVirtualPts(graphIdx).ForEach(pt =>
        //    {
        //        var alreadyIn = Pts.Where(x => x.IsEqualTo(pt, tol));
        //        if (alreadyIn.Count() > 0)
        //        {
        //            var idx = Pts.IndexOf(alreadyIn.First());
        //            ptsIndex.Add(idx);
        //        }
        //    });

        //    return ptsIndex;
        //}
    }
}
