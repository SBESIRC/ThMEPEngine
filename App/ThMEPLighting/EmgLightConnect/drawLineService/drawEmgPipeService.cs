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
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.Algorithm.AStarAlgorithm;
using ThMEPLighting.EmgLightConnect.Model;

namespace ThMEPLighting.EmgLightConnect.Service
{
    public class drawEmgPipeService
    {
        public static Point3d getConnectPtNoUse(ThBlock blk, Line lineTemp)
        {
            Point3d connPt = new Point3d();
            Tolerance tol = new Tolerance(1, 1);
            Point3dCollection pts = new Point3dCollection();

            lineTemp.IntersectWith(blk.outline, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);

            if (pts.Count > 0)
            {
                connPt = ptOnOutlineSidePt(pts[pts.Count - 1], blk);
            }
            else
            {
                var connecPtDistDict = blk.getConnectPt().ToDictionary(x => x, x => x.DistanceTo(lineTemp.StartPoint));
                connPt = connecPtDistDict.OrderBy(x => x.Value).First().Key;
            }

            return connPt;
        }

        /// <summary>
        /// lineTemp start point should be close to blk
        /// </summary>
        /// <param name="blk"></param>
        /// <param name="lineTemp"></param>
        /// <returns></returns>
        public static Point3d getConnectPt(ThBlock blk, Line lineTemp)
        {
            int tolTooCloseDist = 100;
            Point3d connPt = new Point3d();
            Tolerance tol = new Tolerance(1, 1);
            Point3dCollection pts = new Point3dCollection();

            lineTemp.IntersectWith(blk.outline, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);

            if (pts.Count > 0)
            {
                connPt = ptOnOutlineSidePt(pts[pts.Count - 1], blk);
            }

            var connecPtDistDict = blk.getConnectPt().ToDictionary(x => x, x => lineTemp.GetDistToPoint(x, false));
            connecPtDistDict = connecPtDistDict.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

            //如果找不到，找最近的
            if (connPt == Point3d.Origin)
            {
                foreach (var pt in connecPtDistDict)
                {
                    if (pt.Value > tolTooCloseDist)
                    {
                        connPt = pt.Key;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            //如果还找不到，直接连第一个
            if (connPt == Point3d.Origin)
            {
                connPt = connecPtDistDict.First().Key;
            }

            return connPt;
        }

        private static Point3d ptOnOutlineSidePt(Point3d pt, ThBlock blk)
        {
            Point3d connPt = new Point3d();
            Tolerance tol = new Tolerance(10, 10);

            for (int i = 0; i < blk.outline.NumberOfVertices; i++)
            {
                var seg = new Line(blk.outline.GetPoint3dAt(i), blk.outline.GetPoint3dAt((i + 1) % blk.outline.NumberOfVertices));
                if (seg.ToCurve3d().IsOn(pt, tol))
                {
                    connPt = blk.getConnectPt().Where(x => seg.ToCurve3d().IsOn(x, tol)).FirstOrDefault();
                    break;
                }
            }
            return connPt;
        }

        private static bool ptOnPolyline(Point3d pt, Polyline line)
        {
            bool bReturn = false;
            Tolerance tol = new Tolerance(10, 10);

            for (int i = 0; i < line.NumberOfVertices; i++)
            {
                var seg = new Line(line.GetPoint3dAt(i), line.GetPoint3dAt((i + 1) % line.NumberOfVertices));
                if (seg.ToCurve3d().IsOn(pt, tol))
                {
                    bReturn = true;
                    break;
                }
            }
            return bReturn;
        }

        public static Polyline cutLane(Point3d prevP, Point3d pt, ThBlock prevBlk, ThBlock thisBlk, Polyline movedline)
        {
            var moveLanePoly = new Polyline();
            var prevProjP = movedline.GetClosestPointTo(prevP, true);
            var projP = movedline.GetClosestPointTo(pt, true);

            var leftLineTemp = getMoveLinePart(prevProjP, projP, movedline, out int prevPolyInx, out int ptPolyInx);
            var prevConnPt = drawEmgPipeService.getConnectPt(prevBlk, leftLineTemp);
            var prevConnProjPt = leftLineTemp.GetClosestPointTo(prevConnPt, true);

            var rightLineTemp = getMoveLinePart(projP, prevProjP, movedline, out int ptPolyInx2, out int prevPolyInx2);
            var connPt = drawEmgPipeService.getConnectPt(thisBlk, rightLineTemp);
            var connProjPt = rightLineTemp.GetClosestPointTo(connPt, true);


            if (ptOnPolyline(prevConnPt, thisBlk.outline) == true)
            {
                moveLanePoly = null;
                return moveLanePoly;
            }

            //生成小支管
            //确定连接点
            //如果project 点在 图块obb（外扩一点点）内（线穿过图框）， 穿过框线边的点
            //如果project 点在 图块obb 外 找最近的点
            var bAddedPrevConn = tryDistByDegree(prevConnPt, prevConnProjPt, leftLineTemp, out var preAddedPt);
            var bAddedConn = tryDistByDegree(connPt, connProjPt, rightLineTemp, out var addedPt);

            //生成主polyline
            moveLanePoly.AddVertexAt(moveLanePoly.NumberOfVertices, prevConnPt.ToPoint2d(), 0, 0, 0);

            if (bAddedPrevConn == true)
            {
                moveLanePoly.AddVertexAt(moveLanePoly.NumberOfVertices, preAddedPt.ToPoint2d(), 0, 0, 0);
            }

            if (prevPolyInx < ptPolyInx)
            {
                for (int j = prevPolyInx + 1; j < ptPolyInx + 1; j++)
                {
                    moveLanePoly.AddVertexAt(moveLanePoly.NumberOfVertices, movedline.GetPoint3dAt(j).ToPoint2d(), 0, 0, 0);
                }
            }
            if (prevPolyInx > ptPolyInx)
            {
                for (int j = prevPolyInx; j > ptPolyInx; j--)
                {
                    moveLanePoly.AddVertexAt(moveLanePoly.NumberOfVertices, movedline.GetPoint3dAt(j).ToPoint2d(), 0, 0, 0);
                }
            }
            if (bAddedConn == true)
            {

                moveLanePoly.AddVertexAt(moveLanePoly.NumberOfVertices, addedPt.ToPoint2d(), 0, 0, 0);
            }

            moveLanePoly.AddVertexAt(moveLanePoly.NumberOfVertices, connPt.ToPoint2d(), 0, 0, 0);

            //prevBlk.connInfo[prevConnPt].Add(moveLanePoly);
            //thisBlk.connInfo[connPt].Add(moveLanePoly);

            return moveLanePoly;
        }

        private static Line getMoveLinePart(Point3d PrevPtPrj, Point3d ptPrj, Polyline movedLine, out int prevPolyInx, out int ptPolyInx)
        {
            Line moveLinePart = new Line();
            Tolerance tol = new Tolerance(1, 1);
            prevPolyInx = -1;
            ptPolyInx = -1;

            for (int i = 0; i < movedLine.NumberOfVertices; i++)
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

        public static Polyline cutPolyline(Point3d sp, Point3d ep, Polyline pl)
        {
            Polyline returnPl = new Polyline();
            Point3d spPrj = new Point3d();
            Point3d epPrj = new Point3d();
            Tolerance tol = new Tolerance(1, 1);

            if (sp.DistanceTo(pl.StartPoint) <= ep.DistanceTo(pl.StartPoint))
            {
                spPrj = pl.GetClosestPointTo(sp, true);
                epPrj = pl.GetClosestPointTo(ep, true);
            }
            else
            {
                spPrj = pl.GetClosestPointTo(ep, true);
                epPrj = pl.GetClosestPointTo(sp, true);
            }

            int spInx = -1;
            int epInx = -1;

            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                var lineTemp = pl.GetLineSegmentAt(i);

                if (lineTemp.IsOn(spPrj, tol))
                {
                    spInx = i;
                }
                if (lineTemp.IsOn(epPrj, tol))
                {
                    epInx = i;
                }
                if (spInx != -1 && epInx != -1)
                { break; }
            }

            returnPl.AddVertexAt(returnPl.NumberOfVertices, spPrj.ToPoint2d(), 0, 0, 0);
            for (int i = spInx; i < epInx; i++)
            {
                returnPl.AddVertexAt(returnPl.NumberOfVertices, pl.GetPoint3dAt(i + 1).ToPoint2d(), 0, 0, 0);
            }
            returnPl.AddVertexAt(returnPl.NumberOfVertices, epPrj.ToPoint2d(), 0, 0, 0);

            return returnPl;
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
                //if (bEnd == false && seg.Length <= 500 )

                if (bEnd == false && seg.Length <= EmgConnectCommon.TolTooClosePt)
                {
                    addPt = seg.StartPoint;
                    bAddPt = true;
                    bEnd = true;
                }

                if (bEnd == false)
                {
                    adjacent = opposite / Math.Tan(degree * Math.PI / 180);

                    if (adjacent < seg.Length / 5 && adjacent <= 500 && adjacent >= 100)
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

                if (degree >= 90)
                {
                    degree = 85;

                    adjacent = opposite / Math.Tan(degree * Math.PI / 180);
                    addPt = connPtProj + adjacent * (seg.EndPoint - seg.StartPoint).GetNormal();

                    bAddPt = true;
                    bEnd = true;
                }
            }

            return bAddPt;

        }

        public static Point3d getPrjPt(List<Line> lineList, Point3d pt, out int extendDir)
        {
            var prjPt = new Point3d();
            extendDir = -100;
            var matrix = getLineListMatrix(lineList);
            var lineListTrans = lineList.Select(x => new Line(x.StartPoint.TransformBy(matrix.Inverse()), x.EndPoint.TransformBy(matrix.Inverse()))).ToList();

            var ptTrans = pt.TransformBy(matrix.Inverse());

            if (ptTrans.X < lineListTrans.First().StartPoint.X)
            {
                prjPt = lineList.First().GetClosestPointTo(pt, true);
                extendDir = -1;
            }
            else if (ptTrans.X > lineListTrans.Last().EndPoint.X)
            {
                prjPt = lineList.Last().GetClosestPointTo(pt, true);
                extendDir = lineList.Count;
            }
            else
            {
                for (int i = 0; i < lineListTrans.Count; i++)
                {
                    if (lineListTrans[i].StartPoint.X <= ptTrans.X && ptTrans.X <= lineListTrans[i].EndPoint.X)
                    {
                        prjPt = lineList[i].GetClosestPointTo(pt, false);
                        extendDir = i;
                        break;
                    }
                }
            }

            return prjPt;
        }

        private static Matrix3d getLineListMatrix(List<Line> lineList)

        {
            var dir = (lineList.Last().EndPoint - lineList.First().StartPoint).GetNormal();

            var rotationangle = Vector3d.XAxis.GetAngleTo(dir, Vector3d.ZAxis);
            var matrix = Matrix3d.Displacement(lineList.First().StartPoint.GetAsVector()) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0));
            return matrix;
        }

        public static Polyline CorrectConflictFrame(Polyline frame, Polyline linkTemp, ThBlock blkS, ThBlock blkE, List<Polyline> holes)
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

                if (resCut != null)
                {
                    link = resCut;
                }
            }
            return link;
        }

    }
}
