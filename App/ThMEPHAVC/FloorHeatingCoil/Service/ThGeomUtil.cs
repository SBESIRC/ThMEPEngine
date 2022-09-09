using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using NFox.Cad;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;

namespace ThMEPHVAC.FloorHeatingCoil.Service
{
    public class ThGeomUtil
    {
        public static Polyline GetVisibleOBB(BlockReference blk)
        {
            Polyline obb = null;
            try
            {
                var objs = new DBObjectCollection();
                blk.ExplodeWithVisible(objs);
                var curves = objs.OfType<Entity>()
                    .Where(e => e is Curve).ToCollection();
                curves = Tesslate(curves);
                curves = curves.OfType<Curve>().Where(o => o != null && o.GetLength() > 1e-6).ToCollection();
                obb = curves.GetMinimumRectangle();
            }
            catch (Exception ex)
            {

            }
            return obb;
        }

        public static Polyline GetAABB(BlockReference blk)
        {
            var rectangle = blk.GeometricExtents.ToRectangle();
            //rectangle.TransformBy(blk.BlockTransform);
            return rectangle;
        }

        private static DBObjectCollection Tesslate(DBObjectCollection curves,
  double arcLength = 50.0, double chordHeight = 50.0)
        {
            var results = new DBObjectCollection();
            curves.OfType<Curve>().ToList().ForEach(o =>
            {
                if (o is Line)
                {
                    results.Add(o);
                }
                else if (o is Arc arc)
                {
                    results.Add(arc.TessellateArcWithArc(arcLength));
                }
                else if (o is Circle circle)
                {
                    results.Add(circle.TessellateCircleWithArc(arcLength));
                }
                else if (o is Polyline polyline)
                {
                    results.Add(polyline.TessellatePolylineWithArc(arcLength));
                }
                else if (o is Ellipse ellipse)
                {
                    results.Add(ellipse.Tessellate(chordHeight));
                }
                else if (o is Spline spline)
                {
                    results.Add(spline.Tessellate(chordHeight));
                }
            });
            return results;
        }

        public static int GetNumberInText(DBText text)
        {
            var i = 0;
            if (text != null)
            {
                var s = text.TextString;
                s = new string(s.Where(Char.IsDigit).ToArray());
                i = Convert.ToInt32(s);
            }

            return i;
        }

        public static Polyline ProcessFrame(Polyline frame, int simplifyTol)
        {
            Polyline nFrame = null;
            Polyline nFrameNormal = ThMEPFrameService.Normalize(frame);
            if (nFrameNormal.Area > 10)
            {
                nFrameNormal = nFrameNormal.DPSimplify(simplifyTol);
                nFrame = nFrameNormal;
            }
            return nFrame;
        }

        public static MPolygon ProcessMpoly(MPolygon mFrame, int simplifyTol)
        {
            var shell = mFrame.Shell();
            var holes = mFrame.Holes();
            shell = ProcessFrame(shell, simplifyTol);

            holes = holes.Select(x => ProcessFrame(x, simplifyTol)).ToList();
            var curve = holes.OfType<Curve>().ToList();

            var mFrameR = ThMPolygonTool.CreateMPolygon(shell, curve);
            return mFrameR;
        }
    }
}
