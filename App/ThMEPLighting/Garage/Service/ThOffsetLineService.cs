using System;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;

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
            if(Center is Line line)
            {
                Offset(line);
            }
            else if(Center is Polyline polyline)
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
            var vec = newLine.StartPoint.GetVectorTo(newLine.EndPoint)
                   .GetPerpendicularVector().GetNormal();
            var upSp = newLine.StartPoint + vec.MultiplyBy(OffsetDistance);
            var upEp = newLine.EndPoint + vec.MultiplyBy(OffsetDistance);

            var downSp = newLine.StartPoint - vec.MultiplyBy(OffsetDistance);
            var downEp = newLine.EndPoint - vec.MultiplyBy(OffsetDistance);

            First = new Line(upSp, upEp);
            Second = new Line(downSp, downEp);
        }
        private void Offset(Polyline polyline)
        {
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
