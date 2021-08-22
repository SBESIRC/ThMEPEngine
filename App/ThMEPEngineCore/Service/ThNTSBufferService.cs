using System;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Interface;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Service
{
    public class ThNTSBufferService :IBuffer
    {
        public Entity Buffer(Entity entity,double length)
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
                return objs.Cast<Polyline>().OrderByDescending(o=>o.Area).First();
            }
            else
            {
                return null;
            }
        }

        private MPolygon BufferPolygon(MPolygon mPolygon, double length)
        {
            var polygon = mPolygon.ToNTSPolygon().Buffer(length).ToDbCollection(true);
            if(polygon.Count>0)
            {
                return polygon.Cast<Entity>().OrderByDescending(e => e.GetArea()).First() as MPolygon;
            }
            return null;
        }
        private Circle BufferCircle(Circle circle , double length)
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
