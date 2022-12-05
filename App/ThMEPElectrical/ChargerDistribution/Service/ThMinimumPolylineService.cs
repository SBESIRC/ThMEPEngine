using System.Linq;
using System.Collections.Generic;

using NFox.Cad;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;

namespace ThMEPElectrical.ChargerDistribution.Service
{
    public class ThMinimumPolylineService
    {
        public static Polyline CreatePolyline(List<Point3d> points, ObjectId layerId, int colorIndex)
        {
            points = Calculate(points);
            var pointCollection = points.ToCollection();
            var pline = new Polyline();
            if (pointCollection.Count > 1)
            {
                pline.CreatePolyline(pointCollection);
            }
            else if (pointCollection.Count == 1)
            {
                pline = (new Circle(pointCollection[0], Vector3d.ZAxis, 100.0)).TessellateCircleWithChord(100.0);
            }
            pline.LayerId = layerId;
            pline.ColorIndex = colorIndex;
            return pline;
        }

        private static List<Point3d> Calculate(List<Point3d> points)
        {
            // 创建距离矩阵
            var matrix = new double[points.Count, points.Count];
            for (var i = 0; i < points.Count; i++)
            {
                for (var j = 0; j < points.Count; j++)
                {
                    matrix[i, j] = points[i].DistanceTo(points[j]);
                }
            }

            // 计算预估值
            var firstIndex = new List<int>();
            for (var i = 0; i < points.Count; i++)
            {
                firstIndex.Add(i);
            }
            var minLength = 0.0;
            for (var i = 1; i < firstIndex.Count; i++)
            {
                minLength += matrix[firstIndex[i - 1], firstIndex[i]];
            }

            // 遍历
            var info = new IndexInfo(firstIndex, minLength);
            for (var a = 0; a < points.Count; a++)
            {
                var thisIndexs = new List<int> { a };
                Add(matrix, thisIndexs, points, info);
            }

            var result = new List<Point3d>();
            for (var i = 0; i < info.Indexs.Count; i++)
            {
                result.Add(points[info.Indexs[i]]);
            }
            return result;
        }

        private static void Add(double[,] matrix, List<int> thisIndexs, List<Point3d> points, IndexInfo info)
        {
            if (thisIndexs.Count == points.Count && thisIndexs.First() < thisIndexs.Last())
            {
                var length = GetLength(thisIndexs, matrix);
                if (length < info.Length)
                {
                    info.Length = GetLength(thisIndexs, matrix);
                    info.Indexs = thisIndexs;
                }
            }
            else
            {
                for (var b = 0; b < points.Count; b++)
                {
                    if (thisIndexs.Contains(b))
                    {
                        continue;
                    }
                    else
                    {
                        // 及时忽略错误结果
                        var length = 0.0;
                        for (var j = 1; j < thisIndexs.Count; j++)
                        {
                            if (length > info.Length)
                            {
                                break;
                            }
                            else
                            {
                                length += matrix[thisIndexs[j - 1], thisIndexs[j]];
                            }
                        }
                        if (length > info.Length)
                        {
                            continue;
                        }

                        var indexsClone = Clone(thisIndexs);
                        indexsClone.Add(b);
                        Add(matrix, indexsClone, points, info);
                    }
                }
            }
        }

        private static double GetLength(List<int> indexs, double[,] matrix)
        {
            var length = 0.0;
            for (var i = 1; i < indexs.Count; i++)
            {
                length += matrix[indexs[i - 1], indexs[i]];
            }
            return length;
        }

        private static List<int> Clone(List<int> thisIndexs)
        {
            var result = new List<int>();
            thisIndexs.ForEach(o => result.Add(o));
            return result;
        }
    }

    public class IndexInfo
    {
        public double Length { get; set; }

        public List<int> Indexs { get; set; }

        public IndexInfo(List<int> indexs, double length)
        {
            Indexs = indexs;
            Length = length;
        }
    }
}
