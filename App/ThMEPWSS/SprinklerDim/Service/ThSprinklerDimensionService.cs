using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Diagnostics;
using ThMEPWSS.SprinklerDim.Model;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThSprinklerDimensionService
    {

        public static void GenerateDimension(List<ThSprinklerNetGroup> transNetList, double step, string printTag, List<Polyline> walls)
        {
            List<List<int>> FicPts = new List<List<int>>();
            for (int j = 0; j < transNetList.Count; j++)
            {
                FicPts.Add(new List<int>());
                List<List<List<int>>> XDimension = new List<List<List<int>>>();
                List<List<List<int>>> YDimension = new List<List<List<int>>>();
                for (int i = 0; i < transNetList[j].PtsGraph.Count; i++)
                {
                    XDimension.Add(new List<List<int>>());
                    YDimension.Add(new List<List<int>>());
                }
                // 边缘标注
                transNetList[j].XDimension.AddRange(GetEdgeDimensions(transNetList[j].Pts, transNetList[j].YCollineationGroup, out var XDim, step, true));
                transNetList[j].YDimension.AddRange(GetEdgeDimensions(transNetList[j].Pts, transNetList[j].XCollineationGroup, out var YDim, step, false));

                // 补充标注
                for (int i = 0; i < transNetList[j].PtsGraph.Count; i++)
                {
                    XDimension[i] = AddDimensions(transNetList[j], transNetList[j].YCollineationGroup[i], transNetList[j].XCollineationGroup[i], XDim[i], step, true, walls);
                    //XDim[i] = dimx;
                    //transNetList[j].Pts = Ptsx;
                    //FicPts.AddRange(ficPtsx);
                    YDimension[i] = AddDimensions(transNetList[j], transNetList[j].XCollineationGroup[i], transNetList[j].YCollineationGroup[i], YDim[i], step, false, walls);
                    //YDim[i] = dimy;
                    //transNetList[j].Pts = Ptsy;
                    //FicPts.AddRange(ficPtsy);
                }

                List<Point3d> pts = transNetList[j].Pts;

                //插入点
                InsertPoints(ref pts, ref XDimension, step, true, transNetList[j].Transformer, walls, out var ficptsx);
                InsertPoints(ref pts, ref YDimension, step, false, transNetList[j].Transformer, walls, out var ficptsy);
                FicPts[j].AddRange(ficptsx);
                FicPts[j].AddRange(ficptsy);
                transNetList[j].Pts = pts;


                foreach (List<List<int>> xdim in XDimension) transNetList[j].XDimension.AddRange(xdim);
                foreach (List<List<int>> ydim in YDimension) transNetList[j].YDimension.AddRange(ydim);

                // transNetList[j] = MergeClaster(transNetList[j]);
            }


            // 打印标注
            for (int i = 0; i < transNetList.Count; i++)
            {
                List<Line> dimensions = Print(transNetList[i], out var ptsx, out var ptsy);
                DrawUtils.ShowGeometry(dimensions, string.Format("SSS-Dimension-{0}-{1}", printTag, i), 1, 35);
                ptsx.ForEach(p => DrawUtils.ShowGeometry(p, string.Format("SSS-Dimension-{0}-{1}-clasterx", printTag, i), 2, 35, 300));
                ptsy.ForEach(p => DrawUtils.ShowGeometry(p, string.Format("SSS-Dimension-{0}-{1}-clastery", printTag, i), 3, 35));
                List<Point3d> ds = ThChangeCoordinateService.MakeTransformation(transNetList[i].Pts, transNetList[i].Transformer.Inverse());
                FicPts[i].ForEach(p => DrawUtils.ShowGeometry(ds[p], string.Format("SSS-Dimension-{0}-{1}-ficPoints", printTag, i), 0, 35));

            }

        }



        // 优先选转换后坐标系下值小的标注进行边缘合并，若无合并标注且点数小于最长的1/3，则选择最长标注
        private static List<List<int>> MergeEdgeDimensions(List<Point3d> pts, List<List<List<int>>> group, out List<List<int>> dims, double step, bool isXAxis)
        {
            List<List<int>> mergedDim = new List<List<int>>();
            dims = new List<List<int>>();
            for (int i = 0; i < group.Count; i++)
            {
                dims.Add(new List<int>());
            }
            bool[] isMerged = Enumerable.Repeat(false, group.Count).ToArray();
            for (int i = 0; i < group.Count; i++)
            {
                if (!isMerged[i])
                {
                    isMerged[i] = true;
                    List<int> minDim = GetMergedDimension(pts, group[i][0], ref dims, group, ref isMerged, step, isXAxis);
                    if (minDim.Count > group[i][0].Count)
                    {
                        dims[i] = group[i][0];
                        mergedDim.Add(minDim);
                    }
                    else
                    {
                        List<int> maxDim = GetMergedDimension(pts, group[i][group[i].Count - 1], ref dims, group, ref isMerged, step, isXAxis);
                        if (maxDim.Count > group[i][group[i].Count - 1].Count)
                        {
                            dims[i] = group[i][group[i].Count - 1];
                            mergedDim.Add(maxDim);
                        }
                        else
                        {
                            List<int> longestDim = GetLongestLine(group[i]);
                            if (minDim.Count >= longestDim.Count / 3)
                            {
                                dims[i] = minDim;
                                mergedDim.Add(minDim);
                            }
                            else if (maxDim.Count >= longestDim.Count / 3)
                            {
                                dims[i] = maxDim;
                                mergedDim.Add(maxDim);
                            }
                            else
                            {
                                dims[i] = longestDim;
                                mergedDim.Add(longestDim);
                            }

                        }

                    }

                }

            }

            return mergedDim;
        }


        private static List<List<int>> GetEdgeDimensions(List<Point3d> pts, List<List<List<int>>> group, out List<List<int>> dims, double step, bool isXAxis)
        {
            List<List<int>> EdgeDim = new List<List<int>>();
            dims = new List<List<int>>();
            for (int i = 0; i < group.Count; i++)
            {
                dims.Add(new List<int>());
            }

            for (int i = 0; i < group.Count; i++)
            {
                List<int> minDim = group[i][0];
                List<int> maxDim = group[i][group[i].Count - 1];
                List<int> longestDim = GetLongestLine(group[i]);

                if (!isXAxis)
                {
                    if (minDim.Count > longestDim.Count / 2)
                    {
                        dims[i] = minDim;
                        EdgeDim.Add(minDim);
                    }
                    else
                    {
                        dims[i] = longestDim;
                        EdgeDim.Add(longestDim);
                    }
                }
                else
                {
                    if (maxDim.Count > longestDim.Count / 2)
                    {
                        dims[i] = maxDim;
                        EdgeDim.Add(maxDim);
                    }
                    else
                    {
                        dims[i] = longestDim;
                        EdgeDim.Add(longestDim);
                    }
                }
            }

            return EdgeDim;
        }

        private static List<int> GetMergedDimension(List<Point3d> pts, List<int> currentDim, ref List<List<int>> dims, List<List<List<int>>> group, ref bool[] isMerged, double step, bool isXAxis)
        {
            List<int> mergedDim = new List<int>();
            mergedDim.AddRange(currentDim);
            for (int i = 0; i < group.Count; i++)
            {
                if (!isMerged[i])
                {
                    if (CanMerge(pts, mergedDim, group[i][0], isXAxis, step))
                    {
                        isMerged[i] = true;
                        dims[i] = group[i][0];
                        mergedDim.AddRange(group[i][0]);
                        return GetMergedDimension(pts, mergedDim, ref dims, group, ref isMerged, step, isXAxis);
                    }
                    else if (CanMerge(pts, mergedDim, group[i][group[i].Count - 1], isXAxis, step))
                    {
                        isMerged[i] = true;
                        dims[i] = group[i][group[i].Count - 1];
                        mergedDim.AddRange(group[i][group[i].Count - 1]);
                        return GetMergedDimension(pts, mergedDim, ref dims, group, ref isMerged, step, isXAxis);
                    }
                }
            }
            return mergedDim;
        }

        private static bool CanMerge(List<Point3d> pts, List<int> dim1, List<int> dim2, bool isXAxis, double step, double tolerance = 45.0)
        {
            double det = ThChangeCoordinateService.GetOriginalValue(pts[dim1[0]], !isXAxis) - ThChangeCoordinateService.GetOriginalValue(pts[dim2[0]], !isXAxis);
            if (Math.Abs(det) < tolerance)
            {
                double distance1 = ThChangeCoordinateService.GetOriginalValue(pts[dim1[0]], isXAxis) - ThChangeCoordinateService.GetOriginalValue(pts[dim2[dim2.Count - 1]], isXAxis);
                double distance2 = ThChangeCoordinateService.GetOriginalValue(pts[dim1[dim1.Count - 1]], isXAxis) - ThChangeCoordinateService.GetOriginalValue(pts[dim2[0]], isXAxis);

                if ((tolerance < Math.Abs(distance1) && Math.Abs(distance1) < 1.5 * step) || (tolerance < Math.Abs(distance2) && Math.Abs(distance2) < 1.5 * step)) return true;

            }

            return false;
        }

        private static List<int> GetLongestLine(List<List<int>> collineationList)
        {
            int longestLineIndex = 0;
            for (int i = 1; i < collineationList.Count; i++)
            {
                if (collineationList[i].Count > collineationList[longestLineIndex].Count)
                    longestLineIndex = i;
            }

            return collineationList[longestLineIndex];
        }

        private static bool IsConflicted(Point3d pts1, Point3d pts2, Matrix3d matrix, List<Polyline> walls)
        {
            List<Point3d> pts = new List<Point3d> { pts1, pts2 };
            pts = ThChangeCoordinateService.MakeTransformation(pts, matrix.Inverse());
            Line line = new Line(pts[0], pts[pts.Count - 1]);

            return ThSprinklerDimConflictService.IsConflicted(line, walls);
        }


        private static double GetNeareastDistance(List<Point3d> pts, List<int> dim, List<int> isNotDimensioned)
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

        private static void InsertPoints(ref List<Point3d> pts, ref List<List<List<int>>> Dimension, double step, bool IsxAxis, Matrix3d matrix, List<Polyline> walls, out List<int> ficpts)
        {
            List<List<List<int>>> dimensions = new List<List<List<int>>>();
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
                                if (k == j|| Dimension[i][k].Count == 1) continue;
                                else
                                {
                                    if (GetNeareastDistance(pts, Dimension[i][k], Dimension[i][j]) < 2 * step)
                                    {
                                        Line line = new Line(pts[Dimension[i][k][0]], pts[Dimension[i][k][Dimension[i][k].Count - 1]]);
                                        Point3d DropPt = line.GetClosestPointTo(pts[Dimension[i][j][0]], true);
                                        if (IsConflicted(DropPt, pts[Dimension[i][j][0]], matrix, walls)) continue;
                                        else
                                        {
                                            pts.Add(line.GetClosestPointTo(pts[Dimension[i][j][0]], true));
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

        public static List<List<int>> SeperateLine(List<Point3d> pts, List<int> line, bool isXAxis, double step)
        {
            line.Sort((x, y) => ThChangeCoordinateService.GetOriginalValue(pts[x], isXAxis).CompareTo(ThChangeCoordinateService.GetOriginalValue(pts[y], isXAxis)));
            List<List<int>> lines = new List<List<int>>();

            List<int> one = new List<int> { line[0] };
            for (int i = 1; i < line.Count; i++)
            {
                int iPtIndex = one[one.Count - 1];
                int jPtIndex = line[i];
                if (ThChangeCoordinateService.GetOriginalValue(pts[jPtIndex], isXAxis) - ThChangeCoordinateService.GetOriginalValue(pts[iPtIndex], isXAxis) > 2 * step)
                {
                    lines.Add(one);
                    one = new List<int> { jPtIndex };
                }
            }
            lines.Add(one);

            return lines;
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
                                    CheckDimensions(dim, t, ref isDimensioned);
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
                                    CheckDimensions(dim, t, ref isDimensioned);
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

        private static List<List<int>> AddDimensions(ThSprinklerNetGroup group, List<List<int>> collineation, List<List<int>> anotherCollineation, List<int> dim, double step, bool isXAxis, List<Polyline> walls)
        {
            bool[] isDimensioned = Enumerable.Repeat(false, anotherCollineation.Count).ToArray();
            List<List<int>> resDims = new List<List<int>>();

            CheckDimensions(dim, anotherCollineation, ref isDimensioned);

            while (isDimensioned.Contains(false))
            {
                double uProp = 0;
                List<int> tDim1 = new List<int>();
                List<int> tDim2 = new List<int>();
                for (int i = 0; i < anotherCollineation.Count; i++)
                {
                    if (!isDimensioned[i] && GetNeareastDistance(group.Pts, dim, anotherCollineation[i]) > 1.5 * step)
                    {
                        List<int> tDim = GetLongestDimension(anotherCollineation[i], collineation, anotherCollineation, isDimensioned);
                        if (DeleteIsDimed(tDim, anotherCollineation, isDimensioned, out var Prop).Count > tDim2.Count)
                        {
                            tDim1 = tDim;
                            tDim2 = DeleteIsDimed(tDim, anotherCollineation, isDimensioned, out uProp);
                        }

                    }
                    else if (!isDimensioned[i] && GetNeareastDistance(group.Pts, dim, anotherCollineation[i]) <= 1.5 * step)
                    {
                        Line dimline = new Line(group.Pts[dim[0]], group.Pts[dim[dim.Count - 1]]);
                        Point3d Dropfoot = dimline.GetClosestPointTo(group.Pts[anotherCollineation[i][0]], true);

                        if (IsConflicted(Dropfoot, group.Pts[anotherCollineation[i][0]], group.Transformer, walls))
                        {
                            List<int> tDim = GetLongestDimension(anotherCollineation[i], collineation, anotherCollineation, isDimensioned);
                            if (DeleteIsDimed(tDim, anotherCollineation, isDimensioned, out var Prop).Count > tDim2.Count)
                            {
                                tDim1 = tDim;
                                tDim2 = DeleteIsDimed(tDim, anotherCollineation, isDimensioned, out uProp);
                            }
                        }
                        else
                        {
                            List<int> t1 = new List<int> { anotherCollineation[i][0] };
                            List<int> t2 = new List<int> { anotherCollineation[i][anotherCollineation[i].Count - 1] };
                            if (GetNeareastDistance(group.Pts, dim, t1) > GetNeareastDistance(group.Pts, dim, t2)) resDims.Add(t2);
                            else resDims.Add(t1);
                            isDimensioned[i] = true;
                        }
                    }

                }
                if (uProp <= 0.5)
                {
                    if (tDim2.Count != 0)
                    {
                        resDims.AddRange(SeperateLine(group.Pts, tDim2, isXAxis, step));
                        CheckDimensions(tDim2, anotherCollineation, ref isDimensioned);
                    }
                }
                else
                {
                    if (tDim1.Count != 0)
                    {
                        resDims.Add(tDim1);
                        CheckDimensions(tDim1, anotherCollineation, ref isDimensioned);
                    }
                }
                if (tDim1.Count != 1 && tDim1.Count != 0) dim = tDim1;
            }
            return resDims;
        }

        private static List<int> DeleteIsDimed(List<int> tdim, List<List<int>> anotherCollineation, bool[] isDimensioned, out double Prop)
        {
            List<int> dims = new List<int>();
            for (int i = 0; i < anotherCollineation.Count; i++)
            {
                for (int j = 0; j < tdim.Count; j++)
                {
                    if (anotherCollineation[i].Contains(tdim[j]) && !isDimensioned[i]) dims.Add(tdim[j]);
                }
            }
            Prop = dims.Count / (double)tdim.Count;
            return dims;
        }

        private static void CheckDimensions(List<int> dim, List<List<int>> anotherCollineation, ref bool[] isDimensioned)
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

        private static List<int> GetLongestDimension(List<int> undimensionedLine, List<List<int>> collineation, List<List<int>> anoCollineation, bool[] isDimensioned)
        {
            int len = 0;
            List<int> Dim = new List<int>();
            foreach (int i in undimensionedLine)
            {
                List<int> line = collineation.Where(x => x.Contains(i)).ToList()[0];
                List<int> line1 = DeleteIsDimed(line, anoCollineation, isDimensioned, out var prop);
                if (line1.Count > len)
                {
                    len = line1.Count;
                    Dim = line;
                }
            }

            return Dim;
        }

        private static List<Line> Print(ThSprinklerNetGroup group, out List<Point3d> Clatersx, out List<Point3d> Clatersy)
        {
            Clatersx = new List<Point3d>();
            Clatersy = new List<Point3d>();
            List<Point3d> transPts = group.Pts;
            List<Point3d> pts = ThChangeCoordinateService.MakeTransformation(transPts, group.Transformer.Inverse());
            List<Line> lines = new List<Line>();

            foreach (List<int> dim in group.XDimension)
            {
                if (dim == null) continue;
                else if (dim.Count == 1)
                {
                    Clatersx.Add(pts[dim[0]]);
                }
                else
                {
                    for (int i = 0; i < dim.Count - 1; i++)
                    {
                        lines.Add(new Line(pts[dim[i]], pts[dim[i + 1]]));
                    }
                }
            }

            foreach (List<int> dim in group.YDimension)
            {
                if (dim == null) continue;
                else if (dim.Count == 1)
                {
                    Clatersy.Add(pts[dim[0]]);
                }
                else
                {
                    for (int i = 0; i < dim.Count - 1; i++)
                    {
                        lines.Add(new Line(pts[dim[i]], pts[dim[i + 1]]));
                    }
                }
            }
            return lines;
        }

    }
}
