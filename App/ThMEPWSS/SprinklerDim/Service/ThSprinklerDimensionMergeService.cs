using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Diagnostics;
using ThMEPWSS.SprinklerDim.Model;
using static ThMEPWSS.SprinklerDim.Service.ThSprinklerDimensionOperateService;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThSprinklerDimensionMergeService
    {
        /// <summary>
        /// 判断两根标注线是否能合并（PtsGraph之间）
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="dim1"></param>
        /// <param name="dim2"></param>
        /// <param name="isXAxis"></param>
        /// <param name="step"></param>
        /// <param name="matrix"></param>
        /// <param name="walls"></param>
        /// <param name="anothercollinearation"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static bool CanMerge(List<Point3d> pts, List<int> dim1, List<int> dim2, bool isXAxis, double step, Matrix3d matrix, ThCADCoreNTSSpatialIndex walls, List<List<int>> anothercollinearation, double tolerance = 400.0)
        {
            Vector3d xAxis = new Vector3d();
            if (isXAxis) xAxis = new Vector3d(1, 0, 0);
            else xAxis = new Vector3d(0, 1, 0);

            //不考虑点与点之间的合并
            if (dim1.Count == 1 && dim2.Count == 1)
                return false;
            else if (dim1.Count == 0 || dim2.Count == 0)
                return false;

            double det = Math.Abs(ThCoordinateService.GetOriginalValue(pts[dim1[0]], !isXAxis) - ThCoordinateService.GetOriginalValue(pts[dim2[0]], !isXAxis));
            if (dim1.Count == 1 && dim2.Count != 1)
            {
                Line line = new Line(pts[dim2[0]], pts[dim2[0]] + xAxis * 100);
                Point3d droppt = line.GetClosestPointTo(pts[dim1[0]], true);
                List<int> t = anothercollinearation.Where(p => p.Contains(dim1[0])).ToList()[0];
                if (t.Count == 1) tolerance = 1.0 * step;                                        //仅仅当xy方向都为单点的时候放大tolerance
                foreach (int j in t)
                {
                    if (droppt.DistanceTo(pts[j]) < det)
                    {
                        det = droppt.DistanceTo(pts[j]);
                    }
                }
                double d1 = ThCoordinateService.GetOriginalValue(droppt, !isXAxis) - ThCoordinateService.GetOriginalValue(pts[t[0]], !isXAxis);
                double d2 = ThCoordinateService.GetOriginalValue(droppt, !isXAxis) - ThCoordinateService.GetOriginalValue(pts[t[t.Count - 1]], !isXAxis);
                if (d1 * d2 < 0) det = 0;
            }
            else if (dim2.Count == 1 && dim1.Count != 1)
            {
                Line line = new Line(pts[dim1[0]], pts[dim1[0]] + xAxis * 100);
                Point3d droppt = line.GetClosestPointTo(pts[dim2[0]], true);
                List<int> t = anothercollinearation.Where(p => p.Contains(dim2[0])).ToList()[0];
                if (t.Count == 1) tolerance = 1.0 * step;                                        //仅仅当xy方向都为单点的时候放大tolerance
                foreach (int j in t)
                {
                    if (droppt.DistanceTo(pts[j]) < det)
                    {
                        det = droppt.DistanceTo(pts[j]);
                    }
                }
                double d1 = ThCoordinateService.GetOriginalValue(droppt, !isXAxis) - ThCoordinateService.GetOriginalValue(pts[t[0]], !isXAxis);
                double d2 = ThCoordinateService.GetOriginalValue(droppt, !isXAxis) - ThCoordinateService.GetOriginalValue(pts[t[t.Count - 1]], !isXAxis);
                if (d1 * d2 < 0) det = 0;
            }

            if (det < tolerance)
            {
                //判断是否碰撞
                foreach (int i in dim1)
                {
                    if (dim2.Count == 1) break;
                    Line line = new Line(pts[dim2[0]], pts[dim2[0]] + xAxis * 100);
                    Point3d droppt = line.GetClosestPointTo(pts[i], true);
                    if (IsConflicted(droppt, pts[i], matrix, walls) || IsConflicted(droppt, pts[dim2[0]], matrix, walls) || IsConflicted(droppt, pts[dim2[dim2.Count - 1]], matrix, walls))
                        return false;
                }
                foreach (int i in dim2)
                {
                    if (dim1.Count == 1) break;
                    Line line = new Line(pts[dim1[0]], pts[dim1[0]] + xAxis * 100);
                    Point3d droppt = line.GetClosestPointTo(pts[i], true);
                    if (IsConflicted(droppt, pts[i], matrix, walls) || IsConflicted(droppt, pts[dim1[0]], matrix, walls) || IsConflicted(droppt, pts[dim1[dim1.Count - 1]], matrix, walls))
                        return false;
                }
                double distance1 = ThCoordinateService.GetOriginalValue(pts[dim1[0]], isXAxis) - ThCoordinateService.GetOriginalValue(pts[dim2[dim2.Count - 1]], isXAxis);
                double distance2 = ThCoordinateService.GetOriginalValue(pts[dim1[dim1.Count - 1]], isXAxis) - ThCoordinateService.GetOriginalValue(pts[dim2[0]], isXAxis);
                double distance3 = ThCoordinateService.GetOriginalValue(pts[dim1[0]], isXAxis) - ThCoordinateService.GetOriginalValue(pts[dim2[0]], isXAxis);
                double distance4 = ThCoordinateService.GetOriginalValue(pts[dim1[dim1.Count - 1]], isXAxis) - ThCoordinateService.GetOriginalValue(pts[dim2[dim2.Count - 1]], isXAxis);

                if ((Math.Abs(distance1) < 1.5 * step || Math.Abs(distance2) < 1.5 * step) && Math.Min(Math.Abs(distance1), Math.Abs(distance2)) > 45) return true;
                else if (distance3 * distance4 < 0) return true;
            }

            return false;

        }

        /// <summary>
        /// PtsGraph内合并能合并的点
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="Dimension"></param>
        /// <param name="group"></param>
        /// <param name="step"></param>
        /// <param name="IsxAxis"></param>
        /// <param name="matrix"></param>
        /// <param name="walls"></param>
        /// <param name="ficpts"></param>
        public static void InsertPoints(ref List<Point3d> pts, ref List<List<List<int>>> Dimension, List<List<List<int>>> group, double step, bool IsxAxis, Matrix3d matrix, ThCADCoreNTSSpatialIndex walls, out List<int> ficpts)
        {
            Vector3d xAxis = new Vector3d();
            if (IsxAxis) xAxis = new Vector3d(1, 0, 0);
            else xAxis = new Vector3d(0, 1, 0);

            ficpts = new List<int>();
            for (int i = 0; i < Dimension.Count - 1; i++)
            {
                if (Dimension[i].Count == 1) continue;
                for (int j = 0; j < Dimension[i].Count; j++)
                {
                    if (Dimension[i][j].Count == 1)
                    {
                        for (int k = 0; k < Dimension[i].Count; k++)
                        {
                            if (Dimension[i][k] == null) continue;
                            else
                            {
                                if (k == j || Dimension[i][k].Count == 1) continue;
                                else
                                {
                                    List<int> t = new List<int>();
                                    int index = Dimension[i][j][0];
                                    t = group[i].Where(p => p.Contains(index)).ToList()[0];
                                    if (GetNeareastDistance(pts, Dimension[i][k], t) < 1.5 * step && GetNeareastDistance(pts, Dimension[i][k], t) > 45)
                                    {
                                        Line line = new Line(pts[Dimension[i][k][0]], pts[Dimension[i][k][0]] + 100 * xAxis);
                                        Point3d DropPt = line.GetClosestPointTo(pts[Dimension[i][j][0]], true);
                                        if (IsConflicted(DropPt, pts[Dimension[i][j][0]], matrix, walls) || IsConflicted(DropPt, pts[Dimension[i][k][0]], matrix, walls) || DropPt.DistanceTo(line.GetClosestPointTo(pts[Dimension[i][j][0]], false)) > 1.5 * step) continue;
                                        else
                                        {
                                            if (DropPt.DistanceTo(line.GetClosestPointTo(pts[Dimension[i][j][0]], false)) < 45)
                                            {
                                                Dimension[i][j] = null;
                                                continue;
                                            }
                                            else
                                            {
                                                pts.Add(DropPt);
                                                Dimension[i][k].Add(pts.Count - 1);
                                                ficpts.Add(pts.Count - 1);
                                                Dimension[i][j] = null;
                                                List<Point3d> pts1 = pts;
                                                if (IsxAxis) Dimension[i][k] = Dimension[i][k].OrderBy(p => pts1[p].X).ToList();
                                                else
                                                {
                                                    Dimension[i][k] = Dimension[i][k].OrderBy(p => pts1[p].Y).ToList();
                                                }
                                                break;
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

        /// <summary>
        /// PtsGraph之间合并能合并的标注
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="group"></param>
        /// <param name="step"></param>
        /// <param name="isXAxis"></param>
        /// <param name="FicPts"></param>
        /// <param name="matrix"></param>
        /// <param name="walls"></param>
        /// <param name="anothercollinearation"></param>
        /// <returns></returns>
        public static List<List<int>> MergeDimension(ref List<Point3d> pts, List<List<int>> group, double step, bool isXAxis, out List<int> FicPts, Matrix3d matrix, ThCADCoreNTSSpatialIndex walls, List<List<int>> anothercollinearation)
        {
            List<int> FicPts1 = new List<int>();
            Dictionary<int, bool> isMerged = new Dictionary<int, bool>();
            foreach (List<int> j in group) isMerged.Add(j[0], false);
            List<List<int>> MergedDimensions = new List<List<int>>();
            for (int i = 0; i < group.Count; i++)
            {
                if (!isMerged[group[i][0]])
                {
                    MergedDimensions.Add(GetMerged(ref pts, group[i], group, step, isXAxis, ref isMerged, ref FicPts1, matrix, walls, anothercollinearation));
                }
            }
            FicPts = FicPts1;
            return MergedDimensions;
        }

        /// <summary>
        /// PtsGraph之间合并操作函数
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="currentdim1"></param>
        /// <param name="group"></param>
        /// <param name="step"></param>
        /// <param name="isXAxis"></param>
        /// <param name="isMerged"></param>
        /// <param name="FicPts"></param>
        /// <param name="matrix"></param>
        /// <param name="walls"></param>
        /// <param name="anothercollinearation"></param>
        /// <returns></returns>
        public static List<int> GetMerged(ref List<Point3d> pts, List<int> currentdim1, List<List<int>> group, double step, bool isXAxis, ref Dictionary<int, bool> isMerged,ref List<int> FicPts, Matrix3d matrix, ThCADCoreNTSSpatialIndex walls, List<List<int>> anothercollinearation)
        {
            List<int> MergedDims = currentdim1;
            isMerged[currentdim1[0]] = true;
            List<Point3d> pts1 = pts;

            foreach (List<int> currentdim2 in group)
            {
                if (CanMerge(pts, currentdim1, currentdim2, isXAxis, step, matrix, walls, anothercollinearation) && !currentdim1.Equals(currentdim2))
                {
                    if (!isMerged[currentdim2[0]])
                    {
                        isMerged[currentdim2[0]] = true;
                        if (currentdim2.Count > currentdim1.Count)
                        {
                            MergedDims = currentdim2;
                            foreach (int j in currentdim1)
                            {
                                if (isXAxis)
                                {
                                    pts.Add(new Point3d(pts[j].X, pts[currentdim2[0]].Y, 0));
                                    MergedDims.Add(pts.Count - 1);
                                    FicPts.Add(pts.Count - 1);
                                }
                                else
                                {
                                    pts.Add(new Point3d(pts[currentdim2[0]].X, pts[j].Y, 0));
                                    MergedDims.Add(pts.Count - 1);
                                    FicPts.Add(pts.Count - 1);
                                }
                            }
                        }
                        else
                        {
                            bool isDimed = false;
                            foreach (int p in currentdim1)
                            {
                                if (Math.Abs(ThCoordinateService.GetOriginalValue(pts1[p], isXAxis) - ThCoordinateService.GetOriginalValue(pts1[currentdim2[0]], isXAxis)) < 45)
                                {
                                    isDimed = true;
                                    break;
                                }
                            }
                            if (currentdim2.Count == 1 && isDimed) continue;
                            else
                            {
                                foreach (int j in currentdim2)
                                {
                                    if (isXAxis)
                                    {
                                        pts.Add(new Point3d(pts[j].X, pts[currentdim1[0]].Y, 0));
                                        MergedDims.Add(pts.Count - 1);
                                        FicPts.Add(pts.Count - 1);
                                    }
                                    else
                                    {
                                        pts.Add(new Point3d(pts[currentdim1[0]].X, pts[j].Y, 0));
                                        MergedDims.Add(pts.Count - 1);
                                        FicPts.Add(pts.Count - 1);
                                    }
                                }
                            }

                        }
                        MergedDims = GetMerged(ref pts, MergedDims, group, step, isXAxis, ref isMerged, ref FicPts, matrix, walls, anothercollinearation);
                        break;
                    }
                }
            }
            return MergedDims;
            }
        }
    
}
