using System;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Interface;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.BuildRoom.Service
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
        private Polyline BufferPoly(Polyline polyline, double length)
        {
            var objs = polyline.Buffer(length);
            if (objs.Count > 0)
            {
                return objs[0] as Polyline;
            }
            else
            {
                return null;
            }
        }
        private MPolygon BufferPolygon(MPolygon mPolygon, double length)
        {
            var polygon = mPolygon.ToNTSPolygon();
            var shell = polygon.Shell.ToDbPolyline();
            var shellBufferObjs = shell.Buffer(length);
            if (shellBufferObjs.Count > 0)
            {
                var shellPoly = shellBufferObjs[0] as Polyline;
                var holePolys = new List<Polyline>();
                foreach (var hole in polygon.Holes)
                {
                    var holePoly = hole.ToDbPolyline();
                    var holeBufferObjs = holePoly.Buffer(-1.0 * length);
                    if (holeBufferObjs.Count > 0)
                    {
                        holePolys.Add(holeBufferObjs[0] as Polyline);
                    }
                }
                if (holePolys.Count == polygon.Holes.Length)
                {
                    return ThMPolygonTool.CreateMPolygon(shellPoly, holePolys.Cast<Curve>().ToList());
                }
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
