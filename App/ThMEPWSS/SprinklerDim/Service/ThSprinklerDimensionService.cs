using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Diagnostics;
using ThMEPWSS.SprinklerDim.Model;
using static ThMEPWSS.SprinklerDim.Service.ThSprinklerDimensionOperateService;
using static ThMEPWSS.SprinklerDim.Service.ThSprinklerDimensionMergeService;

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

                List<List<int>> XDim = new List<List<int>>();
                List<List<int>> YDim = new List<List<int>>();

                // 边缘标注
                for (int i = 0; i < transNetList[j].PtsGraph.Count; i++)
                {
                    XDimension[i].Add(GetEdgeDimensions(transNetList[j].Pts, transNetList[j].YCollineationGroup[i], out var xdim, step, true));
                    XDim.Add(xdim);
                    YDimension[i].Add(GetEdgeDimensions(transNetList[j].Pts, transNetList[j].XCollineationGroup[i], out var ydim, step, false));
                    YDim.Add(ydim);
                }

                // 补充标注
                for (int i = 0; i < transNetList[j].PtsGraph.Count; i++)
                {
                    XDimension[i].AddRange(AddDimensions(transNetList[j], transNetList[j].YCollineationGroup[i], transNetList[j].XCollineationGroup[i], XDim[i], step, true, walls));
                    //XDim[i] = dimx;
                    //transNetList[j].Pts = Ptsx;
                    //FicPts.AddRange(ficPtsx);
                    YDimension[i].AddRange(AddDimensions(transNetList[j], transNetList[j].XCollineationGroup[i], transNetList[j].YCollineationGroup[i], YDim[i], step, false, walls));
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

                XDimension = DeletNullDimensions(pts, XDimension, true);
                YDimension = DeletNullDimensions(pts, YDimension, false);

                foreach (List<List<int>> xdim in XDimension) transNetList[j].XDimension.AddRange(xdim);
                foreach (List<List<int>> ydim in YDimension) transNetList[j].YDimension.AddRange(ydim);

                //合并能合并的标注
                transNetList[j].XDimension = MergeDimension(ref pts, transNetList[j].XDimension, step, true, out var FicPts1);
                transNetList[j].YDimension = MergeDimension(ref pts, transNetList[j].YDimension, step, false, out var FicPts2);
                FicPts[j].AddRange(FicPts1);
                FicPts[j].AddRange(FicPts2);

                transNetList[j].Pts = pts;

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

        //选取第一根标注线
        private static List<int> GetEdgeDimensions(List<Point3d> pts, List<List<int>> group, out List<int> dims, double step, bool isXAxis)
        {
            List<int> EdgeDim = new List<int>();
            dims = new List<int>();

            List<int> minDim = group[0];
            List<int> maxDim = group[group.Count - 1];
            List<int> longestDim = GetLongestLine(group);

            if (minDim.Count > longestDim.Count / 2.0 && minDim.Count > maxDim.Count)
            {
                dims = minDim;
                EdgeDim.AddRange(minDim);
            }
            else if (maxDim.Count > longestDim.Count / 2.0 && maxDim.Count > minDim.Count)
            {
                dims = longestDim;
                EdgeDim.AddRange(longestDim);
            }
            else if (maxDim.Count > longestDim.Count / 2.0 && maxDim.Count == minDim.Count)
            {
                if (!isXAxis)
                {
                    dims = minDim;
                    EdgeDim.AddRange(minDim);
                }
                else
                {
                    dims = maxDim;
                    EdgeDim.AddRange(maxDim);
                }
            }
            else
            {
                dims = longestDim;
                EdgeDim.AddRange(longestDim);
            }

            return EdgeDim;
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
                List<int> tDim1 = new List<int>();
                List<int> tDim2 = new List<int>();
                for (int i = 0; i < anotherCollineation.Count; i++)
                {
                    if (!isDimensioned[i])
                    {
                        List<int> tDim = GetLongestDimension(anotherCollineation[i], collineation, anotherCollineation, isDimensioned);
                        if (DeleteIsDimed(tDim, anotherCollineation, isDimensioned).Count > tDim2.Count)
                        {
                            if (tDim.Count == 1) 
                            {
                                if (resDims.Count != 0) tDim1.Add(ChooseNearestPt(group.Pts, anotherCollineation[i][0], anotherCollineation[i][anotherCollineation[i].Count - 1], resDims));
                                else tDim1.Add(ChooseNearestPt(group.Pts, anotherCollineation[i][0], anotherCollineation[i][anotherCollineation[i].Count - 1], new List<List<int>> { dim }));
                            }
                            else tDim1 = tDim;
                            tDim2 = DeleteIsDimed(tDim1, anotherCollineation, isDimensioned);
                        }

                    }
                    //else if (!isDimensioned[i] && GetNeareastDistance(group.Pts, dim, anotherCollineation[i]) <= 1.5 * step)
                    //{
                    //    Line dimline = new Line(group.Pts[dim[0]], group.Pts[dim[dim.Count - 1]]);
                    //    Point3d Dropfoot = dimline.GetClosestPointTo(group.Pts[anotherCollineation[i][0]], true);

                    //    if (IsConflicted(Dropfoot, group.Pts[anotherCollineation[i][0]], group.Transformer, walls) || IsConflicted(Dropfoot, group.Pts[dim[0]], group.Transformer, walls))
                    //    {
                    //        List<int> tDim = GetLongestDimension(anotherCollineation[i], collineation, anotherCollineation, isDimensioned);
                    //        if (DeleteIsDimed(tDim, anotherCollineation, isDimensioned, out var Prop).Count > tDim2.Count)
                    //        {
                    //            tDim1 = tDim;
                    //            tDim2 = DeleteIsDimed(tDim, anotherCollineation, isDimensioned, out uProp);
                    //        }
                    //    }
                    //    else
                    //    {
                    //        List<int> t1 = new List<int> { anotherCollineation[i][0] };
                    //        List<int> t2 = new List<int> { anotherCollineation[i][anotherCollineation[i].Count - 1] };
                    //        if (GetNeareastDistance(group.Pts, dim, t1) > GetNeareastDistance(group.Pts, dim, t2)) resDims.Add(t2);
                    //        else resDims.Add(t1);
                    //        isDimensioned[i] = true;
                    //    }
                    //}

                }
                double Prop = (double)tDim2.Count / tDim1.Count;
                if (Prop <= 0.5)
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
            }
            return resDims;
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
