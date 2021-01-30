using System;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.LaneLine;

namespace ThMEPLighting.Garage.Service
{
    public class ThOffsetLineService
    {
        public Curve First { get; set; }
        public Curve Second { get; set; }
        private Curve Center { get; set; }
        private double OffsetDistance { get; set; }
        private ThOffsetLineService(Curve center,double offsetDistance)
        {
            Center = center;
            OffsetDistance = offsetDistance;            
        }
        public static ThOffsetLineService Offset(
            Curve center,double offsetDistance)
        {
            var instance = new ThOffsetLineService(center, offsetDistance);
            instance.Offset();
            return instance;
        }
        private void Offset()
        {
            if (Center is Line line)
            {
                Offset(line);
            }
            else if (Center is Polyline polyline)
            {
                Offset(polyline);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private void Offset(Line line)
        {
            var newLine = line.Normalize();
            var bufferLength = CalOffsetLength(newLine);
            var firstPoly = newLine.GetOffsetCurves(bufferLength)[0] as Curve;
            var secPoly = newLine.GetOffsetCurves(-bufferLength)[0] as Curve;
            //var vec = newLine.StartPoint.GetVectorTo(newLine.EndPoint)
            //       .GetPerpendicularVector().GetNormal();
            //var upSp = newLine.StartPoint + vec.MultiplyBy(OffsetDistance);
            //var upEp = newLine.EndPoint + vec.MultiplyBy(OffsetDistance);

            //var downSp = newLine.StartPoint - vec.MultiplyBy(OffsetDistance);
            //var downEp = newLine.EndPoint - vec.MultiplyBy(OffsetDistance);

            First = firstPoly;
            Second = secPoly;
        }
        private double CalOffsetLength(Curve line)
        {
            var bufferLength = OffsetDistance;
            var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
            if (Math.Abs(lineDir.X) > Math.Abs(lineDir.Y))
            {
                if (lineDir.X < 0)
                {
                    bufferLength = -bufferLength;
                }
            }
            else
            {
                if (lineDir.Y < 0)
                {
                    bufferLength = -bufferLength;
                }
            }
            return bufferLength;
        }

        private void Offset(Polyline polyline)
        {
            if (polyline == null)
            {
                return;
            }
            //var objs = new DBObjectCollection();
            //objs.Add(polyline);
            //objs=ThLaneLineEngine.Simplify(objs);
            //var newPoly=objs[0] as Polyline;
            //获取多段线第一段，normalize
            var lineSeg = polyline.GetLineSegmentAt(0);
            var line = new Line(lineSeg.StartPoint,lineSeg.EndPoint);
            var normalLine = line.Normalize();
            var vec = normalLine.StartPoint.GetVectorTo(normalLine.EndPoint)
                  .GetPerpendicularVector().GetNormal();
            
            var positiveObjs = polyline.GetOffsetCurves(OffsetDistance);
            var negativeObjs = polyline.GetOffsetCurves(OffsetDistance * -1.0);

            var firstPolyline = positiveObjs[0] as Polyline;
            var secondPolyline= negativeObjs[0] as Polyline;

            var firstVec = polyline.StartPoint.GetVectorTo(firstPolyline.StartPoint);
            var secondVec = polyline.StartPoint.GetVectorTo(secondPolyline.StartPoint);

            if(firstVec.IsCodirectionalTo(
                vec,new Autodesk.AutoCAD.Geometry.Tolerance(1.0,1.0)))
            {
                First = firstPolyline;
                Second = secondPolyline;
            }
            else if(secondVec.IsCodirectionalTo(
                vec, new Autodesk.AutoCAD.Geometry.Tolerance(1.0, 1.0)))
            {
                First = secondPolyline;
                Second = firstPolyline;
            }
            else
            {
            }
        }
    }

}
