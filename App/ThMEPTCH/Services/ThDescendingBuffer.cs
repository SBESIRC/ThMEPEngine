using System;
using System.Linq;
using System.Collections.Generic;

using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;

namespace ThMEPTCH.Services
{
    public class ThDescendingBuffer
    {
        private Polyline Slab { get; set; }
        private Polyline Descending { get; set; }
        private bool OnBoundary { get; set; }
        private Polyline DescendingFix { get; set; }

        public ThDescendingBuffer(Polyline slab, Polyline descending)
        {
            Slab = slab;
            Descending = descending;
            OnBoundary = false;
            Init();
        }

        private void Init()
        {
            // 遍历获得在边缘位置的顶点
            var vertices = Descending.Vertices();
            var startIndex = -1;
            var endIndex = -1;
            // true表示边界点
            var startTag = false;
            var endTag = false;
            for (var i = 0; i < vertices.Count; i++)
            {
                if (Slab.Distance(vertices[i]) < 1.0)
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
                if (Slab.Distance(vertices[i]) < 1.0)
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
                OnBoundary = true;
                // 暂时只考虑连续边位于楼板边缘
                var newVertices = new Point3dCollection();
                if (startIndex + 1 < vertices.Count && Slab.Distance(vertices[startIndex + 1]) < 1.0)
                {
                    for (var i = startIndex; i >= 0; i--)
                    {
                        newVertices.Add(vertices[i]);
                    }
                    for (var i = vertices.Count - 1; i >= endIndex; i--)
                    {
                        newVertices.Add(vertices[i]);
                    }
                }
                else
                {
                    for (var i = startIndex; i <= endIndex; i++)
                    {
                        newVertices.Add(vertices[i]);
                    }
                }
                // 楼板内部Polyline
                DescendingFix = newVertices.CreatePolyline(false);
            }
        }

        /// <summary>
        /// 结构降板处理（外扩）
        /// </summary>
        /// <param name="slab"></param>
        /// <param name="descending"></param>
        /// <param name="wrapThickness"></param>
        /// <returns></returns>
        public Polyline OnStructure(double wrapThickness)
        {
            if (OnBoundary)
            {
                return StructureBuffer(Slab, Descending, DescendingFix, wrapThickness);
            }
            else
            {
                // 没有位于边缘的顶点/仅有一点位于边缘位置
                return StructureBuffer(Slab, Descending, wrapThickness);
            }
        }

        /// <summary>
        /// 建筑降板处理（内缩）
        /// </summary>
        /// <param name="slab"></param>
        /// <param name="descending"></param>
        /// <param name="wrapThickness"></param>
        /// <returns></returns>
        public Polyline OnArchitecture(double wrapThickness)
        {
            if (OnBoundary)
            {
                if (wrapThickness == 0)
                {
                    return Descending.Clone() as Polyline;
                }
                else
                {
                    return Architecture(Slab, Descending, DescendingFix, wrapThickness);
                }
            }
            else
            {
                // 没有位于边缘的顶点/仅有一点位于边缘位置
                return Architecture(Slab, Descending, wrapThickness);
            }
        }

        private Polyline StructureBuffer(Polyline slab, Polyline descending, double wrapThickness)
        {
            var buffer = GetPolyline(descending.Buffer(wrapThickness));
            if (!buffer.IsNull())
            {
                var outlineBuffer = GetPolyline(buffer.Intersection(new DBObjectCollection { slab }));
                if (!outlineBuffer.IsNull())
                {
                    return ThMEPFrameService.Simplify(outlineBuffer, 1.0);
                }
            }

            return new Polyline();
        }

        private Polyline StructureBuffer(Polyline slab, Polyline descending, Polyline descendingFix, double wrapThickness)
        {
            var buffer = GetPolyline(descendingFix.BufferFlatPL(wrapThickness));
            if (!buffer.IsNull())
            {
                var objs = new DBObjectCollection()
                {
                    buffer,
                    descending,
                };
                var geometry = objs.UnionGeometries();
                //var geometry = buffer.ToNTSPolygon().Union(descending.ToNTSPolygon());
                var outline = GetPolyline(geometry.ToDbObjectsEx());
                if (!outline.IsNull())
                {
                    var outlineBuffer = GetPolyline(outline.Intersection(new DBObjectCollection { slab }));
                    if (!outlineBuffer.IsNull())
                    {
                        return Simplify(outlineBuffer);
                    }
                }
            }

            return new Polyline();
        }

        private Polyline Architecture(Polyline slab, Polyline descending, double wrapThickness)
        {
            var outlineBuffer = GetPolyline(descending.Buffer(-wrapThickness));
            if (!outlineBuffer.IsNull())
            {
                outlineBuffer = GetPolyline(outlineBuffer.Intersection(new DBObjectCollection { slab }));
                if (!outlineBuffer.IsNull())
                {
                    return ThMEPFrameService.Simplify(outlineBuffer, 1.0);
                }
            }

            return new Polyline();
        }

        private Polyline Architecture(Polyline slab, Polyline descending, Polyline descendingFix, double wrapThickness)
        {
            var buffer = GetPolyline(descendingFix.BufferFlatPL(wrapThickness));
            if (!buffer.IsNull())
            {
                var outline = GetPolyline(descending.Difference(new DBObjectCollection { buffer }));
                if (!outline.IsNull())
                {
                    return Simplify(outline);
                }
            }

            return new Polyline();
        }

        private Polyline GetPolyline(DBObjectCollection coll)
        {
            return coll.OfType<Polyline>().OrderByDescending(p => p.Area).FirstOrDefault();
        }

        private Polyline GetPolyline(List<DBObject> DBObjects)
        {
            return DBObjects.OfType<MPolygon>().Select(o => o.Shell()).OrderByDescending(p => p.Area).FirstOrDefault();
        }

        private Polyline Simplify(Polyline outline)
        {
            var goingOn = true;
            var i = 1;
            while (goingOn)
            {
                goingOn = false;
                for (; i <= outline.EndParam - 1; i++)
                {
                    if (outline.GetPointAtParam(i - 1).DistanceTo(outline.GetPointAtParam(i)) < 1.0)
                    {
                        outline.RemoveVertexAt(i);
                        goingOn = true;
                        break;
                    }
                }
            }

            var pline = ThMEPFrameService.Simplify(outline, 1.0);
            return EndPointConnection(pline);
        }

        private Polyline EndPointConnection(Polyline pline)
        {
            if (pline.Closed && pline.GetPointAtParam(0).DistanceTo(pline.GetPointAtParam(pline.EndParam - 1)) < 1.0)
            {
                var endDirection = (pline.GetPointAtParam(pline.EndParam - 2) - pline.GetPointAtParam(pline.EndParam - 1)).GetNormal();
                var startDirection = (pline.GetPointAtParam(0) - pline.GetPointAtParam(1)).GetNormal();
                if (endDirection.DotProduct(startDirection) > Math.Cos(1 / 180.0 * Math.PI))
                {
                    var vertices = new Point3dCollection();
                    for (var j = 1; j < pline.EndParam - 1; j++)
                    {
                        vertices.Add(pline.GetPointAtParam(j));
                    }
                    return vertices.CreatePolyline();
                }
            }
            return pline;
        }
    }
}
