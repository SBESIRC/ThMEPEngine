using System;
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
using NetTopologySuite.Operation.Relate;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;

using ThMEPWSS.DrainageSystemDiagram;
using ThMEPWSS.SprinklerConnect.Model;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Engine;

namespace ThMEPWSS.SprinklerConnect.Service
{
    internal class ThSprinklerNetGraphService
    {
        /// <summary>
        /// 一组线转成图组
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="groupLine"></param>
        /// <returns></returns>
        public static ThSprinklerNetGroup CreateNetwork(double angle, List<Line> groupLine)
        {
            var tol = new Tolerance(10, 10);
            var lines = new List<Line>();
            var graphListTemp = new List<ThSprinklerGraph>();

            var pts = ThSprinklerLineService.LineListToPtList(groupLine);
            lines.AddRange(groupLine);

            var net = new ThSprinklerNetGroup();
            net.angle = angle;
            var alreadyAdded = new List<Line>();
            while (pts.Count > 0)
            {
                var graph = new ThSprinklerGraph();
                graphListTemp.Add(graph);

                var p = pts[0];
                CreateGraph(p, lines, alreadyAdded, net, graph);
                pts.RemoveAll(x => net.pts.Contains(x));
            }

            
            net.ptsGraph.AddRange(graphListTemp.OrderByDescending(x => x.SprinklerVertexNodeList.Count).ToList());




            return net;

        }

        /// <summary>
        /// 成图
        /// </summary>
        /// <param name="p"></param>
        /// <param name="lines"></param>
        /// <param name="alreadyAdded"></param>
        /// <param name="net"></param>
        /// <param name="graph"></param>
        private static void CreateGraph(Point3d p, List<Line> lines, List<Line> alreadyAdded, ThSprinklerNetGroup net, ThSprinklerGraph graph)
        {
            var tol = new Tolerance(10, 10);

            var connL = ThSprinklerLineService.GetConnLine(p, lines);

            for (int i = 0; i < connL.Count; i++)
            {
                if (alreadyAdded.Contains(connL[i]) == false)
                {
                    var ptOther = connL[i].StartPoint;
                    if (p.IsEqualTo(connL[i].StartPoint, tol))
                    {
                        ptOther = connL[i].EndPoint;
                    }
                    AddEdge(p, connL[i], net, graph);
                    alreadyAdded.Add(connL[i]);

                    lines.RemoveAll(x => x == connL[i]);

                    CreateGraph(ptOther, lines, alreadyAdded, net, graph);
                }
            }
        }

        /// <summary>
        /// 图加边
        /// </summary>
        /// <param name="p"></param>
        /// <param name="l"></param>
        /// <param name="net"></param>
        /// <param name="graph"></param>
        private static void AddEdge(Point3d p, Line l, ThSprinklerNetGroup net, ThSprinklerGraph graph)
        {
            var tol = new Tolerance(10, 10);

            var ptOther = l.StartPoint;
            if (p.IsEqualTo(l.StartPoint, tol))
            {
                ptOther = l.EndPoint;
            }
            var idxPt = net.AddPt(p);
            var idxPtO = net.AddPt(ptOther);
            net.lines.Add(l);


            graph.AddVertex(idxPt);
            graph.AddVertex(idxPtO);
            graph.AddEdge(idxPt, idxPtO);
            graph.AddEdge(idxPtO, idxPt);

        }

        /// <summary>
        /// 根据干管支干管打断图
        /// </summary>
        /// <param name="netGroup"></param>
        /// <param name="mainPipe"></param>
        /// <param name="subMainPipe"></param>
        public static void CreatePartGroup(ThSprinklerNetGroup netGroup, List<Line> mainPipe, List<Line> subMainPipe)
        {

            var lineList = new List<Line>();
            lineList.AddRange(netGroup.lines);

            DrawUtils.ShowGeometry(lineList, "l1origLines", 3, 30);
            var deleteList = RemoveLineIntersectWithMain(lineList, mainPipe);
            BreakLineIntersectWithSub(lineList, subMainPipe, out var breakLine, out var breakPoint);

            DrawUtils.ShowGeometry(deleteList, "l1removeLines", 3, 25);
            DrawUtils.ShowGeometry(breakLine, "l1breakLines", 211, 25);
            DrawUtils.ShowGeometry(lineList, "l1afterLines", 110, 25);
            breakPoint.ForEach(x => DrawUtils.ShowGeometry(x, "l1breakPts", 3, 30, 125));


            var newNetGroup = CreateNetwork(netGroup.angle, lineList);
            for (int j = 0; j < newNetGroup.ptsGraph.Count; j++)
            {
                var lines = newNetGroup.ptsGraph[j].print(newNetGroup.pts);
                DrawUtils.ShowGeometry(lines, string.Format("l2graph0-{0}", j), j % 7);
            }


            AddBreakLineToGraph(newNetGroup, breakLine, breakPoint);
            for (int j = 0; j < newNetGroup.ptsGraph.Count; j++)
            {
                var lines = newNetGroup.ptsGraph[j].print(newNetGroup.pts);
                DrawUtils.ShowGeometry(lines, string.Format("l3graph0-{0}", j), j % 7);
            }
        }

        public static void CreatePartGroup_test(ThSprinklerNetGroup netGroup, List<Line> mainPipe, List<Line> subMainPipe)
        {

            var lineList = new List<Line>();
            lineList.AddRange(netGroup.lines);

            BreakLineIntersectWithSub(lineList, subMainPipe, out var breakLine, out var breakPoint);

            var newNetGroup = CreateNetwork(netGroup.angle, lineList);


            AddBreakLineToGraph(newNetGroup, breakLine, breakPoint);
            for (int j = 0; j < newNetGroup.ptsGraph.Count; j++)
            {
                var lines = newNetGroup.ptsGraph[j].print(newNetGroup.pts);
                DrawUtils.ShowGeometry(lines, string.Format("l3graph0-{0}", j), j % 7);
            }
        }

        /// <summary>
        /// 删掉干管相交线
        /// </summary>
        /// <param name="lineList"></param>
        /// <param name="mainPipe"></param>
        /// <returns></returns>
        private static List<Line> RemoveLineIntersectWithMain(List<Line> lineList, List<Line> mainPipe)
        {
            var removeList = new List<Line>();

            foreach (var l in lineList)
            {
                foreach (var main in mainPipe)
                {
                    var pts = new Point3dCollection();
                    l.IntersectWith(main, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    if (pts.Count > 0)
                    {
                        removeList.Add(l);
                        break;
                    }
                }
            }

            lineList.RemoveAll(x => removeList.Contains(x));


            return removeList;
        }

        /// <summary>
        /// breakLine ：原点位 到 打断点
        /// 打断支干管相交线
        /// </summary>
        /// <param name="lineList"></param>
        /// <param name="subMainPipe"></param>
        /// <param name="breakLine"></param>
        /// <param name="breakPoint"></param>
        private static void BreakLineIntersectWithSub(List<Line> lineList, List<Line> subMainPipe, out List<Line> breakLine, out List<Point3d> breakPoint)
        {
            breakLine = new List<Line>();
            breakPoint = new List<Point3d>();
            var breakLineRemove = new List<Line>();

            foreach (var l in lineList)
            {
                foreach (var main in subMainPipe)
                {
                    var breakPt = new Point3dCollection();
                    l.IntersectWith(main, Intersect.OnBothOperands, breakPt, (IntPtr)0, (IntPtr)0);
                    if (breakPt.Count > 0)
                    {
                        breakPoint.Add(breakPt[0]);
                        breakLine.Add(new Line(l.StartPoint, breakPt[0]));
                        breakLine.Add(new Line(l.EndPoint, breakPt[0]));
                        breakLineRemove.Add(l);
                    }
                }
            }

            lineList.RemoveAll(x => breakLineRemove.Contains(x));
        }

        /// <summary>
        /// 增加打断线到图组
        /// </summary>
        /// <param name="net"></param>
        /// <param name="breakLine"></param>
        /// <param name="breakPoint"></param>
        private static void AddBreakLineToGraph(ThSprinklerNetGroup net, List<Line> breakLine, List<Point3d> breakPoint)
        {
            net.ptsVirtual.AddRange(breakPoint);

            for (int i = 0; i < net.ptsGraph.Count; i++)
            {
                var graph = net.ptsGraph[i];

                for (int j = breakLine.Count - 1; j >= 0; j--)
                {
                    var bkL = breakLine[j];
                    var ptIdx = net.pts.IndexOf(bkL.StartPoint);
                    var VertexIdx = graph.SearchNodeIndex(ptIdx);
                    if (VertexIdx != -1)
                    {
                        AddEdge(bkL.StartPoint, bkL, net, graph);
                        breakLine.RemoveAt(j);
                    }
                }
            }

            for (int i = breakLine.Count - 1; i >= 0; i--)
            {
                var bkL = breakLine[i];

                var graph = new ThSprinklerGraph();
                AddEdge(bkL.StartPoint, bkL, net, graph);
                net.ptsGraph.Add(graph);

                breakLine.RemoveAt(i);
            }
        }
    }
}
