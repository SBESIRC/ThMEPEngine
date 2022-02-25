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

using ThMEPWSS.SprinklerPiping.Model;
using ThMEPEngineCore.Diagnostics;

namespace ThMEPWSS.SprinklerPiping.Engine
{
    class SprinklerGraphAnalyzingEngine
    {
        public static List<SprinklerPoint> GetLinkedSprinklerPoints(SprinklerPipingParameter parameter) //TODO: 需要return吗
        {
            //List<SprinklerPoint> sprinklerPoints = new List<SprinklerPoint>();
            List<SprinklerPoint> sprinklerPoints = parameter.sprinklerPoints;
            List<ThSprinklerNetGroup> netList = parameter.netList;
            Dictionary<Point3d, SprinklerPoint> ptDic = parameter.ptDic;

            ////TODO: 获取graph
            ////sprinkPts - 喷淋点位的list
            //var sprinkPts = sprinklerParameter.SprinklerPt;
            //sprinkPts.ForEach(x => DrawUtils.ShowGeometry(x, "l0pt", 4, 30, 125));

            //var dtOrthogonalSeg = ThSprinklerNetworkService.FindOrthogonalAngleFromDT(sprinkPts, out var dtSeg); //德劳内三角话以后找正交角的线 list<line>格式 line是直线段，polyline是多段线

            //var DTTol = ThSprinklerNetworkService.GetDTLength(dtOrthogonalSeg);

            //ThSprinklerNetworkService.FilterTooLongSeg(ref dtOrthogonalSeg, DTTol * 3);

            //var netList = ThSprinklerPtNetworkEngine.CreateSegGroup(dtOrthogonalSeg, dtSeg, sprinkPts, DTTol); //成网 List<ThSprinklerNetGroup>

            int sptIdx = 0;
            //sprinklerPoints = new List<SprinklerPoint>();
            //遍历点
            for (int groupIdx = 0; groupIdx < netList.Count; groupIdx++)
            {
                ThSprinklerNetGroup group = netList[groupIdx];
                List<Point3d> groupPts = group.Pts;
                double angle = group.Angle;
                double ucsAngle = angle % 90;
                List<int> ptIdxFromGraph = new List<int>(); //所有graph中包含的点的idx

                //遍历graph
                for (int graphIdx = 0; graphIdx < group.PtsGraph.Count; graphIdx++)
                {
                    ThSprinklerGraph graph = group.PtsGraph[graphIdx];
                    for(int nodeIdx = 0; nodeIdx < graph.SprinklerVertexNodeList.Count; nodeIdx++)
                    {
                        //if(sptIdx == 122)
                        //{
                        //    int a = 0;
                        //}
                        ThSprinklerVertexNode node = graph.SprinklerVertexNodeList[nodeIdx];
                        int ptIdx = node.NodeIndex;
                        SprinklerPoint curPoint = new SprinklerPoint(sptIdx, groupPts[ptIdx].X, groupPts[ptIdx].Y, groupIdx, graphIdx, nodeIdx, ptIdx, ucsAngle);

                        //DrawUtils.ShowGeometry(curPoint.pos, String.Format("l00pts-{0}", sptIdx));

                        if (ptDic.ContainsKey(curPoint.pos))
                        {
                            //选近的
                            SprinklerPoint cpt = ptDic[curPoint.pos];
                            SprinklerPoint cptEdge = cpt.leftNeighbor != null ? cpt.leftNeighbor : (cpt.rightNeighbor != null ? cpt.rightNeighbor : cpt.upNeighbor != null ? cpt.upNeighbor : cpt.downNeighbor);
                            ThSprinklerEdgeNode curEdge = node.FirstEdge;
                            //SprinklerPoint curEdgePoint = null;
                            if(cpt.idx == 122)
                            {
                                int aa = 1;
                            }
                            double curMinDist = groupPts[graph.SprinklerVertexNodeList[curEdge.EdgeIndex].NodeIndex].DistanceTo(curPoint.pos);
                            while (curEdge.Next != null)
                            {
                                curEdge = curEdge.Next;
                                double dist = groupPts[graph.SprinklerVertexNodeList[curEdge.EdgeIndex].NodeIndex].DistanceTo(curPoint.pos);
                                curMinDist = curMinDist > dist ? dist : curMinDist;
                                //if (curEdge.EdgeIndex < nodeIdx)
                                //{
                                //    curEdgePoint = sprinklerPoints.Find(p => (p.nodeIdx == curEdge.EdgeIndex && p.graphIdx == graphIdx && p.groupIdx == groupIdx));
                                //    break;
                                //}
                                //curEdge = curEdge.Next;
                            }
                            if(cptEdge != null && (curMinDist >= cpt.pos.DistanceTo(cptEdge.pos)))
                            {
                                //ignore curPoint
                                ptIdxFromGraph.Add(ptIdx);
                                continue;

                            }
                            else
                            {
                                //remove cpt
                                //sprinklerPoints.Add(curPoint);
                                
                                curPoint.idx = cpt.idx;
                                sprinklerPoints[cpt.idx] = curPoint;
                                ptDic[curPoint.pos] = curPoint;
                                //sprinklerPoints.Remove(cpt);

                                if (cpt.rightNeighbor != null) cpt.rightNeighbor.leftNeighbor = null;
                                if (cpt.leftNeighbor != null) cpt.leftNeighbor.rightNeighbor = null;
                                if (cpt.upNeighbor != null) cpt.upNeighbor.downNeighbor = null;
                                if (cpt.downNeighbor != null) cpt.downNeighbor.upNeighbor = null;
                            }
                        }
                        else
                        {
                            sprinklerPoints.Add(curPoint);
                            sptIdx++;
                            ptDic.Add(curPoint.pos, curPoint);
                        }
                        //sprinklerPoints.Add(curPoint);
                        //ptDic.Add(curPoint.pos, curPoint);
                        ptIdxFromGraph.Add(ptIdx);

                        ThSprinklerEdgeNode edgeNode = node.FirstEdge;
                        while (edgeNode != null)
                        {
                            int edgeIdx = edgeNode.EdgeIndex;
                            if (edgeIdx < nodeIdx)
                            {
                                SprinklerPoint edgePoint = sprinklerPoints.Find(p => (p.nodeIdx == edgeIdx && p.graphIdx == graphIdx && p.groupIdx == groupIdx));
                                if(edgePoint != null)
                                {
                                    if (Math.Round(edgePoint.pos.X, 1) > Math.Round(curPoint.pos.X, 1) && Math.Round(edgePoint.pos.Y, 1) >= Math.Round(curPoint.pos.Y, 1))
                                    {
                                        curPoint.rightNeighbor = edgePoint;
                                        edgePoint.leftNeighbor = curPoint;
                                    }
                                    else if (Math.Round(edgePoint.pos.X, 1) <= Math.Round(curPoint.pos.X, 1) && Math.Round(edgePoint.pos.Y, 1) > Math.Round(curPoint.pos.Y, 1))
                                    {
                                        curPoint.upNeighbor = edgePoint;
                                        edgePoint.downNeighbor = curPoint;
                                    }
                                    else if (Math.Round(edgePoint.pos.X, 1) < Math.Round(curPoint.pos.X, 1) && Math.Round(edgePoint.pos.Y, 1) <= Math.Round(curPoint.pos.Y, 1))
                                    {
                                        curPoint.leftNeighbor = edgePoint;
                                        edgePoint.rightNeighbor = curPoint;
                                    }
                                    else
                                    {
                                        curPoint.downNeighbor = edgePoint;
                                        edgePoint.upNeighbor = curPoint;
                                    }
                                }
                            }
                            edgeNode = edgeNode.Next;
                        }

                        //if (ptDic.ContainsKey(curPoint.pos))
                        //{
                        //    //选近的
                        //    SprinklerPoint cpt = ptDic[curPoint.pos];
                        //    SprinklerPoint cptEdge = cpt.leftNeighbor != null ? cpt.leftNeighbor : (cpt.rightNeighbor != null ? cpt.rightNeighbor : cpt.upNeighbor != null ? cpt.upNeighbor : cpt.downNeighbor);
                        //    SprinklerPoint curEdge = curPoint.leftNeighbor != null ? curPoint.leftNeighbor : (curPoint.rightNeighbor != null ? curPoint.rightNeighbor : curPoint.upNeighbor != null ? curPoint.upNeighbor : curPoint.downNeighbor);
                        //    if(curPoint.pos.DistanceTo(curEdge.pos) >= cpt.pos.DistanceTo(cptEdge.pos))
                        //{
                        //    ptIdxFromGraph.Add(ptIdx);

                        //    continue;
                        //}
                        //    else
                        //    {

                        //    }
                        //}
                        //else
                        //{
                        //    sprinklerPoints.Add(curPoint);
                        //    ptDic.Add(curPoint.pos, curPoint);
                        //}
                        ////sprinklerPoints.Add(curPoint);
                        ////ptDic.Add(curPoint.pos, curPoint);
                        //ptIdxFromGraph.Add(ptIdx);
                    }

                }

                //不在图中的点
                ptIdxFromGraph.Sort();
                int startIdx = ptIdxFromGraph[0];
                int lastIdx = 0;
                for (; lastIdx<startIdx; lastIdx++)
                {
                    SprinklerPoint curPoint = new SprinklerPoint(sptIdx++, groupPts[lastIdx].X, groupPts[lastIdx].Y, groupIdx, lastIdx, ucsAngle);
                    if (ptDic.ContainsKey(curPoint.pos))
                    {
                        SprinklerPoint cpt = ptDic[curPoint.pos];
                        continue;
                    }
                    sprinklerPoints.Add(curPoint);
                    ptDic.Add(curPoint.pos, curPoint);
                }
                for (int curIdx = 1; curIdx < ptIdxFromGraph.Count; curIdx++) 
                {
                    int endIdx = ptIdxFromGraph[curIdx];
                    for(int idx=startIdx+1; idx<endIdx; idx++)
                    {
                        SprinklerPoint curPoint = new SprinklerPoint(sptIdx++, groupPts[idx].X, groupPts[idx].Y, groupIdx, idx, ucsAngle);
                        sprinklerPoints.Add(curPoint);
                        ptDic.Add(curPoint.pos, curPoint);
                    }
                    startIdx = endIdx;
                }
                startIdx++;
                while(startIdx < groupPts.Count)
                {
                    SprinklerPoint curPoint = new SprinklerPoint(sptIdx++, groupPts[startIdx].X, groupPts[startIdx].Y, groupIdx, startIdx, ucsAngle);
                    sprinklerPoints.Add(curPoint);
                    ptDic.Add(curPoint.pos, curPoint);
                    startIdx++;
                }
            
            }

            return sprinklerPoints;
        }


    }
}
