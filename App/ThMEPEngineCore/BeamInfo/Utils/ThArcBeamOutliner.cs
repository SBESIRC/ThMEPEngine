using System;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.BeamInfo.Utils
{
    public class ThArcBeamOutliner
    {
        private static double BulgeFromCurve(Curve cv, bool clockwise)
        {
            double bulge = 0.0;
            Arc a = cv as Arc;
            if (a != null)
            {
                double newStart;
                // The start angle is usually greater than the end,
                // as arcs are all counter-clockwise.
                // (If it isn't it's because the arc crosses the
                // 0-degree line, and we can subtract 2PI from the
                // start angle.)
                if (a.StartAngle > a.EndAngle)
                    newStart = a.StartAngle - 8 * Math.Atan(1);
                else
                    newStart = a.StartAngle;

                // Bulge is defined as the tan of
                // one fourth of the included angle
                bulge = Math.Tan((a.EndAngle - newStart) / 4);
                // If the curve is clockwise, we negate the bulge
                if (clockwise)
                    bulge = -bulge;

            }
            return bulge;
        }

        public static Polyline Outline(Arc arc1, Arc arc2)
        {
            //提取包络两段arc的Angle范围
            double startAngle = Math.Min(arc1.StartAngle, arc2.StartAngle);
            double endAngle = Math.Max(arc1.EndAngle, arc2.EndAngle);

            //将输入的两段arc转换为PolylineSegment
            Arc arc_1 = new Arc(arc1.Center, arc1.Radius, startAngle, endAngle);
            Arc arc_2 = new Arc(arc2.Center, arc2.Radius, startAngle, endAngle);
            double arcBulge1 = BulgeFromCurve(arc_1, false);
            double arcBulge2 = BulgeFromCurve(arc_2, false);
            PolylineSegment arcSegment_1 = new PolylineSegment(arc_1.StartPoint.ToPoint2D(), arc_1.EndPoint.ToPoint2D(), arcBulge1);
            PolylineSegment arcSegment_2 = new PolylineSegment(arc_2.EndPoint.ToPoint2D(), arc_2.StartPoint.ToPoint2D(), -arcBulge2);

            // 获取两段新的arc的端点形成两段PolylineSegement
            PolylineSegment lineSegment_1 = new PolylineSegment(arcSegment_1.EndPoint, arcSegment_2.StartPoint);
            PolylineSegment lineSegment_2 = new PolylineSegment(arcSegment_2.EndPoint, arcSegment_1.StartPoint);

            // 合并所有得到的PolylineSegment
            var segmentCollection = new PolylineSegmentCollection()
            {
                arcSegment_1,
                lineSegment_1,
                arcSegment_2,
                lineSegment_2
            };

            // 返回多段线
            return segmentCollection.ToPolyline();
        }
    }
}
