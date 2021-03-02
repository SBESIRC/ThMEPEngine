using System;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
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
            var bufferLength = CalOffsetLength(newLine, OffsetDistance);
            var firstPoly = newLine.GetOffsetCurves(bufferLength)[0] as Curve;
            var secPoly = newLine.GetOffsetCurves(-bufferLength)[0] as Curve;
            
            First = firstPoly;
            Second = secPoly;
        }
        public static double CalOffsetLength(Curve line, double bufferLength)
        {
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
            
            var line = new Line(polyline.StartPoint, polyline.EndPoint);
            var normalLine = line.Normalize();
            var bufferLength = CalOffsetLength(normalLine, OffsetDistance);
            if ((normalLine.EndPoint - normalLine.StartPoint).GetNormal().IsEqualTo((line.EndPoint - line.StartPoint).GetNormal(), new Tolerance(1, 1)))
            {
                bufferLength = -bufferLength;
            }            
            var positiveObjs = polyline.GetOffsetCurves(bufferLength);
            var negativeObjs = polyline.GetOffsetCurves(-bufferLength);

            var firstPolyline = positiveObjs[0] as Polyline;
            var secondPolyline= negativeObjs[0] as Polyline;

            First = firstPolyline;
            Second = secondPolyline;
        }
    }
}
