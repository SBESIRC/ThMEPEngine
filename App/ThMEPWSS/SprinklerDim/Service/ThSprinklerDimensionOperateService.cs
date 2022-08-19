using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThSprinklerDimensionOperateService
    {
        /// <summary>
        /// 去除已经标注过的点位
        /// </summary>
        /// <param name="tdim"></param>
        /// <param name="anotherCollineation"></param>
        /// <param name="isDimensioned"></param>
        /// <returns></returns>
        public static List<int> DeleteIsDimed(List<int> tdim, List<List<int>> anotherCollineation, bool[] isDimensioned)
        {
            List<int> dims = new List<int>();
            for (int i = 0; i < anotherCollineation.Count; i++)
            {
                for (int j = 0; j < tdim.Count; j++)
                {
                    if (anotherCollineation[i].Contains(tdim[j]) && !isDimensioned[i]) dims.Add(tdim[j]);
                }
            }
            return dims;
        }

        /// <summary>
        /// 给已经标注过的组打上标签
        /// </summary>
        /// <param name="dim"></param>
        /// <param name="anotherCollineation"></param>
        /// <param name="isDimensioned"></param>
        public static void CheckDimensions(List<int> dim, List<List<int>> anotherCollineation, ref bool[] isDimensioned)
        {
            foreach (int idx in dim)
            {
                for (int i = 0; i < anotherCollineation.Count; i++)
                {
                    if (anotherCollineation[i].Contains(idx))
                    {
                        isDimensioned[i] = true;
                    }
                }
            }
        }

        /// <summary>
        /// 选取最长的一根标注位
        /// </summary>
        /// <param name="undimensionedLine"></param>
        /// <param name="collineation"></param>
        /// <param name="anoCollineation"></param>
        /// <param name="isDimensioned"></param>
        /// <returns></returns>
        public static List<int> GetLongestDimension(List<int> undimensionedLine, List<List<int>> collineation, List<List<int>> anoCollineation, bool[] isDimensioned)
        {
            int len = 0;
            List<int> Dim = new List<int>();
            foreach (int i in undimensionedLine)
            {
                List<int> line = collineation.Where(x => x.Contains(i)).ToList()[0];
                List<int> line1 = DeleteIsDimed(line, anoCollineation, isDimensioned);
                if (line1.Count > len)
                {
                    len = line1.Count;
                    Dim = line;
                }
            }

            return Dim;
        }

        /// <summary>
        /// 选取点最多的组
        /// </summary>
        /// <param name="collineationList"></param>
        /// <returns></returns>
        public static List<int> GetLongestLine(List<List<int>> collineationList)
        {
            int longestLineIndex = 0;
            for (int i = 1; i < collineationList.Count; i++)
            {
                if (collineationList[i].Count > collineationList[longestLineIndex].Count)
                    longestLineIndex = i;
            }

            return collineationList[longestLineIndex];
        }

        /// <summary>
        /// 判断是否碰撞
        /// </summary>
        /// <param name="pts1"></param>
        /// <param name="pts2"></param>
        /// <param name="matrix"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        public static bool IsConflicted(Point3d pts1, Point3d pts2, Matrix3d matrix, ThCADCoreNTSSpatialIndex walls)
        {
            List<Point3d> pts = new List<Point3d> { pts1, pts2 };
            pts = ThCoordinateService.MakeTransformation(pts, matrix.Inverse());
            Line line = new Line(pts[0], pts[pts.Count - 1]);

            return ThSprinklerDimConflictService.NeedToCutOff(line, walls);
        }

        /// <summary>
        /// 获取最近距离
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="dim"></param>
        /// <param name="isNotDimensioned"></param>
        /// <returns></returns>
        public static double GetNeareastDistance(List<Point3d> pts, List<int> dim, List<int> isNotDimensioned)
        {
            Line dimline = new Line(pts[dim[0]], pts[dim[dim.Count - 1]]);
            Point3d Dropfoot = dimline.GetClosestPointTo(pts[isNotDimensioned[0]], true);
            List<double> distance = new List<double>();
            foreach (int i in isNotDimensioned)
            {
                distance.Add(pts[i].DistanceTo(Dropfoot));
            }

            return distance.Min();
        }

        /// <summary>
        /// 打断去除过已经标注的组
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="line"></param>
        /// <param name="line2"></param>
        /// <param name="isXAxis"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public static List<List<int>> SeperateLine(List<Point3d> pts, List<int> line, List<int> line2, bool isXAxis, double step)
        {
            line.Sort((x, y) => ThCoordinateService.GetOriginalValue(pts[x], isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(pts[y], isXAxis)));
            List<List<int>> lines = new List<List<int>>();
            line2.Sort((x, y) => ThCoordinateService.GetOriginalValue(pts[x], isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(pts[y], isXAxis)));

            List<int> one = new List<int> { line[0] };
            for (int i = 1; i < line.Count; i++)
            {
                int iPtIndex = one[one.Count - 1];
                int jPtIndex = line[i];
                if (ThCoordinateService.GetOriginalValue(pts[jPtIndex], isXAxis) - ThCoordinateService.GetOriginalValue(pts[iPtIndex], isXAxis) > 1.5 * step || Math.Abs(line2.IndexOf(iPtIndex) - line2.IndexOf(jPtIndex)) != 1) 
                {
                    lines.Add(one);
                    one = new List<int> { jPtIndex };
                }
                else
                {
                    one.Add(jPtIndex);
                }
            }
            lines.Add(one);

            return lines;
        }

        /// <summary>
        /// 去重去空处理
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="dimensions"></param>
        /// <param name="isXAxis"></param>
        /// <returns></returns>
        public static List<List<List<int>>> DeletNullDimensions(List<Point3d> pts, List<List<List<int>>> dimensions, bool isXAxis)
        {
            List<List<List<int>>> Dimensions = new List<List<List<int>>>();
            foreach(List<List<int>> dims in dimensions)
            {
                List<List<int>> t = new List<List<int>>();
                for (int k = 0; k < dims.Count; k++) 
                {
                    if(dims[k] != null)
                    {
                        dims[k].Sort((x, y) => ThCoordinateService.GetOriginalValue(pts[x], isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(pts[y], isXAxis)));
                        List<int> one = new List<int> { dims[k][0] };
                        for(int j = 1; j < dims[k].Count; j++)
                        {
                            if (pts[dims[k][j]].DistanceTo(pts[dims[k][j - 1]]) > 10)one.Add(dims[k][j]);
                        }
                        t.Add(one);
                    }
                }
                if (t.Count != 0) Dimensions.Add(t);
            }

            return Dimensions;
        }
    }
}
