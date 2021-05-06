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
   public class drawCorrectLinkService
    {
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
                    var secPoly = new Polyline();
                    for (int i = 0; i < intersectPolys.Count; i++)
                    {
                        var intersectPoly = intersectPolys.ElementAt(i);
                        var intersectPt = intersectPoly.Value;
                        polylines.Remove(intersectPoly.Key);
                        secPoly = intersectPoly.Key;
                        correctLink(intersectPt, blkList, ref firPoly, ref secPoly);
                    }
                    resPolys.Add(secPoly);
                }
                resPolys.Add(firPoly);
            }

            return resPolys;
        }

        private static void correctLink(Point3d intersectPt, List<ThBlock> blkList, ref Polyline firPoly, ref Polyline secPoly)
        {
            var firSBlk = BlockListService.getBlockByConnect(firPoly.StartPoint, blkList);
            var firEBlk = BlockListService.getBlockByConnect(firPoly.EndPoint, blkList);
            var secSBlk = BlockListService.getBlockByConnect(secPoly.StartPoint, blkList);
            var secEBlk = BlockListService.getBlockByConnect(secPoly.EndPoint, blkList);

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
            Point3d newEndPoint = intersectPt + dir * 100;
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
