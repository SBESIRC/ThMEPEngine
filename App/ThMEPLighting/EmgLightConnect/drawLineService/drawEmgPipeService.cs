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

namespace ThMEPLighting.EmgLightConnect.Service
{
    public class drawEmgPipeService
    {
        public static Point3d getConnectPt(Point3d pt, Line lineTemp, List<ThBlock> blkList)
        {
            var blk = GetBlockService.getBlock(pt, blkList);
            var connPt = getConnectPt(blk, lineTemp, blkList);
            return connPt;
        }

        public static Point3d getConnectPt(ThBlock blk, Line lineTemp, List<ThBlock> blkList)
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


    }
}
