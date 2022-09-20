using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPTCH.Services
{
    public class ThPolylineFixer
    {
        private static double Tolerance { get; set; } = 1.0;

        public static Polyline Fix(Polyline slab, Polyline outline)
        {
            // 遍历获得在边缘位置的顶点
            var vertices = outline.Vertices();
            var startIndex = -1;
            var endIndex = -1;
            // true表示边界点
            var startTag = false;
            var endTag = false;
            for (var i = 0; i < vertices.Count; i++)
            {
                if (slab.Distance(vertices[i]) < 1.0)
                {
                    startIndex = i;
                    startTag = true;
                }
                else if (startTag)
                {
                    break;
                }

            }
            for (var i = vertices.Count - 1; i >= 0; i--)
            {
                if (slab.Distance(vertices[i]) < 1.0)
                {
                    endIndex = i;
                    endTag = true;
                }
                else if (endTag)
                {
                    break;
                }
            }

            // 没有位于边缘的顶点/仅有一点位于边缘位置
            if (startIndex != -1 && startIndex != endIndex && !(startIndex == 0 && endIndex == vertices.Count - 1))
            {
                // 暂时只考虑连续边位于楼板边缘
                var newVertices = new Point3dCollection();
                if (startIndex + 1 < vertices.Count && slab.Distance(vertices[startIndex + 1]) < 1.0)
                {
                    for (var i = startIndex; i <= endIndex; i++)
                    {
                        newVertices.Add(vertices[i]);
                    }
                }
                else
                {
                    for (var i = startIndex; i >= 0; i--)
                    {
                        newVertices.Add(vertices[i]);
                    }
                    for (var i = vertices.Count - 2; i >= endIndex; i--)
                    {
                        newVertices.Add(vertices[i]);
                    }
                }
                // 楼板外部Polyline
                var boundary = newVertices.CreatePolyline(false);
                var boundaryBuffer = GetPolyline(boundary.BufferFlatPL(Tolerance));

                var objs = new DBObjectCollection()
                {
                    boundaryBuffer,
                    outline,
                };
                var geometry = objs.UnionGeometries();
                return GetPolyline(geometry.ToDbObjectsEx());
            }
            else
            {
                return outline;
            }
        }

        private static Polyline GetPolyline(DBObjectCollection coll)
        {
            return coll.OfType<Polyline>().OrderByDescending(p => p.Area).FirstOrDefault();
        }

        private static Polyline GetPolyline(List<DBObject> objects)
        {
            return objects.OfType<MPolygon>().Select(o => o.Shell()).OrderByDescending(p => p.Area).FirstOrDefault();
        }
    }
}
