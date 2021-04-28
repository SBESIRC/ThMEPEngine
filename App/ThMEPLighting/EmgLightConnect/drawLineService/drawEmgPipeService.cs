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
using ThMEPLighting.EmgLight.Assistant;
using ThMEPLighting.EmgLightConnect.Model;


namespace ThMEPLighting.EmgLightConnect.Service
{
    public class drawEmgPipeService
    {
        public static Point3d getConnectPt(ThBlock blk, Line lineTemp)
        {
            Point3d connPt = new Point3d();
            Tolerance tol = new Tolerance(1, 1);
            Point3dCollection pts = new Point3dCollection();

            DrawUtils.ShowGeometry(blk.outline, EmgConnectCommon.LayerBlkOutline, Color.FromColorIndex(ColorMethod.ByColor, 40));

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

        private static Point3d ptOnOutlineSidePt(Point3d pt, ThBlock blk)
        {
            Point3d connPt = new Point3d();
            Tolerance tol = new Tolerance(1, 1);

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

        public static Polyline cutLane(Point3d prevP, Point3d pt, ThBlock prevBlk, ThBlock thisBlk, Polyline movedline)
        {
            var prevProjP = movedline.GetClosestPointTo(prevP, true);
            var projP = movedline.GetClosestPointTo(pt, true);


            var leftLineTemp = getMoveLinePart(prevProjP, projP, movedline, out int prevPolyInx, out int ptPolyInx);
            var prevConnPt = drawEmgPipeService.getConnectPt(prevBlk, leftLineTemp);
            var prevConnProjPt = leftLineTemp.GetClosestPointTo(prevConnPt, true);
            var bAddedPrevConn = tryDistByDegree(prevConnPt, prevConnProjPt, leftLineTemp, out var preAddedPt);

            var rightLineTemp = getMoveLinePart(projP, prevProjP, movedline, out int ptPolyInx2, out int prevPolyInx2);
            var connPt = drawEmgPipeService.getConnectPt(thisBlk, rightLineTemp);
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

            prevBlk.connInfo[prevConnPt].Add(moveLanePoly);
            thisBlk.connInfo[connPt].Add(moveLanePoly);

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



        public static List<Polyline> CorrectIntersectLink(List<Polyline> polylines, List<ThBlock> blkList)
        {
            Tolerance tol = new Tolerance(1, 1);
            List<Polyline> resPolys = new List<Polyline>();

            while (polylines.Count > 0)
            {
                var firPoly = polylines.First();
                polylines.Remove(firPoly);
                //找到相交线
                var intersectPolys = polylines
                    .ToDictionary(
                    x => x,
                    x =>
                    {
                        List<Point3d> pts = new List<Point3d>();
                        foreach (Point3d pt in x.IntersectWithEx(firPoly))
                        {
                            if (pt.DistanceTo(x.StartPoint) > 1 && pt.DistanceTo(x.EndPoint) > 1)
                            {
                                pts.Add(pt);
                            }
                        }
                        return pts;
                    })
                    .Where(x => x.Value.Count > 0)
                    .ToDictionary(x => x.Key, y => y.Value.First());

                if (intersectPolys.Count > 0)
                {
                    var intersectPoly = intersectPolys.First();
                    var intersectPt = intersectPoly.Value;
                    polylines.Remove(intersectPoly.Key);
                    var secPoly = intersectPoly.Key;

                    correctLink(intersectPt, blkList, ref firPoly, ref secPoly);

                    resPolys.Add(secPoly);
                }


                resPolys.Add(firPoly);
            }

            return resPolys;
        }

        private static void correctLink(Point3d intersectPt, List<ThBlock> blkList, ref Polyline firPoly, ref Polyline secPoly)
        {
            var firSBlk = GetBlockService.getBlockByConnect(firPoly.StartPoint, blkList);
            var firEBlk = GetBlockService.getBlockByConnect(firPoly.EndPoint, blkList);
            var secSBlk = GetBlockService.getBlockByConnect(secPoly.StartPoint, blkList);
            var secEBlk = GetBlockService.getBlockByConnect(secPoly.EndPoint, blkList);

            if (firSBlk != secSBlk && firSBlk != secEBlk && firEBlk != secSBlk && firEBlk != secEBlk)
            {

            }
            else
            {
                if (firPoly.StartPoint == secPoly.StartPoint || firPoly.StartPoint == secPoly.EndPoint)
                {
                    adjustSamePoint(intersectPt, firPoly.StartPoint, ref firPoly, ref secPoly);
                }
                else if (firPoly.EndPoint == secPoly.StartPoint || firPoly.EndPoint == secPoly.EndPoint)
                {
                    adjustSamePoint(intersectPt, firPoly.EndPoint, ref firPoly, ref secPoly);
                }
                else
                {
                    adjustSameBlock(intersectPt, ref firPoly, ref secPoly);
                }
            }
        }

        private static void adjustSamePoint(Point3d intersectPt, Point3d samePoint, ref Polyline firPoly, ref Polyline secPoly)
        {
            var tol = new Tolerance(1, 1);
            var firPolyNew = firPoly.Clone() as Polyline;
            var secPolyNew = secPoly.Clone() as Polyline;
            bool firRev = false;
            bool secRev = false;

            if (firPoly.EndPoint.IsEqualTo(samePoint, tol))
            {
                firPolyNew.ReverseCurve();
                firRev = true;
            }
            if (secPoly.EndPoint.IsEqualTo(samePoint, tol))
            {
                secPolyNew.ReverseCurve();
                secRev = true;
            }

            //找到哪条线转 intersectPt 是不是在第一条线后
            int firInterIdx = getSeg(intersectPt, firPolyNew);
            int secInterIdx = getSeg(intersectPt, secPolyNew);

            if (firInterIdx > 0)
            {
                moveSegPoint(intersectPt, firInterIdx, ref firPolyNew);
            }
            else if (secInterIdx > 0)
            {
                moveSegPoint(intersectPt, secInterIdx, ref secPolyNew);
            }

            //转回来
            if (firRev == true)
            {
                firPolyNew.ReverseCurve();
            }
            if (secRev == true)
            {
                secPolyNew.ReverseCurve();
            }

            firPoly = firPolyNew;
            secPoly = secPolyNew;
        }

        private static void adjustSameBlock(Point3d intersectPt, ref Polyline firPoly, ref Polyline secPoly)
        {
            var firPolyNew = firPoly.Clone() as Polyline;
            var secPolyNew = secPoly.Clone() as Polyline;
            bool firRev = false;
            bool secRev = false;

            if (firPoly.EndPoint.DistanceTo(intersectPt) < firPoly.StartPoint.DistanceTo(intersectPt))
            {
                firPolyNew.ReverseCurve();
                firRev = true;
            }
            if (secPoly.EndPoint.DistanceTo(intersectPt) < secPoly.StartPoint.DistanceTo(intersectPt))
            {
                secPolyNew.ReverseCurve();
                secRev = true;
            }

            //交换两条线第一个点
            int firInterIdx = getSeg(intersectPt, firPolyNew);
            int secInterIdx = getSeg(intersectPt, secPolyNew);


            if (firInterIdx > 0)
            {
                changeSegSPt(intersectPt, firInterIdx, ref firPolyNew, ref secPolyNew);

            }
            else if (secInterIdx > 0)
            {
                changeSegSPt(intersectPt, secInterIdx, ref secPolyNew, ref firPolyNew);
            }


            //转回来
            if (firRev == true)
            {
                firPolyNew.ReverseCurve();
            }
            if (secRev == true)
            {
                secPolyNew.ReverseCurve();
            }

            firPoly = firPolyNew;
            secPoly = secPolyNew;
        }

        private static void moveSegPoint(Point3d intersectPt, int idx, ref Polyline pl)
        {
            var seg = new Line(pl.GetPoint3dAt(idx), intersectPt);
            var dir = (seg.EndPoint - seg.StartPoint).GetNormal();
            Point3d newEndPoint = seg.EndPoint + dir * 100;
            pl.SetPointAt(idx, newEndPoint.ToPoint2d());
        }

        private static void changeSegSPt(Point3d intersectPt, int idx, ref Polyline firPoly, ref Polyline secPoly)
        {
            var newSecS = firPoly.StartPoint;
            var newFirS = secPoly.StartPoint;

            secPoly.SetPointAt(0, newSecS.ToPoint2d());
            firPoly.SetPointAt(0, newFirS.ToPoint2d());
            firPoly.SetPointAt(idx, intersectPt.ToPoint2d());

        }

        private static int getSeg(Point3d intersecPt, Polyline polyline)
        {
            var tol = new Tolerance(1, 1);
            var idx = -1;
            var closetPt = polyline.GetClosestPointTo(intersecPt, false);

            for (int i = 0; i < polyline.NumberOfVertices-1; i++)
            {
                var seg = new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices));
                if (seg.ToCurve3d().IsOn(closetPt, tol))

                {
                    idx = i;
                    break;
                }
            }
            return idx;
        }

    }
}
