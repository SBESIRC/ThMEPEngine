﻿using System.Collections.Generic;
using System.Linq;
using System;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.SprinklerDim.Service;

namespace ThMEPWSS.SprinklerDim.Model
{
    public class ThSprinklerNetGroup
    {
        public List<Point3d> Pts { get; set; } = new List<Point3d>();//在图里的喷头点

        public List<ThSprinklerGraph> PtsGraph { get; set; } = new List<ThSprinklerGraph>();//图列表

        public List<List<List<int>>> XCollineationGroup { get; set; } = new List<List<List<int>>>();//X相同距离较近的形成组

        public List<List<List<int>>> YCollineationGroup { get; set; } = new List<List<List<int>>>();//Y相同距离较近的形成组

        public List<List<ThSprinklerDimGroup>> XDimension { get; set; } = new List<List<ThSprinklerDimGroup>>();//X方向的喷淋标注点

        public List<List<ThSprinklerDimGroup>> YDimension { get; set; } = new List<List<ThSprinklerDimGroup>>();//Y方向的喷淋标注点

        public HashSet<Tuple<int, int>> LinesCuttedOffByWall = new HashSet<Tuple<int, int>>();//被墙打断的两点间连线

        //public List<Line> Lines { get; private set; } = new List<Line>();//所有的线列表，包括和支干管相交的打断线
        public double Angle { get; set; } = 0;//组角度

        public Matrix3d Transformer { get; set; } = new Matrix3d();//转换矩阵

        public ThSprinklerNetGroup()
        {

        }

        public ThSprinklerNetGroup(double angle)
        {
             Angle = angle;
        }

        public ThSprinklerNetGroup(List<Point3d> pts, List<ThSprinklerGraph> ptsGraph, Matrix3d transformer)
        {
            this.Pts = pts;
            this.PtsGraph = ptsGraph;
            this.Transformer = transformer;
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
