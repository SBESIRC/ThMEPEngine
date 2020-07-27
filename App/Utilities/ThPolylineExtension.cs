using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
 
namespace TianHua.AutoCAD.Utility.ExtensionTools
{
    /// <summary>
    /// Provides the Offset() extension method for the Polyline type
    /// </summary>
    public static class ThPolylineExtension
    {
        /// <summary>
        /// Enumeration of offset side options
        /// </summary>
        public enum OffsetSide
        {
            In, Out, Left, Right, Both
        }

        /// <summary>
        /// Offset the source polyline to specified side(s).
        /// </summary>
        /// <param name="source">The polyline to be offseted.</param>
        /// <param name="offsetDist">The offset distance.</param>
        /// <param name="side">The offset side(s).</param>
        /// <returns>A polyline sequence resulting from the offset of the source polyline.</returns>
        public static IEnumerable<Polyline> Offset(this Polyline source, double offsetDist, OffsetSide side)
        {
            offsetDist = Math.Abs(offsetDist);
            using (var plines = new DisposableSet<Polyline>())
            {
                IEnumerable<Polyline> offsetRight = source.GetOffsetCurves(offsetDist).Cast<Polyline>();
                plines.AddRange(offsetRight);
                IEnumerable<Polyline> offsetLeft = source.GetOffsetCurves(-offsetDist).Cast<Polyline>();
                plines.AddRange(offsetLeft);
                double areaRight = offsetRight.Select(pline => pline.Area).Sum();
                double areaLeft = offsetLeft.Select(pline => pline.Area).Sum();
                switch (side)
                {
                    case OffsetSide.In:
                        return plines.RemoveRange(
                           areaRight < areaLeft ? offsetRight : offsetLeft);
                    case OffsetSide.Out:
                        return plines.RemoveRange(
                           areaRight < areaLeft ? offsetLeft : offsetRight);
                    case OffsetSide.Left:
                        return plines.RemoveRange(offsetLeft);
                    case OffsetSide.Right:
                        return plines.RemoveRange(offsetRight);
                    case OffsetSide.Both:
                        plines.Clear();
                        return offsetRight.Concat(offsetLeft);
                    default:
                        return null;
                }
            }
        }

        public static Point3dCollection Vertices(this Polyline pLine)
        {
            //https://keanw.com/2007/04/iterating_throu.html
            Point3dCollection vertices = new Point3dCollection();
            for (int i = 0; i < pLine.NumberOfVertices; i++)
            {
                vertices.Add(pLine.GetPoint3dAt(i));
            }
            return vertices;
        }

        public static bool IsClosed(this Polyline pLine, Tolerance tolerance)
        {            
            // 最少三个顶点才能形成一个闭环
            var vertices = pLine.Vertices();
            if (vertices.Count < 3)
            {
                return false;
            }

            // 比较第一个顶点和最后一个顶点，若他们重合，则多段线闭合；否则不闭合
            var enumerator = vertices.Cast<Point3d>();
            return enumerator.First().IsEqualTo(enumerator.Last(), tolerance);
        }
    }

    public interface IDisposableCollection<T> : ICollection<T>, IDisposable
       where T : IDisposable
    {
        void AddRange(IEnumerable<T> items);
        IEnumerable<T> RemoveRange(IEnumerable<T> items);
    }

    public class DisposableSet<T> : HashSet<T>, IDisposableCollection<T>
       where T : IDisposable
    {
        public DisposableSet()
        {
        }

        public DisposableSet(IEnumerable<T> items)
        {
            AddRange(items);
        }

        public void Dispose()
        {
            if (base.Count > 0)
            {
                System.Exception last = null;
                var list = this.ToList();
                this.Clear();
                foreach (T item in list)
                {
                    if (item != null)
                    {
                        try
                        {
                            item.Dispose();
                        }
                        catch (System.Exception ex)
                        {
                            last = last ?? ex;
                        }
                    }
                }
                if (last != null)
                    throw last;
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (items == null)
                throw new ArgumentNullException("items");
            base.UnionWith(items);
        }

        public IEnumerable<T> RemoveRange(IEnumerable<T> items)
        {
            if (items == null)
                throw new ArgumentNullException("items");
            base.ExceptWith(items);
            return items;
        }
    }

}


