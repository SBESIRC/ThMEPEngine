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
        public static bool CanMerge(List<Point3d> pts, List<int> dim1, List<int> dim2, bool isXAxis, double step, Matrix3d matrix, ThCADCoreNTSSpatialIndex walls, List<List<int>> anothercollinearation, double tolerance = 400.0)
        {
            //不考虑点与点之间的合并
            if (dim1.Count == 1 && dim2.Count == 1)
            {
                return false;
            }

            double det = ThCoordinateService.GetOriginalValue(pts[dim1[0]], !isXAxis) - ThCoordinateService.GetOriginalValue(pts[dim2[0]], !isXAxis);
            if (dim1.Count == 1 && dim2.Count != 1)
            {
                Line line = new Line(pts[dim2[0]], pts[dim2[dim2.Count - 1]]);
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
            }
            else if (dim2.Count == 1 && dim1.Count != 1)
            {
                Line line = new Line(pts[dim1[0]], pts[dim1[dim1.Count - 1]]);
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
            }

            if (Math.Abs(det) < tolerance)
            {
                //判断是否碰撞
                bool conflict1 = false;
                foreach (int i in dim1)
                {
                    if (dim2.Count == 1) break;
                    Line line = new Line(pts[dim2[0]], pts[dim2[dim2.Count - 1]]);
                    Point3d droppt = line.GetClosestPointTo(pts[i], true);
                    conflict1 = IsConflicted(droppt, pts[i], matrix, walls);
                    if (conflict1) break;
                    conflict1 = IsConflicted(droppt, pts[dim2[0]], matrix, walls);
                    if (conflict1) break;
                }
                bool conflict2 = false;
                foreach (int i in dim2)
                {
                    if (dim1.Count == 1) break;
                    Line line = new Line(pts[dim1[0]], pts[dim1[dim1.Count - 1]]);
                    Point3d droppt = line.GetClosestPointTo(pts[i], true);
                    conflict2 = IsConflicted(droppt, pts[i], matrix, walls);
                    if (conflict2) break;
                    conflict2 = IsConflicted(droppt, pts[dim1[0]], matrix, walls);
                    if (conflict2) break;
                }
                double distance1 = ThCoordinateService.GetOriginalValue(pts[dim1[0]], isXAxis) - ThCoordinateService.GetOriginalValue(pts[dim2[dim2.Count - 1]], isXAxis);
                double distance2 = ThCoordinateService.GetOriginalValue(pts[dim1[dim1.Count - 1]], isXAxis) - ThCoordinateService.GetOriginalValue(pts[dim2[0]], isXAxis);
                double distance3 = ThCoordinateService.GetOriginalValue(pts[dim1[0]], isXAxis) - ThCoordinateService.GetOriginalValue(pts[dim2[0]], isXAxis);
                double distance4 = ThCoordinateService.GetOriginalValue(pts[dim1[dim1.Count - 1]], isXAxis) - ThCoordinateService.GetOriginalValue(pts[dim2[dim2.Count - 1]], isXAxis);

                if (conflict1 || conflict2) return false;

                if ((45 < Math.Abs(distance1) && Math.Abs(distance1) < 1.5 * step) || (45 < Math.Abs(distance2) && Math.Abs(distance2) < 1.5 * step)) return true;
                else if (distance3 * distance4 < 0) return true;
            }

            return false;
        }

        //// 优先选转换后坐标系下值小的标注进行边缘合并，若无合并标注且点数小于最长的1/3，则选择最长标注
        //public static List<List<int>> MergeEdgeDimensions(List<Point3d> pts, List<List<List<int>>> group, out List<List<int>> dims, double step, bool isXAxis)
        //{
        //    List<List<int>> mergedDim = new List<List<int>>();
        //    dims = new List<List<int>>();
        //    for (int i = 0; i < group.Count; i++)
        //    {
        //        dims.Add(new List<int>());
        //    }
        //    bool[] isMerged = Enumerable.Repeat(false, group.Count).ToArray();
        //    for (int i = 0; i < group.Count; i++)
        //    {
        //        if (!isMerged[i])
        //        {
        //            isMerged[i] = true;
        //            List<int> minDim = GetMergedDimension(pts, group[i][0], ref dims, group, ref isMerged, step, isXAxis);
        //            if (minDim.Count > group[i][0].Count)
        //            {
        //                dims[i] = group[i][0];
        //                mergedDim.Add(minDim);
        //            }
        //            else
        //            {
        //                List<int> maxDim = GetMergedDimension(pts, group[i][group[i].Count - 1], ref dims, group, ref isMerged, step, isXAxis);
        //                if (maxDim.Count > group[i][group[i].Count - 1].Count)
        //                {
        //                    dims[i] = group[i][group[i].Count - 1];
        //                    mergedDim.Add(maxDim);
        //                }
        //                else
        //                {
        //                    List<int> longestDim = GetLongestLine(group[i]);
        //                    if (minDim.Count >= longestDim.Count / 2)
        //                    {
        //                        dims[i] = minDim;
        //                        mergedDim.Add(minDim);
        //                    }
        //                    else if (maxDim.Count >= longestDim.Count / 2)
        //                    {
        //                        dims[i] = maxDim;
        //                        mergedDim.Add(maxDim);
        //                    }
        //                    else
        //                    {
        //                        dims[i] = longestDim;
        //                        mergedDim.Add(longestDim);
        //                    }

        //                }

        //            }

        //        }

        //    }

        //    return mergedDim;
        //}

        public static void InsertPoints(ref List<Point3d> pts, ref List<List<List<int>>> Dimension, List<List<List<int>>> group, double step, bool IsxAxis, Matrix3d matrix, ThCADCoreNTSSpatialIndex walls, out List<int> ficpts)
        {
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
                                        Line line = new Line(pts[Dimension[i][k][0]], pts[Dimension[i][k][Dimension[i][k].Count - 1]]);
                                        Point3d DropPt = line.GetClosestPointTo(pts[Dimension[i][j][0]], true);
                                        if (IsConflicted(DropPt, pts[Dimension[i][j][0]], matrix, walls) || IsConflicted(DropPt, pts[Dimension[i][k][0]], matrix, walls) || DropPt.DistanceTo(line.GetClosestPointTo(pts[Dimension[i][j][0]], false)) > 1.5 * step) continue;
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

        private static ThSprinklerNetGroup MergeClaster(ThSprinklerNetGroup group)
        {
            for (int n = 0; n < group.XDimension.Count; n++)
            {
                List<int> xdim = group.XDimension[n];
                if (xdim.Count == 1)
                {
                    for (int i = 0; i < group.PtsGraph.Count; i++)
                    {
                        foreach (List<int> Collineation in group.YCollineationGroup[i])
                        {
                            if (Collineation.Contains(xdim[0]) && Collineation.Count != 1)
                            {
                                List<List<int>> t = new List<List<int>>();
                                foreach (int k in Collineation)
                                {
                                    List<int> line = group.XCollineationGroup[i].Where(x => x.Contains(k)).ToList()[0];
                                    t.Add(line);
                                }
                                bool[] isDimensioned = Enumerable.Repeat(false, t.Count).ToArray();
                                foreach (List<int> dim in group.XDimension)
                                {
                                    ThSprinklerDimensionOperateService.CheckDimensions(dim, t, ref isDimensioned);
                                }
                                List<double> distance = new List<double>();
                                for (int m = 0; m < Collineation.Count; m++)
                                {
                                    if (isDimensioned[m])
                                    {
                                        distance.Add(group.Pts[xdim[0]].DistanceTo(group.Pts[Collineation[m]]));
                                    }
                                }
                                for (int m = 0; m < Collineation.Count; m++)
                                {
                                    if ((int)group.Pts[xdim[0]].DistanceTo(group.Pts[Collineation[m]]) == (int)distance.Min())
                                    {
                                        group.XDimension[n].Add(Collineation[m]);
                                    }
                                }
                            }
                        }


                    }
                }

            }

            for (int n = 0; n < group.YDimension.Count; n++)
            {
                List<int> ydim = group.YDimension[n];
                if (ydim.Count == 1)
                {
                    for (int i = 0; i < group.PtsGraph.Count; i++)
                    {
                        foreach (List<int> Collineation in group.XCollineationGroup[i])
                        {
                            if (Collineation.Contains(ydim[0]) && Collineation.Count != 1)
                            {
                                List<List<int>> t = new List<List<int>>();
                                foreach (int k in Collineation)
                                {
                                    List<int> line = group.YCollineationGroup[i].Where(x => x.Contains(k)).ToList()[0];
                                    t.Add(line);
                                }
                                bool[] isDimensioned = Enumerable.Repeat(false, t.Count).ToArray();
                                foreach (List<int> dim in group.YDimension)
                                {
                                    ThSprinklerDimensionOperateService.CheckDimensions(dim, t, ref isDimensioned);
                                }
                                List<double> distance = new List<double>();
                                for (int m = 0; m < Collineation.Count; m++)
                                {
                                    if (isDimensioned[m])
                                    {
                                        distance.Add(group.Pts[ydim[0]].DistanceTo(group.Pts[Collineation[m]]));
                                    }
                                }
                                for (int m = 0; m < Collineation.Count; m++)
                                {
                                    if ((int)group.Pts[ydim[0]].DistanceTo(group.Pts[Collineation[m]]) == (int)distance.Min())
                                    {
                                        group.YDimension[n].Add(Collineation[m]);
                                    }
                                }
                            }
                        }


                    }
                }

            }
            return group;
        }

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

        public static List<int> GetMerged(ref List<Point3d> pts, List<int> currentdim1, List<List<int>> group, double step, bool isXAxis, ref Dictionary<int, bool> isMerged,ref List<int> FicPts, Matrix3d matrix, ThCADCoreNTSSpatialIndex walls, List<List<int>> anothercollinearation)
        {
            List<int> MergedDims = new List<int>();
            List<int> rMergedDims = new List<int>();
            double xmin = 0;
            double ymin = 0;
            isMerged[currentdim1[0]] = true;
            MergedDims.AddRange(currentdim1);
            xmin = pts[currentdim1[0]].X;
            ymin = pts[currentdim1[0]].Y;
            List<int> t = new List<int>();

            foreach (List<int> currentdim2 in group)
            {
                if (CanMerge(pts, currentdim1, currentdim2, isXAxis, step, matrix, walls, anothercollinearation) && !currentdim1.Equals(currentdim2))
                {
                    if (!isMerged[currentdim2[0]])
                    {
                        isMerged[currentdim2[0]] = true;
                        MergedDims.AddRange(currentdim2);
                        if (currentdim2.Count > currentdim1.Count)
                        {
                            xmin = pts[currentdim2[0]].X;
                            ymin = pts[currentdim2[0]].Y;
                        }
                        MergedDims = GetMerged(ref pts, MergedDims, group, step, isXAxis, ref isMerged, ref FicPts, matrix, walls, anothercollinearation);
                    }
                }
            }

            if (MergedDims.Count == currentdim1.Count) return MergedDims;
            else
            {
                if (isXAxis)
                {
                    foreach (int j in MergedDims)
                    {
                        pts.Add(new Point3d(pts[j].X, ymin, 0));
                        rMergedDims.Add(pts.Count - 1);
                        if (!currentdim1.Contains(j)) t.Add(pts.Count - 1);
                    }
                    var pts1 = pts;
                    rMergedDims = rMergedDims.OrderBy(p => pts1[p].X).ToList();
                }
                else
                {
                    foreach (int j in MergedDims)
                    {
                        pts.Add(new Point3d(xmin, pts[j].Y, 0));
                        rMergedDims.Add(pts.Count - 1);
                        if (!currentdim1.Contains(j)) t.Add(pts.Count - 1);
                    }
                    var pts1 = pts;
                    rMergedDims = rMergedDims.OrderBy(p => pts1[p].Y).ToList();
                }

                FicPts.AddRange(t);
                return rMergedDims;
            }
        }
    }
}
