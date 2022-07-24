using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPEngineCore.Algorithm
{
    public  class ThMEPMaximumInscribedRectangleByDirection
    {
        public  double tolerance = 0.02;
        public  Polyline GetRectangle(Polyline poly, Vector3d xDir, double tol = 100)
        {
            if (poly.Area == 0)
            {
                return null;
            }
            // simplify polygon
            poly = poly.DPSimplify(tolerance);

            var yDir = Vector3d.ZAxis.CrossProduct(xDir);
            var zDir = Vector3d.ZAxis;
            Matrix3d matrix = new Matrix3d(new double[] {
                xDir.X, yDir.X, zDir.X, 0,
                xDir.Y, yDir.Y, zDir.Y, 0,
                xDir.Z, yDir.Z, zDir.Z, 0,
                0.0, 0.0, 0.0, 1.0,
            });
            poly.TransformBy(matrix.Inverse());
            var polyBoundingBox = GetBoundingBox(poly);
            GetDivisionLines(poly, polyBoundingBox[0], polyBoundingBox[1], tol, out List<Line> xLines, out List<Line> ylines);
            var divLines = new List<Line>(xLines);
            divLines.AddRange(ylines);
            var polygons = divLines.ToCollection().PolygonsEx().OfType<Polyline>().ToList();
            var maxinmumRectangle = GetMaxinmumRectangle(poly, polygons, xLines, ylines);
            if (maxinmumRectangle == null)
            {
                return null;
            }
            maxinmumRectangle.TransformBy(matrix);

            return maxinmumRectangle;
        }

        /// <summary>
        /// 计算最大内接矩形
        /// </summary>
        /// <param name="outPoly"></param>
        /// <param name="polylines"></param>
        /// <param name="xLines"></param>
        /// <param name="ylines"></param>
        /// <returns></returns>
        private Polyline GetMaxinmumRectangle(Polyline outPoly, List<Polyline> polylines, List<Line> xLines, List<Line> ylines)
        {
            var xIndexRegion = GetIndex(xLines, true);
            var yIndexRegion = GetIndex(ylines, false);
            var resIndexPoly = MarkPolylineIndex(outPoly, polylines, xIndexRegion, yIndexRegion);
            var lMatrices = GetLRectangleRegions(resIndexPoly);
            var rMatrices = GetRRectangleRegions(resIndexPoly);
            List<Polyline> rectangleLst = null;
            for (int i = 0; i < yIndexRegion.Count; i++)   //行
            {
                for (int j = 0; j < xIndexRegion.Count; j++)   //列
                {
                    if (lMatrices[i, j] != null && rMatrices[i, j] != null)
                    {
                        var vMatrix = GetVMatirx(lMatrices, i, j);
                        var hMatrix = GetHMatirx(rMatrices, i, j);
                        if (vMatrix.Count > 0 && hMatrix.Count > 0)
                        {
                            for (int k = 1; k <= vMatrix[0]; k++)
                            {
                                if (k >= hMatrix.Count)
                                {
                                    break;
                                }
                                if (hMatrix[k] >= 1)
                                {
                                    for (int l = 1; l <= hMatrix[k]; l++)
                                    {
                                        if (l >= vMatrix.Count)
                                        {
                                            break;
                                        }
                                        if (vMatrix[l] >= k)
                                        {
                                            //create cycle
                                            var newPolyLst = CreateRectangleCycle(resIndexPoly, i, j, k, l);
                                            if (rectangleLst == null)
                                            {
                                                rectangleLst = newPolyLst;
                                            }
                                            else
                                            {
                                                if (rectangleLst.Sum(x=>x.Area) < newPolyLst.Sum(x=>x.Area))
                                                {
                                                    rectangleLst = newPolyLst;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (rectangleLst == null || rectangleLst.Count <= 0)
            {
                return null;
            }
            return CreateOutRectangle(rectangleLst);
        }

        /// <summary>
        /// 计算最大矩形（用相接小矩形合成最大矩形）
        /// </summary>
        /// <param name="rectangleLst"></param>
        private Polyline CreateOutRectangle(List<Polyline> rectangleLst)
        {
            var allPts = new List<Point3d>();
            foreach (var poly in rectangleLst)
            {
                for (int i = 0; i < poly.NumberOfVertices; i++)
                {
                    allPts.Add(poly.GetPoint3dAt(i));
                }
            }

            double minX = allPts.OrderBy(x => x.X).First().X;
            double maxX = allPts.OrderByDescending(x => x.X).First().X;
            double minY = allPts.OrderBy(x => x.Y).First().Y;
            double maxY = allPts.OrderByDescending(x => x.Y).First().Y;
            Polyline resPoly = new Polyline() { Closed = true };
            resPoly.AddVertexAt(0, new Point2d(minX, minY), 0, 0, 0);
            resPoly.AddVertexAt(1, new Point2d(maxX, minY), 0, 0, 0);
            resPoly.AddVertexAt(2, new Point2d(maxX, maxY), 0, 0, 0);
            resPoly.AddVertexAt(3, new Point2d(minX, maxY), 0, 0, 0);
            return resPoly;
        }

        /// <summary>
        /// 获得矩形环路
        /// </summary>
        /// <param name="resIndexPoly"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="k"></param>
        /// <param name="l"></param>
        /// <returns></returns>
        private List<Polyline> CreateRectangleCycle(Polyline[,] resIndexPoly, int row, int column, int k, int l)
        {
            var rectangle = new List<Polyline>();
            for (int i = row; i <= row + k; i++)   //行
            {
                for (int j = column; j <= column + l; j++)   //列
                {
                    rectangle.Add(resIndexPoly[i, j]);
                }
            }

            return rectangle;
        }

        /// <summary>
        /// 获得H矩阵
        /// </summary>
        /// <param name="lMatrices"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private List<int> GetHMatirx(Polyline[,] rMatrices, int row, int column)
        {
            var hMatrix = new List<int>();
            for (int i = row; i < rMatrices.GetLength(0); i++)   //行
            {
                if (rMatrices[i, column] == null)
                {
                    break;
                }
                int index = 0;
                for (int j = column; j < rMatrices.GetLength(1); j++)   //列
                {
                    if (rMatrices[i, j] == null)
                    {
                        break;
                    }
                    index += 1;
                }
                hMatrix.Add(index);
            }
            return hMatrix;
        }

        /// <summary>
        /// 获得v矩阵
        /// </summary>
        /// <param name="lMatrices"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private List<int> GetVMatirx(Polyline[,] lMatrices, int row, int column)
        {
            var vMatrix = new List<int>();
            for (int i = column; i < lMatrices.GetLength(1); i++)   //列
            {
                if (lMatrices[row, i] == null)
                {
                    break;
                }
                int index = 0;
                for (int j = row; j < lMatrices.GetLength(0); j++)   //行
                {
                    if (lMatrices[j, i] == null)
                    {
                        break;
                    }
                    index += 1;
                }
                vMatrix.Add(index);
            }
            return vMatrix;
        }

        /// <summary>
        /// 获取L的邻接矩阵
        /// </summary>
        /// <param name="resIndexPoly"></param>
        /// <returns></returns>
        private Polyline[,] GetLRectangleRegions(Polyline[,] resIndexPoly)
        {
            var lMatrices = CreateNewArray(resIndexPoly);
            for (int i = 0; i < lMatrices.GetLength(0); i++)
            {
                for (int j = 0; j < lMatrices.GetLength(1); j++)
                {
                    if (i == lMatrices.GetLength(0) - 1)
                    {
                        lMatrices[i, j] = null;
                    }
                    else
                    {
                        if (lMatrices[i, j] != null)
                        {
                            if (lMatrices[i + 1, j] == null)
                            {
                                lMatrices[i, j] = null;
                            }
                        }
                    }
                }
            }

            return lMatrices;
        }

        /// <summary>
        /// 获取R的邻接矩阵
        /// </summary>
        /// <param name="resIndexPoly"></param>
        /// <returns></returns>
        private Polyline[,] GetRRectangleRegions(Polyline[,] resIndexPoly)
        {
            var rMatrices = CreateNewArray(resIndexPoly);
            for (int i = 0; i < rMatrices.GetLength(0); i++)
            {
                for (int j = 0; j < rMatrices.GetLength(1); j++)
                {
                    if (j == rMatrices.GetLength(1) - 1)
                    {
                        rMatrices[i, j] = null;
                    }
                    else
                    {
                        if (rMatrices[i, j] != null)
                        {
                            if (rMatrices[i , j + 1] == null)
                            {
                                rMatrices[i, j] = null;
                            }
                        }
                    }
                }
            }

            return rMatrices;
        }

        /// <summary>
        /// 复制一个二维数组，把值都传递进去（浅拷贝）
        /// </summary>
        /// <param name="polyAry"></param>
        /// <returns></returns>
        private Polyline[,] CreateNewArray(Polyline[,] polyAry)
        {
            Polyline[,] targetAry = new Polyline[polyAry.GetLength(0), polyAry.GetLength(1)];
            for (int i = 0; i < polyAry.GetLength(0); i++)
            {
                for (int j = 0; j < polyAry.GetLength(1); j++)
                {
                    targetAry[i, j] = polyAry[i, j];
                }
            }

            return targetAry;
        }

        /// <summary>
        /// 获取有效区域，并且给每个区域打上下标
        /// </summary>
        /// <param name="outPoly"></param>
        /// <param name="polylines"></param>
        /// <param name="xLines"></param>
        /// <param name="ylines"></param>
        /// <returns></returns>
        private Polyline[,] MarkPolylineIndex(Polyline outPoly, List<Polyline> polylines, List<double> xIndexRegion, List<double> yIndexRegion)
        {
            var resIndexPoly = new Polyline[yIndexRegion.Count, xIndexRegion.Count];           //polyline: 小框  行 列
            foreach (var poly in polylines)
            {
                if (poly.Area > 1 && poly.NumberOfVertices > 4)
                {
                    var pt1 = poly.GetPoint3dAt(0);
                    var pt2 = poly.GetPoint3dAt(2);
                    Point3d centroid = new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);
                    if (outPoly.Contains(centroid))
                    {
                        var xIndex = xIndexRegion.FindIndex(x => Math.Abs(x - centroid.X) < 1);
                        var yIndex = yIndexRegion.FindIndex(x => Math.Abs(x - centroid.Y) < 1);
                        resIndexPoly[yIndex, xIndex] = poly;
                    }
                }
            }

            return resIndexPoly;
        }

        /// <summary>
        /// 获取索引区间
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="isX"></param>
        /// <returns></returns>
        private List<double> GetIndex(List<Line> lines, bool isX)
        {
            var index = new List<double>();
            if (isX)
            {
                lines = lines.OrderBy(x => x.StartPoint.X).ToList();
                for (int i = 1; i < lines.Count; i++)
                {
                    index.Add((lines[i - 1].StartPoint.X + lines[i].StartPoint.X) / 2);
                }
            }
            else
            {
                lines = lines.OrderBy(x => x.StartPoint.Y).ToList();
                for (int i = 1; i < lines.Count; i++)
                {
                    index.Add((lines[i - 1].StartPoint.Y + lines[i].StartPoint.Y) / 2);
                }
            }
            return index;
        }

        /// <summary>
        /// 获取分割线
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="minPt"></param>
        /// <param name="maxPt"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        private void GetDivisionLines(Polyline poly, Point3d minPt, Point3d maxPt, double tol, out List<Line> xlines, out List<Line> ylines)
        {
            double xLength = maxPt.X - minPt.X;
            double yLength = maxPt.Y - minPt.Y;
            var xNum = Math.Ceiling(xLength / tol) + 1;
            var yNum = Math.Ceiling(yLength / tol) + 1;
            xlines = new List<Line>();
            ylines = new List<Line>();
            for (int i = 0; i <= xNum; i++)
            {
                xlines.Add(new Line(new Point3d(minPt.X + tol * i, minPt.Y - 500, 0), new Point3d(minPt.X + tol * i, maxPt.Y + 500, 0)));
            }
            for (int i = 0; i <= yNum; i++)
            {
                ylines.Add(new Line(new Point3d(minPt.X - 500, minPt.Y + tol * i, 0), new Point3d(maxPt.X + 500, minPt.Y + tol * i, 0)));
            }
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                var pt = poly.GetPoint3dAt(i);
                if (!xlines.Any(x=>Math.Abs(x.StartPoint.X - pt.X) < 1))
                {
                    xlines.Add(new Line(new Point3d(pt.X, minPt.Y - 500, 0), new Point3d(pt.X, maxPt.Y + 500, 0)));
                }
                if (!ylines.Any(x => Math.Abs(x.StartPoint.Y - pt.Y) < 1))
                {
                    ylines.Add(new Line(new Point3d(minPt.X - 500, pt.Y, 0), new Point3d(maxPt.X + 500, pt.Y, 0)));
                }
            }
        }

        /// <summary>
        /// 获得polyline的boundingbox
        /// </summary>
        /// <param name="poly"></param>
        /// <returns></returns>
        private List<Point3d> GetBoundingBox(Polyline poly)
        {
            List<Point3d> pts = new List<Point3d>();
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                pts.Add(poly.GetPoint3dAt(i));
            }
            double minX = pts.OrderBy(x => x.X).First().X;
            double maxX = pts.OrderByDescending(x => x.X).First().X;
            double minY = pts.OrderBy(x => x.Y).First().Y;
            double maxY = pts.OrderByDescending(x => x.Y).First().Y;
            return new List<Point3d>() { new Point3d(minX, minY, 0), new Point3d(maxX, maxY, 0) };
        }
    }
}
