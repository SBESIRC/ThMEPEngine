using System;
using System.Linq;
using GeometryExtensions;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Xbim.Ifc;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.GeometricModelResource;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPIFC
{
    public static class ThTGL2IFCDbExtension
    {
        private static IfcIndexedPolyCurve CreateIfcIndexedPolyCurve(IfcStore model)
        {
            return model.Instances.New<IfcIndexedPolyCurve>(ipc =>
            {
                ipc.SelfIntersect = false;
                ipc.Points = model.Instances.New<IfcCartesianPointList2D>();
            });
        }

        public static IfcIndexedPolyCurve ToIfcIndexedPolyCurve(IfcStore model, Polyline polyline)
        {
            var ret = CreateIfcIndexedPolyCurve(model);
            var cartesianPointList2D = ret.Points as IfcCartesianPointList2D;

            int pcount = 1;
            var points = new List<List<double>>();
            var segments = new PolylineSegmentCollection(polyline);
            for (int k = 0; k < segments.Count(); k++)
            {
                var segment = segments[k];
                if (segment.IsLinear)
                {
                    // 直线段
                    var line = segment.ToLineSegment();
                    points.Add(new List<double> { line.StartPoint.X, line.StartPoint.Y });

                    var ifclineindex = new IfcLineIndex();
                    IfcLineIndex.Add(ref ifclineindex, pcount);
                    if (k != segments.Count() - 1)
                        IfcLineIndex.Add(ref ifclineindex, pcount + 1);
                    else
                        IfcLineIndex.Add(ref ifclineindex, 1);

                    ret.Segments.Add(ifclineindex);
                    pcount++;
                }
                else
                {
                    // 圆弧段
                    var arc = segment.ToCircularArc();
                    points.Add(new List<double> { arc.StartPoint.X, arc.StartPoint.Y });

                    // 圆弧中点
                    double p1 = arc.GetParameterOf(arc.StartPoint);
                    double p2 = arc.GetParameterOf(arc.EndPoint);
                    var midPoint = arc.EvaluatePoint(p1 + (p2 - p1) / 2.0);
                    points.Add(new List<double> { midPoint.X, midPoint.Y });

                    var ifcarcindex = new IfcArcIndex();
                    IfcArcIndex.Add(ref ifcarcindex, pcount);
                    IfcArcIndex.Add(ref ifcarcindex, pcount + 1);
                    if (k != segments.Count() - 1)
                        IfcArcIndex.Add(ref ifcarcindex, pcount + 2);
                    else
                        IfcArcIndex.Add(ref ifcarcindex, 1);

                    ret.Segments.Add(ifcarcindex);
                    pcount += 2;
                }
            }

            var true_points = points.ToArray();
            for (int i = 0; i < true_points.Length; i++)
            {
                var tps = true_points[i].ToArray();
                var values = tps.Select(v => new IfcLengthMeasure(v));
                cartesianPointList2D.CoordList.GetAt(i).AddRange(values);
            }

            return ret;
        }
    }
}
