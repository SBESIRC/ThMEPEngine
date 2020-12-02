using System;
using DotNetARX;
using GeometryExtensions;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System.Linq;

namespace ThCADExtension
{
    public static class ThMPolygonExtension
    {
        public static Point3dCollection Vertices(this MPolygon mPolygon)
        {            
            Point3dCollection vertices = new Point3dCollection();
            for (int i = 0; i < mPolygon.NumMPolygonLoops; i++)
            {
                MPolygonLoop mPolygonLoop = mPolygon.GetMPolygonLoopAt(i);
                Polyline polyline = new Polyline()
                {
                    Closed = true
                };
                for (int j = 0; j < mPolygonLoop.Count; j++)
                {
                    var bulgeVertex = mPolygonLoop[j];
                    polyline.AddVertexAt(j, bulgeVertex.Vertex, bulgeVertex.Bulge, 0, 0);
                }
                var pts=polyline.Vertices();
                pts.Cast<Point3d>().ForEach(o => vertices.Add(o));
            }
            return vertices;
        }
    }
}


