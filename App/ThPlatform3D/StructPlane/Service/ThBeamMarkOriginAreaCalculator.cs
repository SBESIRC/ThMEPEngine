using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO.SVG;

namespace ThPlatform3D.StructPlane.Service
{
    public class ThBeamMarkOriginAreaCalculator
    {
        public static Dictionary<DBText,Tuple<Point3dCollection,Vector3d>> Calculate(List<ThGeometry> beamMarkGeos,double edgeExtendLength)
        {
            /*
             *  --------------------------------------
             *                200 x 400 
             *  --------------------------------------
             *                    |
             *                    |（textMoveDir）
             *                    |                     
             */           
            var results = new Dictionary<DBText, Tuple<Point3dCollection, Vector3d>>();  // 记录每个文字所产生的区域
            beamMarkGeos.ForEach(o =>
            {
                if(o.Boundary is DBText text)
                {
                    var textMoveDir = new Vector3d(); // 文字移动方向
                    if (o.Properties.ContainsKey(ThSvgPropertyNameManager.DirPropertyName))
                    {
                        textMoveDir = o.Properties.GetDirection().ToVector();
                    }
                    var dir = textMoveDir.GetPerpendicularVector().GetNormal();
                    var values = text.TextString.GetDoubles();
                    if (values.Count > 0)
                    {
                        var areaPts = CreateOrginArea(text.AlignmentPoint, dir, values[0] * 2.0, values[0]);
                        results.Add(text, Tuple.Create(areaPts, textMoveDir));
                    }
                }
            });
            return results;
        }

        private static Point3dCollection CreateOrginArea(Point3d center, Vector3d dirUnit, double length ,double width)
        {
            var areaPts = new Point3dCollection();
            var perpendUnit = dirUnit.GetPerpendicularVector();
            var lPt = center - dirUnit.MultiplyBy(length / 2.0);
            var rPt = center + dirUnit.MultiplyBy(length / 2.0);
            areaPts.Add(lPt + perpendUnit.MultiplyBy(width / 2.0));
            areaPts.Add(rPt + perpendUnit.MultiplyBy(width / 2.0));
            areaPts.Add(rPt - perpendUnit.MultiplyBy(width / 2.0));
            areaPts.Add(lPt - perpendUnit.MultiplyBy(width / 2.0));
            return areaPts;
        }
    }
}
