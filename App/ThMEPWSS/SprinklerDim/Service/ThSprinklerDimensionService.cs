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

        public static void GenerateDimension(List<ThSprinklerNetGroup> transNetList, double step, string printTag)
        {
            
            foreach (ThSprinklerNetGroup group in transNetList)
            {
                // 合并边缘
                group.XDimension.AddRange(MergeEdgeDimensions(group.Pts, group.XCollineationGroup, out var XDim, step, true));
                group.YDimension.AddRange(MergeEdgeDimensions(group.Pts, group.YCollineationGroup, out var YDim, step, false));

                // 补充标注
                for(int i = 0; i < group.PtsGraph.Count; i++)
                {
                    group.XDimension.AddRange(AddDimensions(group.XCollineationGroup[i], group.YCollineationGroup[i], XDim[i]));
                    group.YDimension.AddRange(AddDimensions(group.YCollineationGroup[i], group.XCollineationGroup[i], YDim[i]));
                }
                
            }

            // 打印标注
            for (int i = 0; i < transNetList.Count; i++)
            {
                List<Line> dimensions = Print(transNetList[i]);
                DrawUtils.ShowGeometry(dimensions, string.Format("Dimension-{0}-{1}", printTag, i), i % 7);
            }

        }

        // 优先选转换后坐标系下值小的标注进行边缘合并，若无合并标注且点数小于最长的1/3，则选择最长标注
        private static List<List<int>> MergeEdgeDimensions(List<Point3d> pts, List<List<List<int>>> group, out List<List<int>> dims, double step, bool isXAxis)
        {
            List<List<int>> mergedDim = new List<List<int>>();
            dims = new List<List<int>>(group.Count);
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

        private static List<int> GetMergedDimension(List<Point3d> pts, List<int> currentDim, ref List<List<int>> dims, List<List<List<int>>> group, ref bool[] isMerged, double step, bool isXAxis)
        {
            List<int> mergedDim = new List<int>();
            mergedDim.AddRange(currentDim);
            for (int i = 0; i < currentDim.Count; i++)
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

        private static bool CanMerge(List<Point3d> pts, List<int> dim1, List<int> dim2, bool isXAxis, double step, double tolerance=45.0)
        {
            double det = ThChangeCoordinateService.GetOriginalValue(pts[dim1[0]], !isXAxis) - ThChangeCoordinateService.GetOriginalValue(pts[dim2[0]], !isXAxis);
            if(Math.Abs(det) < tolerance)
            {
                double distance1 = ThChangeCoordinateService.GetOriginalValue(pts[dim1[0]], isXAxis) - ThChangeCoordinateService.GetOriginalValue(pts[dim2[dim2.Count-1]], isXAxis);
                double distance2 = ThChangeCoordinateService.GetOriginalValue(pts[dim1[dim1.Count-1]], isXAxis) - ThChangeCoordinateService.GetOriginalValue(pts[dim2[0]], isXAxis);

                if ((tolerance < Math.Abs(distance1) && Math.Abs(distance1) < 2 * step) || (tolerance < Math.Abs(distance2) && Math.Abs(distance2) < 2 * step))
                    return true;

            }

            return false;
        }

        private static List<int> GetLongestLine(List<List<int>> collineationList)
        {
            int longestLineIndex = 0;
            for(int i = 1; i < collineationList.Count; i++)
            {
                if(collineationList[i].Count > collineationList[longestLineIndex].Count)
                    longestLineIndex = i;
            }

            return collineationList[longestLineIndex];
        }




        private static List<List<int>> AddDimensions(List<List<int>> collineation, List<List<int>> anotherCollineation, List<int> dim)
        {
            bool[] isDimensioned = Enumerable.Repeat(false, anotherCollineation.Count).ToArray();
            List<List<int>> resDims = new List<List<int>>();

            CheckDimensions(dim, anotherCollineation, ref isDimensioned);

            for (int i = 0; i < anotherCollineation.Count; i++)
            {
                if (!isDimensioned[i])
                {
                    List<int> tDim = GetLongestDimension(anotherCollineation[i], collineation);
                    CheckDimensions(tDim, anotherCollineation, ref isDimensioned);
                    resDims.Add(tDim);
                }

            }

            return resDims;
        }

        private static void CheckDimensions(List<int> dim, List<List<int>> anotherCollineation, ref bool[] isDimensioned)
        {
            foreach(int idx in dim)
            {
                for(int i = 0; i < anotherCollineation.Count; i++)
                {
                    if (anotherCollineation[i].Contains(idx))
                    {
                        isDimensioned[i] = true;
                    }
                }
            }
        }

        private static List<int> GetLongestDimension(List<int> undimensionedLine, List<List<int>> collineation)
        {
            int len = 0;
            List<int> dim = new List<int>();
            foreach(int i in undimensionedLine)
            {
                List<int> line = collineation.Where(x => x.Contains(i)).ToList()[0];
                if (line.Count > len)
                {
                    len = line.Count;
                    dim = line;
                }
            }

            return dim;
        }


        private static List<Line> Print(ThSprinklerNetGroup group)
        {
            List<Point3d> transPts = group.Pts;
            List<Point3d> pts = ThChangeCoordinateService.MakeTransformation(transPts, group.Transformer);
            List<Line> lines = new List<Line>();

            foreach(List<int> dim in group.XDimension)
            {
                for (int i = 0; i < dim.Count-1; i++)
                {
                    lines.Add(new Line(pts[dim[i]], pts[dim[i + 1]]));
                }
            }

            foreach (List<int> dim in group.YDimension)
            {
                for (int i = 0; i < dim.Count - 1; i++)
                {
                    lines.Add(new Line(pts[dim[i]], pts[dim[i + 1]]));
                }
            }

            return lines;
        }

    }
}
