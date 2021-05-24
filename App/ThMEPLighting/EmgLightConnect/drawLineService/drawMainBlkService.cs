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
using ThMEPLighting.EmgLight.Assistant;
using ThMEPLighting.EmgLightConnect.Model;
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

               
                var moveLanePolyMainList = new List<Polyline>();

                if (side.reMainBlk.Count > 1)
                {
                    for (int reMI = 0; reMI < side.reMainBlk.Count; reMI++)
                    {
                        if (passedMain.Contains(side.reMainBlk[reMI]) == false)
                        {
                            var moveLane = side.moveLaneList.Where(x => x.Item2.Contains(side.reMainBlk[reMI])).First();
                            passedMain.AddRange(moveLane.Item2);

                            if (moveLane.Item2.Count > 1)
                            {
                                var movedline = moveLane.Item1;
                                DrawUtils.ShowGeometry(movedline, EmgConnectCommon.LayerMovedLane, Color.FromColorIndex(ColorMethod.ByColor, 50));
                                for (int i = 1; i < moveLane.Item2.Count; i++)
                                {
                                    var prevPt = moveLane.Item2[i - 1];
                                    var pt = moveLane.Item2[i];

                                    var prevBlk = BlockListService.getBlockByCenter(prevPt, blkList);
                                    var thisBlk = BlockListService.getBlockByCenter(pt, blkList);

                                    var moveLanePoly = drawEmgPipeService.cutLane(prevPt, pt, prevBlk, thisBlk, movedline);

                                    if (moveLanePoly != null)
                                    {
                                        moveLanePolyMainList.Add(moveLanePoly);
                                    }
                                }
                            }
                            //else if (moveLane.Item2.Count ==1)
                            //{
                            //    var moveLanePoly = new Polyline();
                            //    moveLanePoly.AddVertexAt(0, moveLane.Item2[0].ToPoint2d(), 0, 0, 0);
                            //    moveLanePolyMainList.Add(moveLanePoly);
                            //}
                            if (reMI > 0)
                            {
                                var prePt = side.reMainBlk[reMI - 1];
                                var thisPt = side.reMainBlk[reMI];

                                var prevBlk = BlockListService.getBlockByConnect(prePt, blkList);
                                var thisBlk = BlockListService.getBlockByConnect(thisPt, blkList);

                                var sDir = new Vector3d(1, 0, 0);
                                sDir = sDir.TransformBy(prevBlk.blk.BlockTransform).GetNormal();

                                AStarRoutePlanner<Point3d> aStarRoute = new AStarRoutePlanner<Point3d>(frame, sDir, thisBlk.cenPt, 400, 0, 0);
                                aStarRoute.SetObstacle(holes);
                                var res = aStarRoute.Plan(prevBlk.cenPt);

                                if (res.NumberOfVertices >2)
                                {
                                    var seg = res.GetLineSegmentAt(0);
                                    if (seg.Length <1000)
                                    {
                                        res.RemoveVertexAt(0);
                                    }
                                    seg = res.GetLineSegmentAt(res.NumberOfVertices - 2);
                                    if (seg.Length <1000)
                                    {
                                        res.RemoveVertexAt(res.NumberOfVertices-1);
                                    }
                                }

                                DrawUtils.ShowGeometry(res, "l0asdf");
                                
                                var resCut = drawEmgPipeService.cutLane(prevBlk.cenPt, thisBlk.cenPt, prevBlk, thisBlk, res);

                                moveLanePolyList.Add(resCut);
                            }
                        }
                        
                    }
                }

                moveLanePolyList.AddRange(moveLanePolyMainList);
                
            }

            linkLine.AddRange(moveLanePolyList);
            return moveLanePolyList;
        }




















        public static List<Polyline> drawMainToMainNouse(List<ThSingleSideBlocks> singleSideBlocks, List<BlockReference> blkSource, Polyline frame, out List<ThBlock> blkList, ref List<Polyline> linkLine)
        {
            var moveLanePolyList = new List<Polyline>();

            blkList = new List<ThBlock>();
            BlockListService.getBlkList(singleSideBlocks, blkSource, ref blkList);

            for (int sideIndex = 0; sideIndex < singleSideBlocks.Count; sideIndex++)
            {
                var side = singleSideBlocks[sideIndex];

                if (side.reMainBlk.Count > 1)
                {
                    var movedline = moveLane(side, frame);


                    for (int i = 1; i < side.reMainBlk.Count; i++)
                    {
                        var prevPt = side.reMainBlk[i - 1];
                        var pt = side.reMainBlk[i];

                        var prevBlk = BlockListService.getBlockByCenter(prevPt, blkList);
                        var thisBlk = BlockListService.getBlockByCenter(pt, blkList);

                        var moveLanePoly = drawEmgPipeService.cutLane(prevPt, pt, prevBlk, thisBlk, movedline);

                        if (moveLanePoly != null)
                        {
                            moveLanePolyList.Add(moveLanePoly);
                        }


                    }
                    //DrawUtils.ShowGeometry(movedline, EmgConnectCommon.LayerMovedLane, Color.FromColorIndex(ColorMethod.ByColor, 50));
                }
            }

            linkLine.AddRange(moveLanePolyList);
            return moveLanePolyList;
        }

        private static Polyline moveLane(ThSingleSideBlocks side, Polyline frame)
        {
            //找平移量
            double offset = getLaneDisplacement(side.laneSide.Select(x => x.Item1).ToList(), side.reMainBlk, EmgConnectCommon.TolSaperateGroupMaxDistance);
            Polyline movedline = null;

            var tempMoveLineList = new List<Line>();

            //平移车道线
            if (offset != 0)
            {
                for (int j = 0; j < side.laneSide.Count; j++)
                {
                    //GetOffsetCurves line 负值：右 正值：左  polyline 负值：左 正值： 右
                    var offSetDir = side.laneSide[j].Item2 == 0 ? 1 : -1;
                    var tempMoveLine = side.laneSide[j].Item1.GetOffsetCurves(offset * offSetDir)[0] as Line;

                    tempMoveLineList.Add(tempMoveLine);
                }

                movedline = checkMoveLineIntersectOutFrame(tempMoveLineList, side.laneSide.Select(x => x.Item1).ToList(), frame, offset, side);
            }

            return movedline;
        }

        private static Polyline checkMoveLineIntersectOutFrame(List<Line> tempMoveLine, List<Line> lane, Polyline frame, double offset, ThSingleSideBlocks side)
        {

            var lanePoly = getCutLane(lane, side.reMainBlk.First(), side.reMainBlk.Last());
            var moveLineTemp = getCutLane(tempMoveLine, side.reMainBlk.First(), side.reMainBlk.Last());
            var moveLine = moveLineTemp.Clone() as Polyline;

            var movelanePolygon = lanePoly.Clone() as Polyline;
            for (int i = moveLineTemp.NumberOfVertices - 1; i >= 0; i--)
            {
                movelanePolygon.AddVertexAt(movelanePolygon.NumberOfVertices, moveLineTemp.GetPoint2dAt(i), 0, 0, 0);
            }
            movelanePolygon.Closed = true;

            var pts = moveLine.Intersect(frame, Intersect.OnBothOperands);
            if (pts.Count > 0)
            {
                var polyCollection = new DBObjectCollection() { frame };
                var overlap = movelanePolygon.Intersection(polyCollection);

                if (overlap.Count > 0)
                {
                    var overlapPoly = overlap.Cast<Polyline>().OrderByDescending(x => x.Area).First();

                    double newOffsetTemp = 200000;
                    for (int i = 0; i < overlapPoly.NumberOfVertices; i++)
                    {
                        var dist = lanePoly.GetDistToPoint(overlapPoly.GetPoint3dAt(i), false);
                        if (100 < dist && dist < newOffsetTemp)
                        {
                            newOffsetTemp = dist;
                        }
                    }
                    newOffsetTemp = newOffsetTemp - EmgConnectCommon.TolLinkOffsetWithFrame;
                    double newOffset = getLaneDisplacement(side.laneSide.Select(x => x.Item1).ToList(), side.reMainBlk, newOffsetTemp);

                    var dir = (moveLineTemp.StartPoint - lanePoly.StartPoint).GetNormal();
                    var angle = dir.GetAngleTo((lanePoly.GetPoint3dAt(1) - lanePoly.StartPoint).GetNormal(), Vector3d.ZAxis) * 180 / Math.PI;
                    int offSetDir = 1;
                    if (80 <= angle && angle <= 100)
                    {
                        offSetDir = 1;
                    }
                    else
                    {
                        offSetDir = -1;
                    }

                    moveLine = lanePoly.GetOffsetCurves(newOffset * offSetDir)[0] as Polyline;
                }
            }

            return moveLine;

        }

        private static double getLaneDisplacement(List<Line> lanes, List<Point3d> blocks, double maxOffset)
        {
            var displacementList = new List<double>();
            double distance = 0;


            foreach (var blk in blocks)
            {
                displacementList.Add(lanes.Select(x => x.GetDistToPoint(blk, false)).Min());
            }

            var distanceListWithinOff = displacementList
                            .Where(x => x < maxOffset);

            if (distanceListWithinOff.Count() > 0)
            {
                distance = distanceListWithinOff
                               .OrderBy(x => x)
                               .GroupBy(x => Math.Floor(x / 10))
                               .OrderByDescending(x => x.Count())
                               .First()
                               .ToList()
                               .First();
            }
            else
            {
                distance = maxOffset;
            }

            return distance;
        }

        private static Polyline getCutLane(List<Line> lineList, Point3d spt, Point3d ept)
        {
            bool bEnd = false;
            Polyline lineP = new Polyline();

            var sPtPrj = drawEmgPipeService.getPrjPt(lineList, spt, out var sPtEx);
            var ePtPrj = drawEmgPipeService.getPrjPt(lineList, ept, out var ePtEx);

            lineP.AddVertexAt(lineP.NumberOfVertices, sPtPrj.ToPoint2d(), 0, 0, 0);

            if (ePtEx == -1 || sPtEx >= lineList.Count)
            {
                lineP.AddVertexAt(lineP.NumberOfVertices, ePtPrj.ToPoint2d(), 0, 0, 0);
                bEnd = true;
            }

            if (bEnd == false && sPtEx == -1)
            {
                sPtEx = 0;
                lineP.AddVertexAt(lineP.NumberOfVertices, lineList[0].StartPoint.ToPoint2d(), 0, 0, 0);
            }

            if (bEnd == false)
            {
                for (int i = sPtEx; i < ePtEx; i++)
                {
                    if (i < lineList.Count - 1)
                    {
                        lineP.AddVertexAt(lineP.NumberOfVertices, lineList[i].EndPoint.ToPoint2d(), 0, 0, 0);
                    }
                }
                if (ePtEx >= lineList.Count)
                {
                    lineP.AddVertexAt(lineP.NumberOfVertices, lineList[lineList.Count - 1].EndPoint.ToPoint2d(), 0, 0, 0);
                }
                lineP.AddVertexAt(lineP.NumberOfVertices, ePtPrj.ToPoint2d(), 0, 0, 0);
            }

            return lineP;

        }







        private static Polyline moveLaneNotUse(ThSingleSideBlocks side, Polyline frame)
        {
            //找平移量
            double offset = getLaneDisplacement(side.laneSide.Select(x => x.Item1).ToList(), side.reMainBlk, EmgConnectCommon.TolSaperateGroupMaxDistance);
            Polyline movedline = null;
            var movedlineTemp = new Polyline();
            var lanePoly = new Polyline();

            //平移车道线
            if (offset != 0)
            {
                for (int j = 0; j < side.laneSide.Count; j++)
                {
                    //GetOffsetCurves line 负值：右 正值：左  polyline 负值：左 正值： 右
                    var offSetDir = side.laneSide[j].Item2 == 0 ? 1 : -1;
                    var tempMoveLine = side.laneSide[j].Item1.GetOffsetCurves(offset * offSetDir)[0] as Line;

                    movedlineTemp.AddVertexAt(movedlineTemp.NumberOfVertices, tempMoveLine.StartPoint.ToPoint2d(), 0, 0, 0);
                    lanePoly.AddVertexAt(lanePoly.NumberOfVertices, side.laneSide[j].Item1.StartPoint.ToPoint2d(), 0, 0, 0);

                    if (j == side.laneSide.Count - 1)
                    {
                        movedlineTemp.AddVertexAt(movedlineTemp.NumberOfVertices, tempMoveLine.EndPoint.ToPoint2d(), 0, 0, 0);
                        lanePoly.AddVertexAt(lanePoly.NumberOfVertices, side.laneSide[j].Item1.EndPoint.ToPoint2d(), 0, 0, 0);
                    }
                }

                movedline = checkMoveLineIntersectOutFrameNoUse(movedlineTemp, lanePoly, frame, offset, side);

            }

            return movedline;
        }
        private static Polyline checkMoveLineIntersectOutFrameNoUse(Polyline movedlineTemp, Polyline lanePoly, Polyline frame, double offset, ThSingleSideBlocks side)
        {
            DrawUtils.ShowGeometry(lanePoly.GetClosestPointTo(side.reMainBlk.First(), false), "l0testtest2");
            DrawUtils.ShowGeometry(lanePoly.GetClosestPointTo(side.reMainBlk.Last(), false), "l0testtest2", Color.FromColorIndex(ColorMethod.ByColor, 40));


            DrawUtils.ShowGeometry(side.laneSide[0].Item1.GetClosestPointTo(side.reMainBlk.First(), true), "l0testtest2", Color.FromColorIndex(ColorMethod.ByColor, 220));


            lanePoly.SetPointAt(0, lanePoly.GetClosestPointTo(side.reMainBlk.First(), true).ToPoint2d());
            lanePoly.SetPointAt(lanePoly.NumberOfVertices - 1, lanePoly.GetClosestPointTo(side.reMainBlk.Last(), true).ToPoint2d());



            DrawUtils.ShowGeometry(movedlineTemp.GetClosestPointTo(side.reMainBlk.First(), true), "l0testtest2");
            DrawUtils.ShowGeometry(movedlineTemp.GetClosestPointTo(side.reMainBlk.Last(), true), "l0testtest2", Color.FromColorIndex(ColorMethod.ByColor, 40));

            movedlineTemp.SetPointAt(0, movedlineTemp.GetClosestPointTo(side.reMainBlk.First(), true).ToPoint2d());
            movedlineTemp.SetPointAt(movedlineTemp.NumberOfVertices - 1, movedlineTemp.GetClosestPointTo(side.reMainBlk.Last(), true).ToPoint2d());

            DrawUtils.ShowGeometry(lanePoly, "l0testtest");
            DrawUtils.ShowGeometry(movedlineTemp, "l0testtest");


            var moveLine = movedlineTemp.Clone() as Polyline;

            var movelanePolygon = lanePoly.Clone() as Polyline;
            for (int i = movedlineTemp.NumberOfVertices - 1; i >= 0; i--)
            {
                movelanePolygon.AddVertexAt(movelanePolygon.NumberOfVertices, movedlineTemp.GetPoint2dAt(i), 0, 0, 0);
            }
            movelanePolygon.Closed = true;
            DrawUtils.ShowGeometry(movelanePolygon, "l0testtest");
            ThCADCoreNTSRelate relation = new ThCADCoreNTSRelate(movelanePolygon, frame);

            if (relation.IsOverlaps)
            {
                var polyCollection = new DBObjectCollection() { frame };
                var overlap = movelanePolygon.Intersection(polyCollection);

                if (overlap.Count > 0)
                {
                    var overlapPoly = overlap.Cast<Polyline>().OrderByDescending(x => x.Area).First();
                    double newOffsetTemp = 200000;
                    for (int i = 0; i < overlapPoly.NumberOfVertices; i++)
                    {
                        var dist = lanePoly.GetDistToPoint(overlapPoly.GetPoint3dAt(i), false);
                        if (100 < dist && dist < newOffsetTemp)
                        {
                            newOffsetTemp = dist;
                        }
                    }
                    newOffsetTemp = newOffsetTemp - EmgConnectCommon.TolLinkOffsetWithFrame;
                    double newOffset = getLaneDisplacement(side.laneSide.Select(x => x.Item1).ToList(), side.reMainBlk, newOffsetTemp);

                    var dir = (movedlineTemp.StartPoint - lanePoly.StartPoint).GetNormal();
                    var angle = dir.GetAngleTo((lanePoly.GetPoint3dAt(1) - lanePoly.StartPoint).GetNormal(), Vector3d.ZAxis) * 180 / Math.PI;
                    int offSetDir = 1;
                    if (80 <= angle && angle <= 100)
                    {
                        offSetDir = 1;
                    }
                    else
                    {
                        offSetDir = -1;
                    }

                    moveLine = lanePoly.GetOffsetCurves(newOffset * offSetDir)[0] as Polyline;
                }
            }

            return moveLine;

        }

    }
}
