using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Interface;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThNTSBufferService : IBuffer
    {
        public Entity Buffer(Entity entity, double length)
        {
            if (entity is Polyline polyline)
            {
                return BufferPoly(polyline, length);
            }
            else if (entity is MPolygon mPolygon)
            {
                return BufferPolygon(mPolygon, length);
            }
            else if (entity is Circle circle)
            {
                return BufferCircle(circle, length);
            }
            else if (entity is Line line && length > 0.0)
            {
                return line.Buffer(length);
            }
            else if (entity is Ellipse ellipse)
            {
                return BufferEllipse(ellipse , length);
            }
            else
            {
                return null;
            }
        }

        //TODO: BuffePoly并没有对看上去封闭但实际上没有封闭的作处理。
        private Polyline BufferPoly(Polyline polyline, double length)
        {
            var objs = new DBObjectCollection();
            if (polyline.Closed)
            {
                objs = polyline.Buffer(length);
            }
            else
            {
                objs = polyline.BufferPL(length);
            }
            if (objs.Count > 0)
            {
                return objs.Cast<Polyline>().OrderByDescending(o => o.Area).First();
            }
            else
            {
                return null;
            }
        }

        private MPolygon BufferPolygon(MPolygon mPolygon, double length)
        {
            var polygons = mPolygon.Buffer(length, true);
            if (polygons.Count > 0)
            {
                return polygons.Cast<Entity>().OrderByDescending(e => e.GetArea()).First() as MPolygon;
            }
            return null;
        }

        private Ellipse BufferEllipse(Ellipse ellipse, double length)
        {
            if (length > 0 ||
                (length < 0  && Math.Abs(length) < ellipse.MajorAxis.Length && Math.Abs(length) < ellipse.MajorAxis.Length))
            {
           
                var newMajor = ellipse.MajorAxis + ellipse.MajorAxis.GetNormal() * length;
                var newMinor = ellipse.MinorAxis + ellipse.MinorAxis.GetNormal() * length;
                var newRatio = newMinor.Length  / newMajor.Length ;
                return new Ellipse(ellipse.Center, ellipse.Normal, newMajor, newRatio, ellipse.StartAngle, ellipse.EndAngle);
            }
            
                return null;
          
        }

        private Circle BufferCircle(Circle circle, double length)
        {
            if (length > 0)
            {
                return new Circle(circle.Center, circle.Normal, circle.Radius + length);
            }
            else
            {
                if (Math.Abs(length) < circle.Radius)
                {
                    return new Circle(circle.Center, circle.Normal, circle.Radius + length);
                }
            }
            return null;
        }
    }
}
