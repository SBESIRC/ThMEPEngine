﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.DrainageSystemDiagram;
using ThMEPWSS.SprinklerConnect.Model;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Engine;



namespace ThMEPWSS.SprinklerConnect.Model
{
    public class ThSprinklerNetGroup
    {
        public List<Point3d> pts { get; private set; } = new List<Point3d>();//在图里的喷头点，不在图里的散点，在图里的和支干管相交的虚拟点
        public List<Point3d> ptsVirtual { get; private set; } = new List<Point3d>();//在图里的和支干管相交的虚拟点
        public List<ThSprinklerGraph> ptsGraph { get; private set; } = new List<ThSprinklerGraph>();//图列表
        public List<Line> lines { get; private set; } = new List<Line>();//所有的线列表，包括和支干管相交的打断线
        public double angle { get; set; } = 0;//组角度

        public ThSprinklerNetGroup()
        {
        }

        public int AddPt(Point3d pt)
        {
            var tol = new Tolerance(10, 10);
            var idx = -1;
            var alreadyIn = pts.Where(x => x.IsEqualTo(pt, tol));
            if (alreadyIn.Count() > 0)
            {
                idx = pts.IndexOf(alreadyIn.First());
            }
            else
            {
                pts.Add(pt);
                idx = pts.Count - 1;
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
            for (int i = 0; i < ptsGraph[graphIdx]. SprinklerVertexNodeList.Count; i++)
            {
                var p = pts[ptsGraph[graphIdx].SprinklerVertexNodeList[i].NodeIndex];
                ptsInGraph.Add(p);
            }

            return ptsInGraph;
        }

    }
}
