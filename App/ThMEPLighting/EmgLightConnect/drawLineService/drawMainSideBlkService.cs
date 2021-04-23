﻿using System;
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


namespace ThMEPLighting.EmgLightConnect.Service
{
    public class drawMainSideBlkService
    {
        public static void drawMainToMain(List<ThSingleSideBlocks> singleSideBlocks, List<BlockReference> blkSource, Polyline frame, out List<ThBlock> blkList)
        {
            var moveLanePolyList = new List<Polyline>();

            blkList = new List<ThBlock>();
            GetBlockService.getBlkList(singleSideBlocks, blkSource, ref blkList);

            for (int sideIndex = 0; sideIndex < singleSideBlocks.Count; sideIndex++)
            {
                var side = singleSideBlocks[sideIndex];

                if (side.reMainBlk.Count > 1)
                {
                    //debug : 不穿框线
                    var movedline = moveLane(side, frame);

                    //断线 or 延长
                    Dictionary<Point3d, Point3d> reMainBlkOnLineDict = new Dictionary<Point3d, Point3d>(); //key:remainblk, value:blk project to movedline
                    for (int i = 0; i < side.reMainBlk.Count; i++)
                    {
                        reMainBlkOnLineDict.Add(side.reMainBlk[i], movedline.GetClosestPointTo(side.reMainBlk[i], true));
                    }

                    //生成小支管
                    //确定连接点
                    //如果project 点在 图块obb（外扩一点点）内（线穿过图框）， 穿过框线边的点
                    //如果project 点在 图块obb 外 找最近的点
                    for (int i = 1; i < side.reMainBlk.Count; i++)
                    {
                        var prevPt = side.reMainBlk[i - 1];
                        var pt = side.reMainBlk[i];

                        var leftLineTemp = getMoveLinePart(reMainBlkOnLineDict[prevPt], reMainBlkOnLineDict[pt], movedline, out int prevPolyInx, out int ptPolyInx);
                        var prevConnPt = drawEmgPipeService . getConnectPt(prevPt, leftLineTemp, blkList);
                        var prevConnProjPt = leftLineTemp.GetClosestPointTo(prevConnPt, true);
                        var bAddedPrevConn = tryDistByDegree(prevConnPt, prevConnProjPt, leftLineTemp, out var preAddedPt);

                        var rightLineTemp = getMoveLinePart(reMainBlkOnLineDict[pt], reMainBlkOnLineDict[prevPt], movedline, out int ptPolyInx2, out int prevPolyInx2);
                        var connPt = drawEmgPipeService.getConnectPt(pt, rightLineTemp, blkList);
                        var connProjPt = rightLineTemp.GetClosestPointTo(connPt, true);
                        var bAddedConn = tryDistByDegree(connPt, connProjPt, rightLineTemp, out var addedPt);

                        //生成主polyline
                        var moveLanePoly = new Polyline();
                        moveLanePoly.AddVertexAt(moveLanePoly.NumberOfVertices, prevConnPt.ToPoint2d(), 0, 0, 0);

                        if (bAddedPrevConn == true)
                        {
                            moveLanePoly.AddVertexAt(moveLanePoly.NumberOfVertices, preAddedPt.ToPoint2d(), 0, 0, 0);
                        }

                        if (prevPolyInx < ptPolyInx)
                        {
                            for (int j = prevPolyInx + 1; j < ptPolyInx + 1; j++)
                            {
                                moveLanePoly.AddVertexAt(moveLanePoly.NumberOfVertices, movedline.GetPoint2dAt(j), 0, 0, 0);
                            }
                        }
                        if (prevPolyInx > ptPolyInx)
                        {
                            for (int j = prevPolyInx; j > ptPolyInx; j--)
                            {
                                moveLanePoly.AddVertexAt(moveLanePoly.NumberOfVertices, movedline.GetPoint2dAt(j), 0, 0, 0);
                            }
                        }
                        if (bAddedConn == true)
                        {

                            moveLanePoly.AddVertexAt(moveLanePoly.NumberOfVertices, addedPt.ToPoint2d(), 0, 0, 0);
                        }

                        moveLanePoly.AddVertexAt(moveLanePoly.NumberOfVertices, connPt.ToPoint2d(), 0, 0, 0);
                        moveLanePolyList.Add(moveLanePoly);
                    }

                    DrawUtils.ShowGeometry(movedline, EmgConnectCommon.LayerMovedLane, Color.FromColorIndex(ColorMethod.ByColor, 50));
                }

                DrawUtils.ShowGeometry(moveLanePolyList, EmgConnectCommon.LayerConnectLineFinal, Color.FromColorIndex(ColorMethod.ByColor, 130));

            }
        }

        private static Polyline moveLane(ThSingleSideBlocks side, Polyline frame)
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

                movedline = checkMoveLineIntersectOutFrame(movedlineTemp, lanePoly, frame, offset, side);

            }

            //外扩movedline保证点project 在moveline范围内
            //if (movedline.NumberOfVertices > 0)
            //{

            //    movedline.ExtendPolyline(EmgConnectCommon.TolSaperateGroupMaxDistance);

            //}

            return movedline;
        }

        private static Line getMoveLinePart(Point3d PrevPtPrj, Point3d ptPrj, Polyline movedLine, out int prevPolyInx, out int ptPolyInx)
        {
            Line moveLinePart = new Line();
            Tolerance tol = new Tolerance(1, 1);
            prevPolyInx = -1;
            ptPolyInx = -1;

            for (int i = 0; i < movedLine.NumberOfVertices - 1; i++)
            {
                var lineTemp = movedLine.GetLineSegmentAt(i);

                if (lineTemp.IsOn(PrevPtPrj, tol))
                {
                    prevPolyInx = i;
                }
                if (lineTemp.IsOn(ptPrj, tol))
                {
                    ptPolyInx = i;
                }

                if (prevPolyInx != -1 && ptPolyInx != -1)
                { break; }
            }

            moveLinePart.StartPoint = PrevPtPrj;

            if (prevPolyInx < ptPolyInx)
            {
                moveLinePart.EndPoint = movedLine.GetPoint3dAt(prevPolyInx + 1);
            }
            if (prevPolyInx > ptPolyInx)
            {
                moveLinePart.EndPoint = movedLine.GetPoint3dAt(prevPolyInx);
            }
            if (prevPolyInx == ptPolyInx)
            {
                moveLinePart.EndPoint = ptPrj;
            }

            return moveLinePart;
        }

        private static bool tryDistByDegree(Point3d connPt, Point3d connPtProj, Line seg, out Point3d addPt)
        {
            var bAddPt = false;
            double adjacent = -1;
            bool bEnd = false;
            addPt = new Point3d();

            double opposite = (connPt - connPtProj).Length;
            int degree = 30;

            while (bEnd == false)
            {
                if (opposite <= 20)
                {
                    adjacent = 0;
                    bEnd = true;
                }
                if (bEnd == false && seg.Length <= 500)
                {
                    addPt = seg.StartPoint;
                    bAddPt = true;
                    bEnd = true;
                }

                if (bEnd == false)
                {
                    adjacent = opposite / Math.Tan(degree * Math.PI / 180);

                    if (adjacent < seg.Length / 5)
                    {
                        addPt = connPtProj + adjacent * (seg.EndPoint - seg.StartPoint).GetNormal();
                        bAddPt = true;
                        bEnd = true;
                    }
                }
                if (bEnd == false)
                {
                    degree = degree + 5;
                }

                if (degree >= 80)
                {
                    degree = 90;
                    addPt = seg.StartPoint;
                    bAddPt = true;
                    bEnd = true;
                }
            }

            return bAddPt;

        }

        private static Polyline checkMoveLineIntersectOutFrame(Polyline movedlineTemp, Polyline lanePoly, Polyline frame, double offset, ThSingleSideBlocks side)
        {
            lanePoly.SetPointAt(0, lanePoly.GetClosestPointTo(side.reMainBlk.First(), true).ToPoint2d());
            lanePoly.SetPointAt(lanePoly.NumberOfVertices - 1, lanePoly.GetClosestPointTo(side.reMainBlk.Last(), true).ToPoint2d());

            movedlineTemp.SetPointAt(0, movedlineTemp.GetClosestPointTo(side.reMainBlk.First(), true).ToPoint2d());
            movedlineTemp.SetPointAt(movedlineTemp.NumberOfVertices - 1, movedlineTemp.GetClosestPointTo(side.reMainBlk.Last(), true).ToPoint2d());

            var moveLine = movedlineTemp.Clone() as Polyline;

            var movelanePolygon = lanePoly.Clone() as Polyline;
            for (int i = movedlineTemp.NumberOfVertices - 1; i >= 0; i--)
            {
                movelanePolygon.AddVertexAt(movelanePolygon.NumberOfVertices, movedlineTemp.GetPoint2dAt(i), 0, 0, 0);
            }
            movelanePolygon.Closed = true;

            ThCADCoreNTSRelate relation = new ThCADCoreNTSRelate(movelanePolygon, frame);
            if (relation.IsOverlaps)
            {
                var polyCollection = new DBObjectCollection() { frame };
                var overlap = movelanePolygon.Intersection(polyCollection);

                if (overlap.Count > 0)
                {
                    var overlapPoly = overlap.Cast<Polyline>().OrderByDescending(x => x.Area).First();
                    double newOffsetTemp = 200000;
                    for (int i = 0; i < overlapPoly.NumberOfVertices - 1; i++)
                    {
                        var dist = lanePoly.GetDistToPoint(overlapPoly.GetPoint3dAt(i), false);
                        if (100 < dist && dist < newOffsetTemp)
                        {
                            newOffsetTemp = dist;
                        }
                    }

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

                    double newOffset = getLaneDisplacement(side.laneSide.Select(x => x.Item1).ToList(), side.reMainBlk, newOffsetTemp);
                    moveLine = lanePoly.GetOffsetCurves(newOffset * offSetDir)[0] as Polyline;
                }
            }

            return moveLine;

        }

        private static double getLaneDisplacement(List<Line> lanes, List<Point3d> blocks, double maxOffset)
        {
            var displacementList = new List<double>();

            foreach (var blk in blocks)
            {
                displacementList.Add(lanes.Select(x => x.GetDistToPoint(blk, false)).Min());
            }

            var distance = displacementList
                            .Where(x => x < maxOffset)
                            .OrderBy(x => x)
                            .GroupBy(x => Math.Floor(x / 10))
                            .OrderByDescending(x => x.Count())
                            .First()
                            .ToList()
                            .First();

            return distance;
        }

    }
}
