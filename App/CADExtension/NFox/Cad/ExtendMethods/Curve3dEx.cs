﻿using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace NFox.Cad
{
    /// <summary>
    /// 三维解析类曲线转换为三维实体曲线扩展类
    /// </summary>
    public static class Curve3dEx
    {
        /// <summary>
        /// 判断两个浮点数是否相等
        /// </summary>
        /// <param name="tol">容差</param>
        /// <param name="d1">第一个数</param>
        /// <param name="d2">第二个数</param>
        /// <returns>两个数的差值的绝对值小于容差返回 <see langword="true"/>,反之返回 <see langword="false"/></returns>
        public static bool IsEqualPoint(this Tolerance tol, double d1, double d2)
        {
            return Math.Abs(d1 - d2) < tol.EqualPoint;
        }

        #region Curve3d

        /// <summary>
        /// 获取三维解析类曲线(自交曲线)的交点参数
        /// </summary>
        /// <param name="c3d">三维解析类曲线</param>
        /// <returns>曲线参数的列表</returns>
        public static List<double> GetParamsAtIntersectionPoints(this Curve3d c3d)
        {
            CurveCurveIntersector3d cci = new CurveCurveIntersector3d(c3d, c3d, Vector3d.ZAxis);
            List<double> pars = new List<double>();
            for (int i = 0; i < cci.NumberOfIntersectionPoints; i++)
            {
                pars.AddRange(cci.GetIntersectionParameters(i));
            }
            pars.Sort();
            return pars;
        }

        /// <summary>
        /// 获取三维解析类子曲线
        /// </summary>
        /// <param name="curve">三维解析类曲线</param>
        /// <param name="from">子段曲线起点参数</param>
        /// <param name="to">子段曲线终点参数</param>
        /// <returns>三维解析类曲线</returns>
        public static Curve3d GetSubCurve(this Curve3d curve, double from, double to)
        {
            Interval inter = curve.GetInterval();
            bool atStart = Tolerance.Global.IsEqualPoint(inter.LowerBound, from);
            bool atEnd = Tolerance.Global.IsEqualPoint(inter.UpperBound, to);
            if (atStart && atEnd)
                return (Curve3d)curve.Clone();
            if (curve is NurbCurve3d)
            {
                if (from < to)
                {
                    NurbCurve3d clone = (NurbCurve3d)curve.Clone();
                    if (atStart || atEnd)
                    {
                        clone.HardTrimByParams(from, to);
                        return clone;
                    }
                    else
                    {
                        clone.HardTrimByParams(inter.LowerBound, to);
                        clone.HardTrimByParams(from, to);
                        return clone;
                    }
                }
                else
                {
                    NurbCurve3d clone1 = (NurbCurve3d)curve.Clone();
                    clone1.HardTrimByParams(from, inter.UpperBound);
                    NurbCurve3d clone2 = (NurbCurve3d)curve.Clone();
                    clone2.HardTrimByParams(inter.LowerBound, to);
                    clone1.JoinWith(clone2);
                    return clone1;
                }
            }
            else
            {
                Curve3d clone = (Curve3d)curve.Clone();
                clone.SetInterval(new Interval(from, to, Tolerance.Global.EqualPoint));
                return clone;
            }
        }

        /// <summary>
        /// 将三维解析类曲线转换为三维实体类曲线
        /// </summary>
        /// <param name="curve">三维解析类曲线</param>
        /// <returns>三维实体类曲线</returns>
        public static Curve ToCurve(this Curve3d curve)
        {
            if (curve is CompositeCurve3d)
            {
                return ToCurve((CompositeCurve3d)curve);
            }
            else if (curve is LineSegment3d)
            {
                return ToCurve((LineSegment3d)curve);
            }
            else if (curve is EllipticalArc3d)
            {
                return ToCurve((EllipticalArc3d)curve);
            }
            else if (curve is CircularArc3d)
            {
                return ToCurve((CircularArc3d)curve);
            }
            else if (curve is NurbCurve3d)
            {
                return ToCurve((NurbCurve3d)curve);
            }
            else if (curve is PolylineCurve3d)
            {
                return ToCurve((PolylineCurve3d)curve);
            }
            else if (curve is Line3d)
            {
                return ToCurve((Line3d)curve);
            }
            return null;
        }

        /// <summary>
        /// 将三维解析类曲线转换为三维解析类Nurb曲线
        /// </summary>
        /// <param name="curve">三维解析类曲线</param>
        /// <returns>三维解析类Nurb曲线</returns>
        public static NurbCurve3d ToNurbCurve3d(this Curve3d curve)
        {
            if (curve is LineSegment3d)
            {
                return new NurbCurve3d((LineSegment3d)curve);
            }
            else if (curve is EllipticalArc3d)
            {
                return new NurbCurve3d((EllipticalArc3d)curve);
            }
            else if (curve is CircularArc3d)
            {
                return new NurbCurve3d(ToEllipticalArc3d((CircularArc3d)curve));
            }
            else if (curve is NurbCurve3d)
            {
                return (NurbCurve3d)curve;
            }
            else if (curve is PolylineCurve3d)
            {
                return new NurbCurve3d(3, (PolylineCurve3d)curve, false);
            }

            return null;
        }

        #endregion Curve3d

        #region CompositeCurve3d

        /// <summary>
        /// 判断是否为圆和椭圆
        /// </summary>
        /// <param name="curve">三维解析类曲线</param>
        /// <returns>完整圆及完整的椭圆返回 <see langword="true"/>,反之返回 <see langword="false"/></returns>
        public static bool IsCircular(this Curve3d curve)
        {
            if (curve is CircularArc3d || curve is EllipticalArc3d)
            {
                return curve.IsClosed();
            }
            return false;
        }

        /// <summary>
        /// 将三维复合曲线按曲线参数分割
        /// </summary>
        /// <param name="c3d">三维复合曲线</param>
        /// <param name="pars">曲线参数列表</param>
        /// <returns>三维复合曲线列表</returns>
        public static List<CompositeCurve3d> GetSplitCurves(this CompositeCurve3d c3d, List<double> pars)
        {
            Interval inter = c3d.GetInterval();
            Curve3d[] c3ds = c3d.GetCurves();
            pars.Sort();
            for (int i = pars.Count - 1; i > 0; i--)
            {
                if (Tolerance.Global.IsEqualPoint(pars[i], pars[i - 1]))
                    pars.RemoveAt(i);
            }

            if (pars.Count == 0)
                return new List<CompositeCurve3d>();
            if (c3ds.Length == 1 && c3ds[0].IsClosed())
            {
                //闭合曲线不允许打断于一点
                if (pars.Count > 1)
                {
                    //如果包含起点
                    if (Tolerance.Global.IsEqualPoint(pars[0], inter.LowerBound))
                    {
                        pars[0] = inter.LowerBound;
                        //又包含终点，去除终点
                        if (Tolerance.Global.IsEqualPoint(pars[pars.Count - 1], inter.UpperBound))
                        {
                            pars.RemoveAt(pars.Count - 1);
                            if (pars.Count == 1)
                                return new List<CompositeCurve3d>();
                        }
                    }
                    else if (Tolerance.Global.IsEqualPoint(pars[pars.Count - 1], inter.UpperBound))
                    {
                        pars[pars.Count - 1] = inter.UpperBound;
                    }
                    //加入第一点以支持反向打断
                    pars.Add(pars[0]);
                }
                else
                {
                    return new List<CompositeCurve3d>();
                }
            }
            else
            {
                //非闭合曲线加入起点和终点
                if (Tolerance.Global.IsEqualPoint(pars[0], inter.LowerBound))
                    pars[0] = inter.LowerBound;
                else
                    pars.Insert(0, inter.LowerBound);
                if (Tolerance.Global.IsEqualPoint(pars[pars.Count - 1], inter.UpperBound))
                    pars[pars.Count - 1] = inter.UpperBound;
                else
                    pars.Add(inter.UpperBound);
            }

            List<CompositeCurve3d> curves = new List<CompositeCurve3d>();
            for (int i = 0; i < pars.Count - 1; i++)
            {
                List<Curve3d> cc3ds = new List<Curve3d>();
                //复合曲线参数转换到包含曲线参数
                CompositeParameter cp1 = c3d.GlobalToLocalParameter(pars[i]);
                CompositeParameter cp2 = c3d.GlobalToLocalParameter(pars[i + 1]);
                if (cp1.SegmentIndex == cp2.SegmentIndex)
                {
                    cc3ds.Add(
                        c3ds[cp1.SegmentIndex].GetSubCurve(
                            cp1.LocalParameter,
                            cp2.LocalParameter));
                }
                else
                {
                    inter = c3ds[cp1.SegmentIndex].GetInterval();
                    cc3ds.Add(
                        c3ds[cp1.SegmentIndex].GetSubCurve(
                            cp1.LocalParameter,
                            inter.UpperBound));
                    for (int j = cp1.SegmentIndex + 1; j < cp2.SegmentIndex; j++)
                    {
                        cc3ds.Add((Curve3d)c3ds[j].Clone());
                    }
                    inter = c3ds[cp2.SegmentIndex].GetInterval();
                    cc3ds.Add(
                        c3ds[cp2.SegmentIndex].GetSubCurve(
                            inter.LowerBound,
                            cp2.LocalParameter));
                }
                curves.Add(new CompositeCurve3d(cc3ds.ToArray()));
            }

            if (c3d.IsClosed() && c3ds.Length > 1)
            {
                var cs = curves[curves.Count - 1].GetCurves().ToList();
                cs.AddRange(curves[0].GetCurves());
                curves[curves.Count - 1] = new CompositeCurve3d(cs.ToArray());
                curves.RemoveAt(0);
            }

            return curves;
        }

        /// <summary>
        /// 将复合曲线转换为实体类曲线
        /// </summary>
        /// <param name="curve">三维复合曲线</param>
        /// <returns>实体曲线</returns>
        public static Curve ToCurve(this CompositeCurve3d curve)
        {
            Curve3d[] cs = curve.GetCurves();
            if (cs.Length == 0)
            {
                return null;
            }
            else if (cs.Length == 1)
            {
                return ToCurve(cs[0]);
            }
            else
            {
                bool hasNurb = false;

                foreach (var c in cs)
                {
                    if (c is NurbCurve3d || c is EllipticalArc3d)
                    {
                        hasNurb = true;
                        break;
                    }
                }
                if (hasNurb)
                {
                    NurbCurve3d nc3d = cs[0].ToNurbCurve3d();
                    for (int i = 1; i < cs.Length; i++)
                        nc3d.JoinWith(cs[i].ToNurbCurve3d());
                    return nc3d.ToCurve();
                }
                else
                {
                    return ToPolyline(curve);
                }
            }
        }

        /// <summary>
        /// 将三维复合曲线转换为实体类多段线
        /// </summary>
        /// <param name="cc3d">三维复合曲线</param>
        /// <returns>实体类多段线</returns>
        public static Polyline ToPolyline(this CompositeCurve3d cc3d)
        {
            Polyline pl = new Polyline();
            pl.Elevation = cc3d.StartPoint[2];
            Plane plane = pl.GetPlane();
            Point2d endver = Point2d.Origin;
            int i = 0;
            foreach (Curve3d c3d in cc3d.GetCurves())
            {
                if (c3d is CircularArc3d)
                {
                    CircularArc3d ca3d = (CircularArc3d)c3d;
                    double b = Math.Tan(0.25 * (ca3d.EndAngle - ca3d.StartAngle)) * ca3d.Normal[2];
                    pl.AddVertexAt(i, c3d.StartPoint.Convert2d(plane), b, 0, 0);
                    endver = c3d.EndPoint.Convert2d(plane);
                }
                else
                {
                    pl.AddVertexAt(i, c3d.StartPoint.Convert2d(plane), 0, 0, 0);
                    endver = c3d.EndPoint.Convert2d(plane);
                }
                i++;
            }
            pl.AddVertexAt(i, endver, 0, 0, 0);
            return pl;
        }

        #endregion CompositeCurve3d

        #region Line3d

        /// <summary>
        /// 将解析类三维构造线转换为实体类构造线
        /// </summary>
        /// <param name="line3d">解析类三维构造线</param>
        /// <returns>实体类构造线</returns>
        public static Xline ToCurve(this Line3d line3d)
        {
            return
                new Xline
                {
                    BasePoint = line3d.PointOnLine,
                    SecondPoint = line3d.PointOnLine + line3d.Direction
                };
        }

        /// <summary>
        /// 将三维解析类构造线转换为三维解析类线段
        /// </summary>
        /// <param name="line3d">三维解析类构造线</param>
        /// <param name="fromParameter">起点参数</param>
        /// <param name="toParameter">终点参数</param>
        /// <returns>三维解析类线段</returns>
        public static LineSegment3d ToLineSegment3d(this Line3d line3d, double fromParameter, double toParameter)
        {
            return
                new LineSegment3d
                (
                    line3d.EvaluatePoint(fromParameter),
                    line3d.EvaluatePoint(toParameter)
                );
        }

        #endregion Line3d

        #region LineSegment3d

        /// <summary>
        /// 将三维解析类线段转换为实体类直线
        /// </summary>
        /// <param name="lineSeg3d">三维解析类线段</param>
        /// <returns>实体类直线</returns>
        public static Line ToCurve(this LineSegment3d lineSeg3d)
        {
            return new Line(lineSeg3d.StartPoint, lineSeg3d.EndPoint);
        }

        #endregion LineSegment3d

        #region CircularArc3d

        /// <summary>
        /// 将三维解析类圆/弧转换为实体圆/弧
        /// </summary>
        /// <param name="ca3d">三维解析类圆/弧</param>
        /// <returns>实体圆/弧</returns>
        public static Curve ToCurve(this CircularArc3d ca3d)
        {
            if (ca3d.IsClosed())
            {
                return ToCircle(ca3d);
            }
            else
            {
                return ToArc(ca3d);
            }
        }

        /// <summary>
        /// 将三维解析类圆/弧转换为实体圆
        /// </summary>
        /// <param name="ca3d">三维解析类圆/弧</param>
        /// <returns>实体圆</returns>
        public static Circle ToCircle(this CircularArc3d ca3d)
        {
            return
               new Circle(
                   ca3d.Center,
                   ca3d.Normal,
                   ca3d.Radius);
        }

        /// <summary>
        /// 将三维解析类圆/弧转换为实体圆弧
        /// </summary>
        /// <param name="ca3d">三维解析类圆/弧</param>
        /// <returns>实体圆弧</returns>
        public static Arc ToArc(this CircularArc3d ca3d)
        {
            //必须新建，而不能直接使用GetPlane()获取
            double angle =
                ca3d.ReferenceVector.AngleOnPlane(
                    new Plane(
                        ca3d.Center,
                        ca3d.Normal));
            return
                new Arc(
                    ca3d.Center,
                    ca3d.Normal,
                    ca3d.Radius,
                    ca3d.StartAngle + angle,
                    ca3d.EndAngle + angle);
        }

        /// <summary>
        /// 将三维解析类圆/弧转换为三维解析类椭圆弧
        /// </summary>
        /// <param name="ca3d">三维解析类圆/弧</param>
        /// <returns>三维解析类椭圆弧</returns>
        public static EllipticalArc3d ToEllipticalArc3d(this CircularArc3d ca3d)
        {
            Vector3d zaxis = ca3d.Normal;
            Vector3d xaxis = ca3d.ReferenceVector;
            Vector3d yaxis = zaxis.CrossProduct(xaxis);

            return
                new EllipticalArc3d(
                    ca3d.Center,
                    xaxis,
                    yaxis,
                    ca3d.Radius,
                    ca3d.Radius,
                    ca3d.StartAngle,
                    ca3d.EndAngle);
        }

        /// <summary>
        /// 将三维解析类圆/弧转换为三维解析类Nurb曲线
        /// </summary>
        /// <param name="ca3d">三维解析类圆/弧</param>
        /// <returns>三维解析类Nurb曲线</returns>
        public static NurbCurve3d ToNurbCurve3d(this CircularArc3d ca3d)
        {
            EllipticalArc3d ea3d = ToEllipticalArc3d(ca3d);
            NurbCurve3d nc3d = new NurbCurve3d(ea3d);
            return nc3d;
        }

        #endregion CircularArc3d

        #region EllipticalArc3d

        /// <summary>
        /// 将三维解析类椭圆弧转换为实体类椭圆弧
        /// </summary>
        /// <param name="ea3d">三维解析类椭圆弧</param>
        /// <returns>实体类椭圆弧</returns>
        public static Ellipse ToCurve(this EllipticalArc3d ea3d)
        {
            Ellipse ell =
                new Ellipse(
                    ea3d.Center,
                    ea3d.Normal,
                    ea3d.MajorAxis * ea3d.MajorRadius,
                    ea3d.MinorRadius / ea3d.MajorRadius,
                    0,
                    Math.PI * 2);
            //Ge椭圆角度就是Db椭圆的参数
            if (!ea3d.IsClosed())
            {
                ell.StartAngle = ell.GetAngleAtParameter(ea3d.StartAngle);
                ell.EndAngle = ell.GetAngleAtParameter(ea3d.EndAngle);
            }
            return ell;
        }

        #endregion EllipticalArc3d

        #region NurbCurve3d

        /// <summary>
        /// 将三维解析类Nurb曲线转换为实体类样条曲线
        /// </summary>
        /// <param name="nc3d">三维解析类Nurb曲线</param>
        /// <returns>实体类样条曲线</returns>
        public static Spline ToCurve(this NurbCurve3d nc3d)
        {
            Spline spl = null;
            if (nc3d.HasFitData)
            {
                NurbCurve3dFitData fdata = nc3d.FitData;
                if (fdata.TangentsExist)
                {
                    spl = new Spline(
                        fdata.FitPoints,
                        fdata.StartTangent,
                        fdata.EndTangent,
                        nc3d.Order,
                        fdata.FitTolerance.EqualPoint);
                }
                else
                {
                    spl = new Spline(
                        fdata.FitPoints,
                        nc3d.Order,
                        fdata.FitTolerance.EqualPoint);
                }
            }
            else
            {
                DoubleCollection knots = new DoubleCollection();
                foreach (double knot in nc3d.Knots)
                    knots.Add(knot);

                NurbCurve3dData ncdata = nc3d.DefinitionData;

                spl = new Spline(
                        ncdata.Degree,
                        ncdata.Rational,
                        nc3d.IsClosed(),
                        ncdata.Periodic,
                        ncdata.ControlPoints,
                        knots,
                        ncdata.Weights,
                        Tolerance.Global.EqualPoint,
                        ncdata.Knots.Tolerance);
            }
            return spl;
        }

        #endregion NurbCurve3d

        #region PolylineCurve3d

        /// <summary>
        /// 将三维解析类多段线转换为实体类三维多段线
        /// </summary>
        /// <param name="pl3d">三维解析类多段线</param>
        /// <returns>实体类三维多段线</returns>
        public static Polyline3d ToCurve(this PolylineCurve3d pl3d)
        {
            Point3dCollection pnts = new Point3dCollection();

            for (int i = 0; i < pl3d.NumberOfControlPoints; i++)
            {
                pnts.Add(pl3d.ControlPointAt(i));
            }

            bool closed = false;
            int n = pnts.Count - 1;
            if (pnts[0] == pnts[n])
            {
                pnts.RemoveAt(n);
                closed = true;
            }
            return new Polyline3d(Poly3dType.SimplePoly, pnts, closed);
        }

        #endregion PolylineCurve3d
    }
}