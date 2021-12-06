using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    internal class ThArcDrawTool
    {
        public const double ArcAngle = 45.0; // 灯两点间弧的角度
        public static double CalculateRadiusByGap(double lightDis, double gap)
        {
            /*         
             *             |
             *             | gap
             *      ---------------(此段是lightDis)
             *       \     | h   /
             *        \    |    /
             *         \   |   / radius
             *          \  |  /
             *           \ | /
             *            \|/
             */
            // 用勾股定理计算半径
            var half = lightDis / 2.0;
            if (gap < half)
            {
                return (half * half + gap * gap) / (2 * gap);
            }
            else
            {
                // ratio默认为 0.5, h=ratio*radius
                double ratio = 0.5;
                return Math.Sqrt(half * half / (1 - ratio * ratio));
            }
        }
        public static double CalculateRadiusByAngle(double lightDis, double angle)
        {
            /*         
             *             |
             *          a  | 
             *      ---------------
             *       \     |     /
             *        \    |    /
             *       r \   |   / r
             *          \  |  /
             *           \ | /
             *            \|/
             */
            // angle是两条半径的夹角
            // 用正弦定理
            double a = lightDis / 2.0;
            double rad = (angle / 2.0).AngToRad();
            double sine = Math.Sin(rad);
            return a / sine;
        }
        public static Arc DrawArc(Point3d startPt, Point3d endPt, double radius, Vector3d arcTopVec)
        {
            var dis = startPt.DistanceTo(endPt);
            if (dis <= 1e-6 || radius <= 1e-6 || arcTopVec.Length <= 1e-6)
            {
                return new Arc();
            }
            var perpendVec = startPt.GetVectorTo(endPt).GetPerpendicularVector().GetNormal();
            var arcVec = perpendVec;
            if (!perpendVec.IsSameDirection(arcTopVec))
            {
                arcVec = perpendVec.Negate();
            }
            var midPt = startPt.GetMidPt(endPt);
            var h = Math.Sqrt(radius * radius - dis / 2.0 * dis / 2.0);
            Point3d topPt = midPt + arcVec.MultiplyBy(radius - h);
            var center = midPt + arcVec.Negate().MultiplyBy(h);
            var startVec = center.GetVectorTo(startPt);
            var endVec = center.GetVectorTo(endPt);
            var startAng = new Line(center, startPt).Angle;
            var endAng = new Line(center, endPt).Angle;
            if (startVec.IsAntiClockwise(arcVec) && arcVec.IsAntiClockwise(endVec))
            {
                return new Arc(center, radius, startAng, endAng);
            }
            else
            {
                return new Arc(center, radius, endAng, startAng);
            }
        }
        public static Vector3d CalculateArcTopVec(Point3d first, Point3d second, Vector3d referVec)
        {
            var vec = first.GetVectorTo(second);
            var perpendVec = vec.GetPerpendicularVector();
            if (perpendVec.DotProduct(referVec) > 0.0)
            {
                return perpendVec;
            }
            else
            {
                return perpendVec.Negate();
            }
        }
        /// <summary>
        /// 参照CadDBText获取方向
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="ep"></param>
        /// <returns></returns>
        public static Vector3d GetArcTopVector(Point3d sp, Point3d ep)
        {
            return sp.GetVectorTo(ep).GetNormal().GetAlignedDimensionTextDir();
        }
        /// <summary>
        /// 根据outerPt计算方向
        /// </summary>
        /// <param name="startPt"></param>
        /// <param name="endPt"></param>
        /// <param name="outerPt"></param>
        /// <returns></returns>
        public static Vector3d CalculateReferenceVec(Point3d startPt, Point3d endPt, Point3d outerPt)
        {
            var projectionPt = outerPt.GetProjectPtOnLine(startPt, endPt);
            return projectionPt.GetVectorTo(outerPt);
        }
    }
}
