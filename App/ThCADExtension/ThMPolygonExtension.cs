using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThMPolygonExtension
    {
        /// <summary>
        /// MPolygon顶点集合（不支持圆弧段）
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <returns></returns>
        public static Point3dCollection Vertices(this MPolygon mPolygon)
        {
            var vertices = mPolygon
                .Loops()
                .Select(o => o.Vertices())
                .SelectMany(o => o.Cast<Point3d>());
            return new Point3dCollection(vertices.ToArray());
        }
        /// <summary>
        /// MPolygon顶点集合（支持圆弧段）
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Point3dCollection VerticesEx(this MPolygon mPolygon, double length)
        {
            var vertices = mPolygon
                .Loops()
                .Select(o => o.VerticesEx(length))
                .SelectMany(o => o.Cast<Point3d>());
            return new Point3dCollection(vertices.ToArray());
        }
        public static List<Polyline> Loops(this MPolygon mPolygon)
        {
            var loops = new List<Polyline>();
            for (int i = 0; i < mPolygon.NumMPolygonLoops; i++)
            {
                loops.Add(ToDbPolyline(mPolygon.GetMPolygonLoopAt(i)));
            }
            return loops;
        }
        /// <summary>
        /// 获取MPolygon的Shell
        /// 仅支持单个Shell带多个Hole的MPolygon
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <returns></returns>
        public static Polyline Shell(this MPolygon mPolygon)
        {
            for (int i = 0; i < mPolygon.NumMPolygonLoops; i++)
            {
                var loopDir = mPolygon.GetLoopDirection(i);
                if(loopDir == LoopDirection.Exterior)
                {
                    return ToDbPolyline(mPolygon.GetMPolygonLoopAt(i));
                }
            }
            return new Polyline();
        }
        /// <summary>
        /// 获取MPolygon的Holes
        /// 仅支持单个Shell带多个Hole的MPolygon
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <returns></returns>
        public static List<Polyline> Holes(this MPolygon mPolygon)
        {
            var holes = new List<Polyline>();
            for (int i = 0; i < mPolygon.NumMPolygonLoops; i++)
            {
                var loopDir = mPolygon.GetLoopDirection(i);
                if (loopDir == LoopDirection.Interior)
                {
                    holes.Add(ToDbPolyline(mPolygon.GetMPolygonLoopAt(i)));
                }
            }
            return holes;
        }
        /// <summary>
        /// 获取MPolygon是否带洞
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <returns></returns>
        public static bool HasHoles(this MPolygon mPolygon)
        {
            for (int i = 0; i < mPolygon.NumMPolygonLoops; i++)
            {
                var loopDir = mPolygon.GetLoopDirection(i);
                if (loopDir == LoopDirection.Interior)
                {
                    return true;
                }
            }
            return false;
        }
        public static Polyline ToDbPolyline(this MPolygonLoop loop)
        {
            Polyline polyline = new Polyline()
            {
                Closed = true
            };
            for (int i = 0; i < loop.Count; i++)
            {
                var bulgeVertex = loop[i];
                polyline.AddVertexAt(i, bulgeVertex.Vertex, bulgeVertex.Bulge, 0, 0);
            }
            return polyline;
        }
    }
}


