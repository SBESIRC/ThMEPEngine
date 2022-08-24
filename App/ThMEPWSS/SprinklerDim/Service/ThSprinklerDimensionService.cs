using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Diagnostics;
using ThMEPWSS.SprinklerDim.Model;
using static ThMEPWSS.SprinklerDim.Service.ThSprinklerDimensionOperateService;
using static ThMEPWSS.SprinklerDim.Service.ThSprinklerDimensionMergeService;
using ThCADCore.NTS;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThSprinklerDimensionService
    {
        /// <summary>
        /// 选取标注位主函数
        /// </summary>
        /// <param name="transNetList"></param>
        /// <param name="step"></param>
        /// <param name="printTag"></param>
        /// <param name="walls"></param>
        public static void GenerateDimension(List<ThSprinklerNetGroup> transNetList, double step, string printTag, ThCADCoreNTSSpatialIndex walls)
        {
            // 记录虚拟点的index
            List<List<int>> FicPts = new List<List<int>>();

            //处理每一个netlist
            for (int j = 0; j < transNetList.Count; j++)
            {
                FicPts.Add(new List<int>());
                List<List<List<ThSprinklerDimGroup>>> XDimension = new List<List<List<ThSprinklerDimGroup>>>();
                List<List<List<ThSprinklerDimGroup>>> YDimension = new List<List<List<ThSprinklerDimGroup>>>();
                for (int i = 0; i < transNetList[j].PtsGraph.Count; i++)
                {
                    XDimension.Add(new List<List<ThSprinklerDimGroup>>());
                    YDimension.Add(new List<List<ThSprinklerDimGroup>>());
                }

                List<List<int>> XDim = new List<List<int>>();
                List<List<int>> YDim = new List<List<int>>();

                // 边缘标注
                for (int i = 0; i < transNetList[j].PtsGraph.Count; i++)
                {
                    XDimension[i].Add(GetEdgeDimensions(transNetList[j].Pts, transNetList[j].YCollineationGroup[i], transNetList[j].XCollineationGroup[i], out var xdim, true));
                    XDim.Add(xdim);
                    YDimension[i].Add(GetEdgeDimensions(transNetList[j].Pts, transNetList[j].XCollineationGroup[i], transNetList[j].YCollineationGroup[i], out var ydim, false));
                    YDim.Add(ydim);
                }

                // 补充标注
                for (int i = 0; i < transNetList[j].PtsGraph.Count; i++)
                {
                    XDimension[i].AddRange(AddDimensions(transNetList[j], transNetList[j].YCollineationGroup[i], transNetList[j].XCollineationGroup[i], XDim[i], step, true, walls));
                    YDimension[i].AddRange(AddDimensions(transNetList[j], transNetList[j].XCollineationGroup[i], transNetList[j].YCollineationGroup[i], YDim[i], step, false, walls));
                }

                //将虚拟点存入pts中
                List<Point3d> pts = transNetList[j].Pts;

                //PtsGraph内的标注合并
                InsertPoints(ref pts, ref XDimension, step, true, transNetList[j].Transformer, walls, out var ficptsx);
                InsertPoints(ref pts, ref YDimension, step, false, transNetList[j].Transformer, walls, out var ficptsy);

                //记录虚拟点的下标      
                FicPts[j].AddRange(ficptsx);
                FicPts[j].AddRange(ficptsy);

                //去重和去空
                XDimension = DeletNullDimensions(pts, XDimension, true);
                YDimension = DeletNullDimensions(pts, YDimension, false);

                foreach (List<List<ThSprinklerDimGroup>> xdim in XDimension) transNetList[j].XDimension.AddRange(xdim);
                foreach (List<List<ThSprinklerDimGroup>> ydim in YDimension) transNetList[j].YDimension.AddRange(ydim);

                //PtsGraph之间的标注合并
                transNetList[j].XDimension = MergeDimension(ref pts, transNetList[j].XDimension, step, true, out var FicPts1, transNetList[j].Transformer, walls);
                transNetList[j].YDimension = MergeDimension(ref pts, transNetList[j].YDimension, step, false, out var FicPts2, transNetList[j].Transformer, walls);
                FicPts[j].AddRange(FicPts1);
                FicPts[j].AddRange(FicPts2);
                transNetList[j].Pts = pts;
            }

            // 打印标注test
            for (int i = 0; i < transNetList.Count; i++)
            {
                List<Line> dimensions = Print(transNetList[i], out var ptsx, out var ptsy);
                DrawUtils.ShowGeometry(dimensions, string.Format("SSS-Dimension-{0}-{1}", printTag, i), 1, 35);
                ptsx.ForEach(p => DrawUtils.ShowGeometry(p, string.Format("SSS-Dimension-{0}-{1}-clasterx", printTag, i), 2, 35, 300));
                ptsy.ForEach(p => DrawUtils.ShowGeometry(p, string.Format("SSS-Dimension-{0}-{1}-clastery", printTag, i), 3, 35));
                List<Point3d> ds = ThCoordinateService.MakeTransformation(transNetList[i].Pts, transNetList[i].Transformer.Inverse());
                FicPts[i].ForEach(p => DrawUtils.ShowGeometry(ds[p], string.Format("SSS-Dimension-{0}-{1}-ficPoints", printTag, i), 0, 35));

            }

        }

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

        /// <summary>
        /// 获取第一根标注位
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="collinearation"></param>
        /// <param name="anothercollinearation"></param>
        /// <param name="dims"></param>
        /// <param name="isXAxis"></param>
        /// <returns></returns>
        private static List<ThSprinklerDimGroup> GetEdgeDimensions(List<Point3d> pts, List<List<int>> collinearation, List<List<int>> anothercollinearation, out List<int> dims, bool isXAxis)
        {
            List<int> EdgeDim = new List<int>();
            dims = new List<int>();
            List<int> longestDim = GetLongestLine(collinearation);
            List<int> minmarks = new List<int>();
            List<int> maxmarks = new List<int>();
            List<int> minDim = new List<int>();
            List<int> maxDim = new List<int>();

            anothercollinearation.ForEach(p => p.Sort((x, y) => ThCoordinateService.GetOriginalValue(pts[x], !isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(pts[y], !isXAxis))));
            for (int i = 0; i < anothercollinearation.Count; i++)
            {
                minmarks.Add(anothercollinearation[i][0]);
                maxmarks.Add(anothercollinearation[i][anothercollinearation[i].Count - 1]);
            }

            int mincount = 0;
            int maxcount = 0;
            int longcount = 0;

            for (int p = 0; p < collinearation.Count; p++)
            {
                int linecount = 0;
                foreach (int i in collinearation[p])
                {
                    if (minmarks.Contains(i) && !maxmarks.Contains(i)) linecount += 2;
                    else if (minmarks.Contains(i) && maxmarks.Contains(i)) linecount += 0;
                    else
                        linecount += 1;
                }
                if (linecount > mincount)
                {
                    mincount = linecount;
                    minDim = collinearation[p];
                }
            }

            for (int p = collinearation.Count - 1; p >= 0; p--)
            {
                int linecount = 0;
                foreach (int i in collinearation[p])
                {
                    if (maxmarks.Contains(i) && !minmarks.Contains(i)) linecount += 2;
                    else if (minmarks.Contains(i) && maxmarks.Contains(i)) linecount += 0;
                    else
                        linecount += 1;
                }
                if (linecount > maxcount)
                {
                    maxcount = linecount;
                    maxDim = collinearation[p];
                }
            }
            foreach (int p in longestDim)
            {
                if (minmarks.Contains(p) && maxmarks.Contains(p)) continue;
                else if (!minmarks.Contains(p) && !maxmarks.Contains(p)) longcount += 1;
                else longcount += 2;
            }

            if (mincount > longcount && mincount > maxcount)
            {
                dims = minDim;
                EdgeDim.AddRange(minDim);
            }
            else if (maxcount > longcount && maxcount > mincount)
            {
                dims = maxDim;
                EdgeDim.AddRange(maxDim);
            }
            else if (maxcount > longcount && maxcount == mincount)
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

            List<ThSprinklerDimGroup> FirstDimLine = new List<ThSprinklerDimGroup>();
            foreach (int i in EdgeDim) FirstDimLine.Add(new ThSprinklerDimGroup(i, anothercollinearation.Where(p => p.Contains(i)).ToList()[0]));

            return FirstDimLine;
        }

        /// <summary>
        /// 在第一根标注位上循环补点
        /// </summary>
        /// <param name="group"></param>
        /// <param name="collineation"></param>
        /// <param name="anotherCollineation"></param>
        /// <param name="dim"></param>
        /// <param name="step"></param>
        /// <param name="isXAxis"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        private static List<List<ThSprinklerDimGroup>> AddDimensions(ThSprinklerNetGroup group, List<List<int>> collineation, List<List<int>> anotherCollineation, List<int> dim, double step, bool isXAxis, ThCADCoreNTSSpatialIndex walls)
        {
            bool[] isDimensioned = Enumerable.Repeat(false, anotherCollineation.Count).ToArray();
            List<List<ThSprinklerDimGroup>> resDims = new List<List<ThSprinklerDimGroup>>();
            CheckDimensions(dim, anotherCollineation, ref isDimensioned);
            while (isDimensioned.Contains(false))
            {
                List<int> tDim1 = new List<int>();
                List<int> tDim2 = new List<int>();
                List<ThSprinklerDimGroup> Dim1 = new List<ThSprinklerDimGroup>();
                List<ThSprinklerDimGroup> Dim2 = new List<ThSprinklerDimGroup>();

                for (int i = 0; i < anotherCollineation.Count; i++)
                {
                    if (!isDimensioned[i])
                    {
                        List<int> tDim = GetLongestDimension(anotherCollineation[i], collineation, anotherCollineation, isDimensioned);
                        if (DeleteIsDimed(tDim, anotherCollineation, isDimensioned).Count > tDim2.Count)
                        {
                            List<ThSprinklerDimGroup> t1 = new List<ThSprinklerDimGroup>();
                            List<ThSprinklerDimGroup> t2 = new List<ThSprinklerDimGroup>();
                            tDim1 = tDim;
                            tDim1.ForEach(p => t1.Add(new ThSprinklerDimGroup(p, anotherCollineation.Where(q => q.Contains(p)).ToList()[0])));
                            tDim2 = DeleteIsDimed(tDim, anotherCollineation, isDimensioned);
                            tDim2.ForEach(p => t2.Add(new ThSprinklerDimGroup(p, anotherCollineation.Where(q => q.Contains(p)).ToList()[0])));
                            Dim1 = t1;
                            Dim2 = t2;
                        }

                    }
                }

                double Prop = (double)tDim2.Count / tDim1.Count;
                if (Prop <= 0.5)
                {
                    if (Dim2.Count != 0)
                    {
                        resDims.AddRange(SeperateLine(group.Pts, Dim2, tDim1, isXAxis, step));
                        CheckDimensions(tDim2, anotherCollineation, ref isDimensioned);
                    }
                }
                else
                {
                    if (Dim1.Count != 0)
                    {
                        resDims.Add(Dim1);
                        CheckDimensions(tDim1, anotherCollineation, ref isDimensioned);
                    }
                }
            }

            return resDims;
        }

        /// <summary>
        /// test  打印结果
        /// </summary>
        /// <param name="group"></param>
        /// <param name="Clatersx"></param>
        /// <param name="Clatersy"></param>
        /// <returns></returns>
        private static List<Line> Print(ThSprinklerNetGroup group, out List<Point3d> Clatersx, out List<Point3d> Clatersy)
        {
            Clatersx = new List<Point3d>();
            Clatersy = new List<Point3d>();
            List<Point3d> transPts = group.Pts;
            List<Point3d> pts = ThCoordinateService.MakeTransformation(transPts, group.Transformer.Inverse());
            List<Line> lines = new List<Line>();

            foreach (List<ThSprinklerDimGroup> dim in group.XDimension)
            {
                if (dim == null) continue;
                else if (dim.Count == 1)
                {
                    Clatersx.Add(pts[dim[0].pt]);
                }
                else
                {
                    for (int i = 0; i < dim.Count - 1; i++)
                    {
                        lines.Add(new Line(pts[dim[i].pt], pts[dim[i + 1].pt]));
                    }
                }
            }

            foreach (List<ThSprinklerDimGroup> dim in group.YDimension)
            {
                if (dim == null) continue;
                else if (dim.Count == 1)
                {
                    Clatersy.Add(pts[dim[0].pt]);
                }
                else
                {
                    for (int i = 0; i < dim.Count - 1; i++)
                    {
                        lines.Add(new Line(pts[dim[i].pt], pts[dim[i + 1].pt]));
                    }
                }
            }
            return lines;
        }
    }
}
