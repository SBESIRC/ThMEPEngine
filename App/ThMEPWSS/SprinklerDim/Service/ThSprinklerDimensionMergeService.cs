using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Diagnostics;
using ThMEPWSS.SprinklerDim.Model;
using ThMEPWSS.UndergroundWaterSystem.Service;
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
        public static bool CanMerge(List<Point3d> pts, List<ThSprinklerDimGroup> dim1, List<ThSprinklerDimGroup> dim2, bool isXAxis, double step, Matrix3d matrix, ThCADCoreNTSSpatialIndex walls, double tolerance = 400.0)
        {
            Vector3d xAxis = isXAxis ? new Vector3d(1, 0, 0) : new Vector3d(0, 1, 0);
            List<ThSprinklerDimGroup> BaseLine = dim1.Count < dim2.Count ? dim2 : dim1;
            List<ThSprinklerDimGroup> DimensionToBeMerged = dim1.Count >= dim2.Count ? dim2 : dim1;

            //不考虑点与点之间的合并或者空列表
            if (BaseLine.Count <= 1) return false;

            //计算标注线间距
            double det = Math.Abs(ThCoordinateService.GetOriginalValue(pts[BaseLine[0].pt], !isXAxis) - ThCoordinateService.GetOriginalValue(pts[DimensionToBeMerged[0].pt], !isXAxis));
            if (DimensionToBeMerged.Count == 1)
            {
                Line line = new Line(pts[BaseLine[0].pt], pts[BaseLine[0].pt] + xAxis * 100);
                Point3d droppt = line.GetClosestPointTo(pts[DimensionToBeMerged[0].pt], true);
                List<int> t = DimensionToBeMerged[0].PtsDimed;
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
                foreach (ThSprinklerDimGroup i in DimensionToBeMerged)
                {
                    Line line = new Line(pts[BaseLine[0].pt], pts[BaseLine[0].pt] + xAxis * 100);
                    Point3d droppt = line.GetClosestPointTo(pts[i.pt], true);
                    if (IsConflicted(droppt, pts[i.pt], matrix, walls) || IsConflicted(droppt, pts[BaseLine[0].pt], matrix, walls) || IsConflicted(droppt, pts[BaseLine[BaseLine.Count - 1].pt], matrix, walls))
                        return false;
                }

                double distance1 = ThCoordinateService.GetOriginalValue(pts[BaseLine[0].pt], isXAxis) - ThCoordinateService.GetOriginalValue(pts[DimensionToBeMerged[DimensionToBeMerged.Count - 1].pt], isXAxis);
                double distance2 = ThCoordinateService.GetOriginalValue(pts[BaseLine[BaseLine.Count - 1].pt], isXAxis) - ThCoordinateService.GetOriginalValue(pts[DimensionToBeMerged[0].pt], isXAxis);
                double distance3 = ThCoordinateService.GetOriginalValue(pts[BaseLine[0].pt], isXAxis) - ThCoordinateService.GetOriginalValue(pts[DimensionToBeMerged[0].pt], isXAxis);
                double distance4 = ThCoordinateService.GetOriginalValue(pts[BaseLine[BaseLine.Count - 1].pt], isXAxis) - ThCoordinateService.GetOriginalValue(pts[DimensionToBeMerged[DimensionToBeMerged.Count - 1].pt], isXAxis);

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
        public static void InsertPoints(ref List<Point3d> pts, ref List<List<List<ThSprinklerDimGroup>>> Dimension, double step, bool IsxAxis, Matrix3d matrix, ThCADCoreNTSSpatialIndex walls, out List<int> ficpts)
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
                                    List<int> t = new List<int> { Dimension[i][k][0].pt };
                                    double det = GetNeareastDistance(pts, t, Dimension[i][j][0].PtsDimed, IsxAxis);
                                    if (det < 1.0 * step && 45 < det)
                                    {
                                        List<Point3d> pts1 = pts;
                                        Line line = new Line(pts[Dimension[i][k][0].pt], pts[Dimension[i][k][0].pt] + 100 * xAxis);
                                        Point3d DropPt = line.GetClosestPointTo(pts[Dimension[i][j][0].pt], true);
                                        List<double> distance = new List<double>();
                                        Dimension[i][k].ForEach(p => distance.Add(pts1[p.pt].DistanceTo(DropPt)));
                                        if (IsConflicted(DropPt, pts[Dimension[i][j][0].pt], matrix, walls) || IsConflicted(DropPt, pts[Dimension[i][k][0].pt], matrix, walls) || distance.Min() > 1.5 * step) continue;
                                        else
                                        {
                                            pts.Add(DropPt);
                                            Dimension[i][k].Add(new ThSprinklerDimGroup(pts.Count - 1, Dimension[i][j][0].PtsDimed));
                                            ficpts.Add(pts.Count - 1);
                                            Dimension[i][j] = null;
                                            if (IsxAxis) Dimension[i][k] = Dimension[i][k].OrderBy(p => pts1[p.pt].X).ToList();
                                            else
                                            {
                                                Dimension[i][k] = Dimension[i][k].OrderBy(p => pts1[p.pt].Y).ToList();
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
        public static List<List<ThSprinklerDimGroup>> MergeDimension(ref List<Point3d> pts, List<List<ThSprinklerDimGroup>> group, double step, bool isXAxis, out List<int> FicPts, Matrix3d matrix, ThCADCoreNTSSpatialIndex walls)
        {
            List<int> FicPts1 = new List<int>();
            Dictionary<int, bool> isMerged = new Dictionary<int, bool>();
            foreach (List<ThSprinklerDimGroup> j in group) isMerged.Add(j[0].pt, false);
            List<List<ThSprinklerDimGroup>> MergedDimensions = new List<List<ThSprinklerDimGroup>>();
            for (int i = 0; i < group.Count; i++)
            {
                if (!isMerged[group[i][0].pt])
                {
                    MergedDimensions.Add(GetMerged(ref pts, group[i], group, step, isXAxis, ref isMerged, ref FicPts1, matrix, walls));
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
        public static List<ThSprinklerDimGroup> GetMerged(ref List<Point3d> pts, List<ThSprinklerDimGroup> currentdim, List<List<ThSprinklerDimGroup>> group, double step, bool isXAxis, ref Dictionary<int, bool> isMerged,ref List<int> FicPts, Matrix3d matrix, ThCADCoreNTSSpatialIndex walls)
        {
            List<ThSprinklerDimGroup> MergedDims = currentdim;
            isMerged[currentdim[0].pt] = true;

            foreach (List<ThSprinklerDimGroup> DimensionToBeMerged in group)
            {
                if (CanMerge(pts, currentdim, DimensionToBeMerged, isXAxis, step, matrix, walls) && !currentdim.Equals(DimensionToBeMerged))
                {
                    if (!isMerged[DimensionToBeMerged[0].pt])
                    {
                        isMerged[DimensionToBeMerged[0].pt] = true;
                        if (DimensionToBeMerged.Count > currentdim.Count)
                        {
                            MergedDims = DimensionToBeMerged;
                            foreach (ThSprinklerDimGroup j in currentdim)
                            {
                                if (isXAxis)
                                {
                                    pts.Add(new Point3d(pts[j.pt].X, pts[DimensionToBeMerged[0].pt].Y, 0));
                                    MergedDims.Add(new ThSprinklerDimGroup(pts.Count - 1, j.PtsDimed));
                                    FicPts.Add(pts.Count - 1);
                                }
                                else
                                {
                                    pts.Add(new Point3d(pts[DimensionToBeMerged[0].pt].X, pts[j.pt].Y, 0));
                                    MergedDims.Add(new ThSprinklerDimGroup(pts.Count - 1, j.PtsDimed));
                                    FicPts.Add(pts.Count - 1);
                                }
                            }
                        }
                        else
                        {
                            foreach (ThSprinklerDimGroup j in DimensionToBeMerged)
                            {
                                if (isXAxis)
                                {
                                    pts.Add(new Point3d(pts[j.pt].X, pts[currentdim[0].pt].Y, 0));
                                    MergedDims.Add(new ThSprinklerDimGroup(pts.Count - 1, j.PtsDimed));
                                    FicPts.Add(pts.Count - 1);
                                }
                                else
                                {
                                    pts.Add(new Point3d(pts[currentdim[0].pt].X, pts[j.pt].Y, 0));
                                    MergedDims.Add(new ThSprinklerDimGroup(pts.Count - 1, j.PtsDimed));
                                    FicPts.Add(pts.Count - 1);
                                }
                            }
                        }
                        List<Point3d> pts1 = pts;
                        MergedDims.Sort((x, y) => ThCoordinateService.GetOriginalValue(pts1[x.pt], isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(pts1[y.pt], isXAxis)));
                        MergedDims = GetMerged(ref pts, MergedDims, group, step, isXAxis, ref isMerged, ref FicPts, matrix, walls);
                        break;
                    }
                }
            }
            return MergedDims;
        }
    }
    
}
