using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.SprinklerDim.Model;

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
        public static double GetNeareastDistance(List<Point3d> pts, List<int> dim, List<int> isNotDimensioned,bool IsxAxis)
        {
            int d1 = dim[0];
            List<double> det = new List<double>();
            List<double> absdet = new List<double>();
            foreach (int id in isNotDimensioned)
            {
                det.Add(ThCoordinateService.GetOriginalValue(pts[id], !IsxAxis) - ThCoordinateService.GetOriginalValue(pts[d1], !IsxAxis));
            }
            det.Sort();
            det.ForEach(p => absdet.Add(Math.Abs(p)));
            if (det[0] * det[det.Count - 1] < 0) return 0;
            else return absdet.Min();
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
        public static List<List<ThSprinklerDimGroup>> SeperateLine(List<Point3d> pts, List<ThSprinklerDimGroup> DimedPtRemoved, List<int> DimedPtNotRemovedIndex, bool isXAxis, double step)
        {
            DimedPtRemoved.Sort((x, y) => ThCoordinateService.GetOriginalValue(pts[x.pt], isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(pts[y.pt], isXAxis)));
            List<List<ThSprinklerDimGroup>> lines = new List<List<ThSprinklerDimGroup>>();
            DimedPtNotRemovedIndex.Sort((x, y) => ThCoordinateService.GetOriginalValue(pts[x], isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(pts[y], isXAxis)));

            List<ThSprinklerDimGroup> one = new List<ThSprinklerDimGroup> { DimedPtRemoved[0] };
            for (int i = 1; i < DimedPtRemoved.Count; i++)
            {
                ThSprinklerDimGroup iPtIndex = one[one.Count - 1];
                ThSprinklerDimGroup jPtIndex = DimedPtRemoved[i];
                if (ThCoordinateService.GetOriginalValue(pts[jPtIndex.pt], isXAxis) - ThCoordinateService.GetOriginalValue(pts[iPtIndex.pt], isXAxis) > 1.5 * step || Math.Abs(DimedPtNotRemovedIndex.IndexOf(iPtIndex.pt) - DimedPtNotRemovedIndex.IndexOf(jPtIndex.pt)) != 1)
                {
                    lines.Add(one);
                    one = new List<ThSprinklerDimGroup> { jPtIndex };
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
        public static List<List<List<ThSprinklerDimGroup>>> DeletNullDimensions(List<Point3d> pts, List<List<List<ThSprinklerDimGroup>>> dimensions, bool isXAxis)
        {
            List<List<List<ThSprinklerDimGroup>>> Dimensions = new List<List<List<ThSprinklerDimGroup>>>();
            foreach (List<List<ThSprinklerDimGroup>> dims in dimensions)
            {
                List<List<ThSprinklerDimGroup>> t = new List<List<ThSprinklerDimGroup>>();
                for (int k = 0; k < dims.Count; k++)
                {
                    if (dims[k] != null)
                    {
                        dims[k].Sort((x, y) => ThCoordinateService.GetOriginalValue(pts[x.pt], isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(pts[y.pt], isXAxis)));
                        List<ThSprinklerDimGroup> one = new List<ThSprinklerDimGroup> { dims[k][0] };
                        for (int j = 1; j < dims[k].Count; j++)
                        {
                            if (pts[dims[k][j].pt].DistanceTo(pts[dims[k][j - 1].pt]) > 10) one.Add(dims[k][j]);
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
