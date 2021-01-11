using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.CAD;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.Garage.Service
{
    public class ThExtendService
    {
        private List<Tuple<Curve, Curve, Curve>> Curves { get; set; }
        private ThExtendService(List<Tuple<Curve ,Curve,Curve>> curves)
        {
            Curves = curves;
        }
        public static void Extend(List<Tuple<Curve, Curve, Curve>> curves)
        {
            var instance = new ThExtendService(curves);
            instance.Extend();
        }
        private void Extend()
        {
            for(int i=0;i< Curves.Count-1;i++)
            {
                for (int j = i+1; j < Curves.Count; j++)
                {
                    var pts = new Point3dCollection();
                    Curves[i].Item1.IntersectWith(Curves[j].Item1,
                        Intersect.OnBothOperands,pts,IntPtr.Zero, IntPtr.Zero);
                    if(pts.Count>0)
                    {
                        Extend(Curves[i], Curves[j]);
                    }
                }
            }
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
            extendLine.IntersectWith(first, Intersect.ExtendBoth, firstPts, IntPtr.Zero, IntPtr.Zero);
            var secondPts = new Point3dCollection();
            extendLine.IntersectWith(second, Intersect.ExtendBoth, secondPts, IntPtr.Zero, IntPtr.Zero);
            var pts = new List<Point3d>();
            pts.AddRange(firstPts.Cast<Point3d>().ToList());
            pts.AddRange(secondPts.Cast<Point3d>().ToList());
            if(pts.Count==0)
            {
                return;
            }
            var fitlerPts = FilterNotOnCurvePts(extendLine, pts);
            if(fitlerPts.Count==0)
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
            extendLine.Extend(extendStart, toPoint);
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
    }
}
