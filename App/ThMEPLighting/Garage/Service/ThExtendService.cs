﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.CAD;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.Common;
using ThMEPEngineCore.Algorithm;

namespace ThMEPLighting.Garage.Service
{
    public class ThExtendService
    {
        private List<Tuple<Curve, Curve, Curve>> Curves { get; set; }
        private ThExtendService(List<Tuple<Curve ,Curve,Curve>> curves)
        {
            Curves = curves;
        }
        public static List<Tuple<Curve, Curve, Curve>> Extend(List<Tuple<Curve, Curve, Curve>> curves)
        {
            var instance = new ThExtendService(curves);
            instance.Extend();
            return instance.Curves;
        }
        private void Extend()
        {
            for(int i=0;i< Curves.Count-1;i++)
            {
                for (int j = i+1; j < Curves.Count; j++)
                {
                    if(Curves[i].Item1 is Line first && Curves[j].Item1 is Line second)
                    {
                        if (first.StartPoint.GetVectorTo(first.EndPoint).
                             IsParallelToEx(second.StartPoint.GetVectorTo(second.EndPoint)))
                        {
                            continue;
                        }
                    }
                    var pts = new Point3dCollection();
                    (Curves[i].Item1 as Line).ExtendLine().IntersectWith((Curves[j].Item1 as Line).ExtendLine(),
                        Intersect.OnBothOperands,pts,IntPtr.Zero, IntPtr.Zero);
                    if(pts.Count>0)
                    {
                        Extend(Curves[i], Curves[j]);
                    }
                }
            }

            AdjustMergeLine();
        }

        private void AdjustMergeLine()
        {
            ChangeCurve(true);
            ChangeCurve(false);
        }
        private void ChangeCurve(bool isOne)
        {
            List<Tuple<Curve, Curve, Curve>> newCurves = new List<Tuple<Curve, Curve, Curve>>();
            var firCurve = Curves.First();
            Curves.Remove(firCurve);
            while (Curves.Count > 0)
            {
                var oneCurve = firCurve.Item2;
                if (!isOne)
                {
                    oneCurve = firCurve.Item3;
                }
                bool needChange = false;
                foreach (var curve in Curves)
                {
                    var twoCurve = curve.Item2;
                    if (!isOne)
                    {
                        twoCurve = curve.Item3;
                    }
                    List<Tuple<Curve, Curve, Curve>> checkCurves = new List<Tuple<Curve, Curve, Curve>>(Curves);
                    checkCurves.AddRange(newCurves);
                    checkCurves.Remove(curve);
                    Overlap(ref oneCurve, twoCurve, checkCurves, true, out needChange);
                    if (needChange)
                    {
                        break;
                    }
                }
                if (needChange)
                {
                    Curves.Add(firCurve);
                }
                else
                {
                    if (isOne)
                    {
                        newCurves.Add(new Tuple<Curve, Curve, Curve>(firCurve.Item1, oneCurve, firCurve.Item3));
                    }
                    else
                    {
                        newCurves.Add(new Tuple<Curve, Curve, Curve>(firCurve.Item1, firCurve.Item2, oneCurve));
                    }
                }
                firCurve = Curves.First();
                Curves.Remove(firCurve);
            }
            newCurves.Add(new Tuple<Curve, Curve, Curve>(firCurve.Item1, firCurve.Item2, firCurve.Item3));
            Curves = newCurves;
        }
        private void Overlap(ref Curve curve1, Curve curve2, List<Tuple<Curve, Curve, Curve>> curves, bool isOne, out bool needChange)
        {
            needChange = false;
            if (curve2.GetClosestPointTo(curve1.StartPoint, false).DistanceTo(curve1.StartPoint) < 1&&
                !curve1.StartPoint.IsEqualTo(curve2.StartPoint, new Tolerance(5, 5)) && 
                !curve1.StartPoint.IsEqualTo(curve2.EndPoint, new Tolerance(5, 5)))
            {
                if (curve2.StartPoint.DistanceTo(curve1.EndPoint) < curve2.EndPoint.DistanceTo(curve1.EndPoint))
                {
                    if (!(curve1.GetClosestPointTo(curve2.StartPoint, true).DistanceTo(curve2.StartPoint) < 1))
                    {
                        return;
                    }
                    if (!CheckOnNumLine(curves, curve2.StartPoint, isOne))
                    {
                        needChange = true;
                        return;
                    }
                    if (curve1 is Line)
                    {
                        curve1 = new Line(curve2.StartPoint, curve1.EndPoint);
                    }
                    if (curve1 is Polyline curvePoly)
                    {
                        Polyline polyline = new Polyline();
                        polyline.AddVertexAt(0, curve2.StartPoint.ToPoint2D(), 0, 0, 0);
                        for (int i = 1; i < curvePoly.NumberOfVertices; i++)
                        {
                            polyline.AddVertexAt(i, curvePoly.GetPoint2dAt(i), 0, 0, 0);
                        }
                        curve1 = polyline;
                    }
                }
                else
                {
                    if (!(curve1.GetClosestPointTo(curve2.EndPoint, true).DistanceTo(curve2.EndPoint) < 1))
                    {
                        return;
                    }
                    if (!CheckOnNumLine(curves, curve2.EndPoint, isOne))
                    {
                        needChange = true;
                        return;
                    }
                    if (curve1 is Line)
                    {
                        curve1 = new Line(curve2.EndPoint, curve1.EndPoint);
                    }
                    if (curve1 is Polyline curvePoly)
                    {
                        Polyline polyline = new Polyline();
                        polyline.AddVertexAt(0, curve2.EndPoint.ToPoint2D(), 0, 0, 0);
                        for (int i = 1; i < curvePoly.NumberOfVertices; i++)
                        {
                            polyline.AddVertexAt(i, curvePoly.GetPoint2dAt(i), 0, 0, 0);
                        }
                        curve1 = polyline;
                    }
                }
            }
            else if (curve2.GetClosestPointTo(curve1.EndPoint, false).DistanceTo(curve1.EndPoint) < 1 &&
                !curve1.EndPoint.IsEqualTo(curve2.StartPoint, new Tolerance(5, 5)) &&
                !curve1.EndPoint.IsEqualTo(curve2.EndPoint, new Tolerance(5, 5)))
            {
                if (curve2.StartPoint.DistanceTo(curve1.StartPoint) < curve2.EndPoint.DistanceTo(curve1.StartPoint))
                {
                    if (!(curve1.GetClosestPointTo(curve2.StartPoint, true).DistanceTo(curve2.StartPoint) < 1))
                    {
                        return;
                    }
                    if (!CheckOnNumLine(curves, curve2.StartPoint, isOne))
                    {
                        needChange = true;
                        return;
                    }
                    if (curve1 is Line)
                    {
                        curve1 = new Line(curve1.StartPoint, curve2.StartPoint);
                    }
                    if (curve1 is Polyline curvePoly)
                    {
                        Polyline polyline = new Polyline();
                        for (int i = 0; i < curvePoly.NumberOfVertices - 1; i++)
                        {
                            polyline.AddVertexAt(i, curvePoly.GetPoint2dAt(i), 0, 0, 0);
                        }
                        polyline.AddVertexAt(curvePoly.NumberOfVertices - 1, curve2.StartPoint.ToPoint2D(), 0, 0, 0);
                        curve1 = polyline;
                    }
                }
                else
                {
                    if (!(curve1.GetClosestPointTo(curve2.EndPoint, true).DistanceTo(curve2.EndPoint) < 1))
                    {
                        return;
                    }
                    if (!CheckOnNumLine(curves, curve2.EndPoint, isOne))
                    {
                        needChange = true;
                        return;
                    }
                    if (curve1 is Line)
                    {
                        curve1 = new Line(curve1.StartPoint, curve2.EndPoint);
                    }
                    if (curve1 is Polyline curvePoly)
                    {
                        Polyline polyline = new Polyline();
                        for (int i = 0; i < curvePoly.NumberOfVertices - 1; i++)
                        {
                            polyline.AddVertexAt(i, curvePoly.GetPoint2dAt(i), 0, 0, 0);
                        }
                        polyline.AddVertexAt(curvePoly.NumberOfVertices - 1, curve2.EndPoint.ToPoint2D(), 0, 0, 0);
                        curve1 = polyline;
                    }
                }
            }
        }
        private bool CheckOnNumLine(List<Tuple<Curve, Curve, Curve>> curves, Point3d pt, bool isOne)
        {
            bool isTrue = false;
            foreach (var curve in curves)
            {
                if (isOne)
                {
                    if (curve.Item2.GetClosestPointTo(pt, false).DistanceTo(pt) < 1)
                    {
                        isTrue = true;
                    }
                }
                else
                {
                    if (curve.Item3.GetClosestPointTo(pt, false).DistanceTo(pt) < 1)
                    {
                        isTrue = true;
                    }
                }
            }

            return isTrue;
        } 
        private void Extend(Tuple<Curve, Curve, Curve> current, Tuple<Curve, Curve, Curve> other)
        {
            Extend(current.Item2, other.Item2, other.Item3);
            Extend(current.Item3, other.Item2, other.Item3);
            Extend(other.Item2, current.Item2, current.Item3);
            Extend(other.Item3, current.Item2, current.Item3);
        }
        private void Extend(Curve extendLine,Curve first,Curve second)
        {            
            var firstPts = new Point3dCollection();
            Extend(extendLine).IntersectWith(Extend(first), Intersect.ExtendBoth, firstPts, IntPtr.Zero, IntPtr.Zero);
            var secondPts = new Point3dCollection();
            Extend(extendLine).IntersectWith(Extend(second), Intersect.ExtendBoth, secondPts, IntPtr.Zero, IntPtr.Zero);
            var pts = new List<Point3d>();
            pts.AddRange(firstPts.Cast<Point3d>().ToList());
            pts.AddRange(secondPts.Cast<Point3d>().ToList());
            if(pts.Count==0)
            {
                return;
            }
            var fitlerPts = FilterNotOnCurvePts(extendLine, pts);
            if(fitlerPts.Count==0 /*|| firstPts.Count <= 1 || secondPts.Count <= 1*/)
            {
                return;
            }
            bool extendStart = fitlerPts[0].DistanceTo(extendLine.StartPoint) 
                < fitlerPts[0].DistanceTo(extendLine.EndPoint) ? true : false;
            Point3d toPoint;
            if(extendStart)
            {
                toPoint = fitlerPts.OrderByDescending(o => o.DistanceTo(extendLine.StartPoint)).First();
            }
            else
            {
                toPoint = fitlerPts.OrderByDescending(o => o.DistanceTo(extendLine.EndPoint)).First();
            }
            try
            {
                extendLine.Extend(extendStart, toPoint);
            }
            catch(System.Exception ex)
            {
                return;
                //throw ex;
            }
        }
        private List<Point3d> FilterNotOnCurvePts(Curve curve ,List<Point3d> pts)
        {
            var results = new List<Point3d>();
            if (curve is Polyline polylne)
            {
                results = pts.Where(o => !o.IsPointOnPolyline(polylne)).ToList();
            }
            else if (curve is Line line)
            {
                results = pts.Where(o => !o.IsPointOnLine(line)).ToList();
            }
            else
            {
                throw new NotSupportedException();
            }
            return results;
        }
        private Curve Extend(Curve curve)
        {
            if(curve is Line line)
            {
                return line.ExtendLine();
            }
            else if(curve is Polyline polyline)
            {
                return polyline.ExtendPolyline();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}

