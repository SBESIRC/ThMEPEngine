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


    }
}
