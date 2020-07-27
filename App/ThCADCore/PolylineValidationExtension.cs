using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore
{
    public enum PolygonValidateResult
    {
        OK = 0,
        NotClosed = 1,
        DuplicateVertices = 2,
        SelfIntersection = 4,
    }

    /// <summary>
    /// 多段线验证
    /// </summary>
    /// http://drive-cad-with-code.blogspot.com/2017/10/validate-polyline-lighweightpolyline-as.html
    public static class PolylineValidationExtension
    {
        private const double TOLERANCE = 0.001;

        public static PolygonValidateResult IsValidPolygon(this Polyline pline)
        {
#if ACAD_ABOVE_2012
            var result = PolygonValidateResult.OK;

            if (!pline.Closed)
            {
                result += 1;
            }

            var t = new Tolerance(TOLERANCE, TOLERANCE);
            using (var curve1 = pline.GetGeCurve(t))
            {
                using (var curve2 = pline.GetGeCurve(t))
                {
                    using (var curveInter = new CurveCurveIntersector3d(
                        curve1, curve2, pline.Normal, t))
                    {
                        int interCount = curveInter.NumberOfIntersectionPoints;
                        int overlaps = curveInter.OverlapCount();
                        if (!pline.Closed) overlaps += 1;

                        if (overlaps < pline.NumberOfVertices)
                        {
                            result += 2;
                        }

                        if (interCount > overlaps)
                        {
                            result += 4;
                        }
                    }
                }
            }

            return result;
#else
            return PolygonValidateResult.OK;
#endif
        }

        public static PolygonValidateResult IsValidPolygon(this ObjectId polyId)
        {
            var result = PolygonValidateResult.OK;

            if (polyId.ObjectClass.DxfName.ToUpper() != "LWPOLYLINE")
            {
                throw new ArgumentException("Not a Lightweight Polyline!");
            }

            using (var tran = polyId.Database.TransactionManager.StartTransaction())
            {
                var poly = (Polyline)tran.GetObject(polyId, OpenMode.ForRead);
                result = poly.IsValidPolygon();
                tran.Commit();
            }

            return result;
        }

        public static string ToResultString(this PolygonValidateResult res)
        {
            string msg = "";
            if (res == PolygonValidateResult.OK)
            {
                msg = "valid polyline.";
            }
            else
            {
                if ((res & PolygonValidateResult.NotClosed) == PolygonValidateResult.NotClosed)
                {
                    msg = msg + "Polyline is not closed";
                }

                if ((res & PolygonValidateResult.DuplicateVertices) == PolygonValidateResult.DuplicateVertices)
                {
                    if (msg.Length > 0) msg = msg + "; ";
                    msg = msg + "Polyline has duplicate vertices";
                }

                if ((res & PolygonValidateResult.SelfIntersection) == PolygonValidateResult.SelfIntersection)
                {
                    if (msg.Length > 0) msg = msg + "; ";
                    msg = msg + "Polyline is self-intersecting";
                }
            }

            return msg;
        }
    }
}
