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
using ThMEPLighting.EmgLightConnect.Model;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.Algorithm.AStarAlgorithm;

namespace ThMEPLighting.EmgLightConnect.Service
{
    public class drawMainBlkService
    {
        public static List<Polyline> drawMainToMain(List<ThSingleSideBlocks> singleSideBlocks, List<BlockReference> blkSource, Polyline frame, List<Polyline> holes, out List<ThBlock> blkList, ref List<Polyline> linkLine)
        {
            
            blkList = new List<ThBlock>();
            BlockListService.getBlkList(singleSideBlocks, blkSource, ref blkList);
            var moveLanePolyList = new List<Polyline>();

            for (int sideIndex = 0; sideIndex < singleSideBlocks.Count; sideIndex++)
            {
                var side = singleSideBlocks[sideIndex];
                var passedMain = new List<Point3d>();

                if (side.reMainBlk.Count > 1)
                {
                    for (int reMI = 0; reMI < side.reMainBlk.Count; reMI++)
                    {
                        if (passedMain.Contains(side.reMainBlk[reMI]) == false)
                        {
                            var moveLane = side.moveLaneList.Where(x => x.Item2.Contains(side.reMainBlk[reMI])).First();
                            passedMain.AddRange(moveLane.Item2);

                            ConnectSingleSideService.forDebugPrintBlkLable(moveLane.Item2, "l5moveblk");

                            if (moveLane.Item2.Count > 1)
                            {
                                var movedline = moveLane.Item1;

                                DrawUtils.ShowGeometry(movedline, "l0moveLaneSeg");

                                for (int i = 1; i < moveLane.Item2.Count; i++)
                                {
                                    var prevPt = moveLane.Item2[i - 1];
                                    var pt = moveLane.Item2[i];

                                    var prevBlk = BlockListService.getBlockByCenter(prevPt, blkList);
                                    var thisBlk = BlockListService.getBlockByCenter(pt, blkList);

                                    var moveLanePoly = drawEmgPipeService.cutLane(prevPt, pt, prevBlk, thisBlk, movedline);
                                    //moveLanePoly= drawEmgPipeService.CorrectConflictFrame(frame, moveLanePoly, prevBlk, thisBlk, holes);

                                    if (moveLanePoly != null)
                                    {
                                        moveLanePolyList.Add(moveLanePoly);
                                    }
                                }
                            }
                            
                            if (reMI > 0)
                            {
                               var resCut= connectEachMainPart(side, reMI, blkList, frame, holes );
                                if (resCut != null)
                                {
                                    moveLanePolyList.Add(resCut);
                                }
                               
                            }
                        }
                    }
                }
            }

            linkLine.AddRange(moveLanePolyList);
            return moveLanePolyList;
        }

        private static Polyline  connectEachMainPart(ThSingleSideBlocks side,int reMI, List<ThBlock> blkList,Polyline frame,List<Polyline> holes)
        {
            Polyline resCut = null;
            int TolDistDalta = 1000;

            var prePt = side.reMainBlk[reMI - 1];
            var thisPt = side.reMainBlk[reMI];

            var prevBlk = BlockListService.getBlockByConnect(prePt, blkList);
            var thisBlk = BlockListService.getBlockByConnect(thisPt, blkList);

            var sDir = new Vector3d(1, 0, 0);
            sDir = sDir.TransformBy(prevBlk.blk.BlockTransform).GetNormal();

            AStarRoutePlanner<Point3d> aStarRoute = new AStarRoutePlanner<Point3d>(frame, sDir, thisBlk.blkCenPt, 400, 0, 0);
            aStarRoute.SetObstacle(holes);
            var res = aStarRoute.Plan(prevBlk.blkCenPt);

            DrawUtils.ShowGeometry(res, "l0mainSegAStar");

            resCut = res;

            if (res!=null && res.NumberOfVertices >= 2)
            {
                var seg = res.GetLineSegmentAt(0);
                if (seg.Length < TolDistDalta)
                {
                    res.RemoveVertexAt(0);
                }
                seg = res.GetLineSegmentAt(res.NumberOfVertices - 2);
                if (seg.Length < TolDistDalta)
                {
                    res.RemoveVertexAt(res.NumberOfVertices - 1);
                }
                 resCut = drawEmgPipeService.cutLane(prevBlk.blkCenPt, thisBlk.blkCenPt, prevBlk, thisBlk, res);
            }

            

            return resCut;
        }
    }
}
