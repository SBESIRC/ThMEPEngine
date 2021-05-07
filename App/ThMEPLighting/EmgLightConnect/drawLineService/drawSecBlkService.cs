using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADExtension;
using NFox.Cad;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Algorithm.AStarAlgorithm;
using ThMEPLighting.EmgLight.Assistant;
using ThMEPLighting.EmgLightConnect.Model;

namespace ThMEPLighting.EmgLightConnect.Service
{
    public class drawSecBlkService
    {
        public static List<Polyline> drawSecToMain(List<ThSingleSideBlocks> singleSideBlocks, Polyline frame, List<ThBlock> blkList, ref List<Polyline> linkLine,List<Polyline > holes)
        {
            List<Polyline> connList = new List<Polyline>();
            for (int sideIndex = 0; sideIndex < singleSideBlocks.Count; sideIndex++)
            {
                var side = singleSideBlocks[sideIndex];

                for (int i = 0; i < side.ptLink.Count; i++)
                {
                    var ptL = side.ptLink[i];

                    if ((side.reSecBlk.Contains(ptL.Item1) && side.getTotalBlock().Contains(ptL.Item2)) || (side.reSecBlk.Contains(ptL.Item2) && side.getTotalBlock().Contains(ptL.Item1)))
                    {
                        var linkTemp = linkSecToMain(ptL, blkList, out var blkS, out var blkE);
                        var link = CorrectConflictFrame(frame, linkTemp, blkS, blkE, holes);
                        connList.Add(link);
                    }
                }
            }

            linkLine.AddRange(connList);
            return connList;
        }

        public static List<Polyline> drawGroupToGroup(List<(Point3d, Point3d)> connPts, Polyline frame, List<ThBlock> blkList, ref List<Polyline> linkLine)
        {
            List<Polyline> connList = new List<Polyline>();

            for (int i = 0; i < connPts.Count; i++)
            {
                var link = linkSecToMain(connPts[i], blkList, out var blkS, out var blkE);
                connList.Add(link);
            }

            linkLine.AddRange(connList);
            return connList;
        }

        private static Polyline linkSecToMain((Point3d, Point3d) ptL, List<ThBlock> blkList, out ThBlock blkS, out ThBlock blkE)
        {
            blkS = BlockListService.getBlockByCenter(ptL.Item1, blkList);
            blkE = BlockListService.getBlockByCenter(ptL.Item2, blkList);

            Point3d ptS = new Point3d();
            Point3d ptE = new Point3d();
            Point3d ptAdd = new Point3d();
            var poly = new Polyline();

            var lineTemp = new Line(blkS.cenPt, blkE.cenPt);
            if (lineTemp.Length > EmgConnectCommon.TolTooClosePt)
            {
                ptS = drawEmgPipeService.getConnectPt(blkS, lineTemp);
                ptE = drawEmgPipeService.getConnectPt(blkE, lineTemp);

            }
            else
            {
                //未来躲柱子用
                ptS = drawEmgPipeService.getConnectPt(blkS, lineTemp);
                ptE = drawEmgPipeService.getConnectPt(blkE, lineTemp);

            }

            poly.AddVertexAt(poly.NumberOfVertices, ptS.ToPoint2d(), 0, 0, 0);
            if (ptAdd != Point3d.Origin)
            {
                poly.AddVertexAt(poly.NumberOfVertices, ptAdd.ToPoint2d(), 0, 0, 0);
            }
            poly.AddVertexAt(poly.NumberOfVertices, ptE.ToPoint2d(), 0, 0, 0);

            blkS.connInfo[ptS].Add(poly);
            blkE.connInfo[ptE].Add(poly);

            return poly;

        }

        private static Polyline CorrectConflictFrame(Polyline frame, Polyline linkTemp, ThBlock blkS, ThBlock blkE,List<Polyline> holes)
        {
            Polyline link = linkTemp;
            //var holes = new List<Polyline>();
            var sDir = new Vector3d(1, 0, 0);
            sDir = sDir.TransformBy(blkS.blk.BlockTransform).GetNormal();
            //blkS.connInfo[linkTemp.StartPoint].Remove(linkTemp);
            //blkE.connInfo[linkTemp.EndPoint].Remove(linkTemp);

            var pts = linkTemp.Intersect(frame, Intersect.OnBothOperands);
            holes.ForEach(x => pts.AddRange(linkTemp.Intersect(x, Intersect.OnBothOperands)));

            if (pts.Count > 0)
            {
                var sPt = blkS.blkCenPt;
                var ePt = blkE.blkCenPt;

                AStarRoutePlanner<Point3d> aStarRoute = new AStarRoutePlanner<Point3d>(frame, sDir, ePt, 400, 0, 0);
                aStarRoute.SetObstacle(holes);
                var res = aStarRoute.Plan(sPt);

                var resCut = drawEmgPipeService.cutLane(sPt, ePt, blkS, blkE, res);

                link = resCut;
            }


            return link;
        }


    }
}
