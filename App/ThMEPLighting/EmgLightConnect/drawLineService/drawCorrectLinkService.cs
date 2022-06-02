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
using ThMEPEngineCore.Algorithm.AStarAlgorithm;
using ThMEPLighting.EmgLightConnect.Model;

namespace ThMEPLighting.EmgLightConnect.Service
{
    public class drawCorrectLinkService
    {
        public static List<Polyline> CorrectIntersectLink(List<Polyline> polylines, List<ThBlock> blkList)
        {
            Tolerance tol = new Tolerance(20, 20);
            int tolPt = 20;
            List<Polyline> resPolys = new List<Polyline>();

            while (polylines.Count > 0)
            {
                var firPoly = polylines.First();
                polylines.Remove(firPoly);
                //找到相交线
                //var intersectPolys = polylines
                //    .ToDictionary(
                //    x => x,
                //    x =>
                //    {
                //        //相交
                //        List<Point3d> pts = new List<Point3d>();
                //        foreach (Point3d pt in x.IntersectWithEx(firPoly))
                //        {
                //            if (pt.DistanceTo(x.StartPoint) > 1 && pt.DistanceTo(x.EndPoint) > 1)
                //            {
                //                pts.Add(pt);
                //            }
                //        }
                //        return pts;
                //    })
                //    .Where(x => x.Value.Count > 0)
                //    .ToDictionary(x => x.Key, y => y.Value.First());

                var intersectPts = new Dictionary<Polyline, List<Point3d>>();
                foreach (var poly in polylines)
                {
                    List<Point3d> pts = new List<Point3d>();
                    foreach (Point3d pt in poly.IntersectWithEx(firPoly))
                    {
                        if (pt.DistanceTo(poly.StartPoint) > tolPt && pt.DistanceTo(poly.EndPoint) > tolPt)
                        {
                            pts.Add(pt);
                        }
                    }

                    //视觉重合
                    var ptsTemp = new List<Point3d>();
                    for (int i = 0; i < firPoly.NumberOfVertices - 1; i++)
                    {
                        for (int j = 0; j < poly.NumberOfVertices - 1; j++)
                        {
                            var pt = firPoly.GetPoint3dAt(i);
                            var seg = new Line(poly.GetPoint3dAt(j), poly.GetPoint3dAt(j + 1));
                            if (pt.IsPointOnLine(seg, tolPt))
                            {
                                ptsTemp.Add(pt);
                            }

                            var pt2 = poly.GetPoint3dAt(j);
                            var seg2 = new Line(firPoly.GetPoint3dAt(i), firPoly.GetPoint3dAt(i + 1));
                            if (pt2.IsPointOnLine(seg2, tolPt))
                            {
                                ptsTemp.Add(pt2);
                            }
                        }
                    }
                    foreach (var pt in ptsTemp)
                    {
                        if (pt.DistanceTo(poly.StartPoint) > tolPt && pt.DistanceTo(poly.EndPoint) > tolPt
                            && pt.DistanceTo(firPoly.StartPoint) > tolPt && pt.DistanceTo(firPoly.EndPoint) > tolPt)
                        {
                            var alreadyIn = pts.Where(x => x.DistanceTo(pt) <= tolPt);
                            if (alreadyIn.Count() == 0)
                            {
                                pts.Add(pt);
                            }
                        }
                    }

                    intersectPts.Add(poly, pts);
                }

              var  intersectPolys = intersectPts.Where(x => x.Value.Count > 0)
                                            .ToDictionary(x => x.Key, y => y.Value.First());


                //if (intersectPolys.Count ==1 )
                if (intersectPolys.Count > 0)
                {
                    var secPoly = new Polyline();
                    for (int i = 0; i < intersectPolys.Count; i++)
                    {
                        var intersectPoly = intersectPolys.ElementAt(i);
                        var intersectPt = intersectPoly.Value;
                        polylines.Remove(intersectPoly.Key);
                        secPoly = intersectPoly.Key;
                        correctLink(intersectPt, blkList, ref firPoly, ref secPoly);
                        polylines.Add(secPoly);
                        //resPolys.Add(secPoly);
                    }

                }
                resPolys.Add(firPoly);
            }

            return resPolys;
        }

        private static void correctLink(Point3d intersectPt, List<ThBlock> blkList, ref Polyline firPoly, ref Polyline secPoly)
        {
            var samePt = findStPt(firPoly, secPoly, blkList);

            if (samePt == Point3d.Origin)
            {
                //两条相交线没有同一点
            }
            else
            {
                var firRev = reversePl(firPoly, samePt, out var firPolyNew);
                var secRev = reversePl(secPoly, samePt, out var secPolyNew);

                if (firPoly.StartPoint == secPoly.StartPoint || firPoly.StartPoint == secPoly.EndPoint ||
                    firPoly.EndPoint == secPoly.StartPoint || firPoly.EndPoint == secPoly.EndPoint)
                {
                    adjustSamePoint(intersectPt, ref firPolyNew, ref secPolyNew);
                }
                else
                {
                    adjustSameBlock(intersectPt, ref firPolyNew, ref secPolyNew);
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
        }

        private static Point3d findStPt(Polyline firPoly, Polyline secPoly, List<ThBlock> blkList)
        {
            var firSBlk = BlockListService.getBlockByConnect(firPoly.StartPoint, blkList);
            var firEBlk = BlockListService.getBlockByConnect(firPoly.EndPoint, blkList);
            var secSBlk = BlockListService.getBlockByConnect(secPoly.StartPoint, blkList);
            var secEBlk = BlockListService.getBlockByConnect(secPoly.EndPoint, blkList);

            Point3d sameBlkCenterPt = new Point3d();

            if (firSBlk == secSBlk || firSBlk == secEBlk)
            {
                sameBlkCenterPt = firSBlk.blkCenPt;
            }
            if (firEBlk == secSBlk || firEBlk == secEBlk)
            {
                sameBlkCenterPt = firEBlk.blkCenPt;
            }

            return sameBlkCenterPt;
        }

        private static bool reversePl(Polyline pl, Point3d startPt, out Polyline reversePl)
        {
            reversePl = pl.Clone() as Polyline;
            bool bReverse = false;

            if (pl.EndPoint.DistanceTo(startPt) < pl.StartPoint.DistanceTo(startPt))
            {
                reversePl.ReverseCurve();
                bReverse = true;
            }

            return bReverse;
        }

        private static void adjustSamePoint(Point3d intersectPt, ref Polyline firPolyNew, ref Polyline secPolyNew)
        {
            //找到哪条线转 intersectPt 是不是在第一条线后
            int firInterIdx = getSeg(intersectPt, firPolyNew);
            int secInterIdx = getSeg(intersectPt, secPolyNew);

            if (firInterIdx > 0)
            {
                moveSegPoint(intersectPt, firInterIdx, ref firPolyNew);
            }
            if (secInterIdx > 0)
            {
                moveSegPoint(intersectPt, secInterIdx, ref secPolyNew);
            }

        }

        private static void adjustSameBlock(Point3d intersectPt, ref Polyline firPolyNew, ref Polyline secPolyNew)
        {
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

        }

        private static void moveSegPoint(Point3d intersectPt, int idx, ref Polyline pl)
        {
            var tol = 300;
            var seg = new Line();
            if (pl.GetPoint3dAt(idx).IsEqualTo(intersectPt, new Tolerance(1, 1)))
            {
                seg.StartPoint = intersectPt;
                seg.EndPoint = pl.GetPoint3dAt(idx + 1);
            }
            else
            {
                seg.StartPoint = pl.GetPoint3dAt(idx);
                seg.EndPoint = intersectPt;
            }


            var dir = (seg.EndPoint - seg.StartPoint).GetNormal();
            Point3d newEndPoint = intersectPt + dir * tol;
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
            var tol = new Tolerance(20, 20);
            var idx = -1;
            var closetPt = polyline.GetClosestPointTo(intersecPt, false);

            for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
            {
                var seg = new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices));
                if (closetPt.IsEqualTo(seg.EndPoint, tol))
                {
                    idx = i + 1;
                    break;
                }
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
