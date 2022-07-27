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
    public class ThSprinklerDimPosition
    {
        //处理单个已经分好的组（单个方向）
        //输入参数（单个的组netList，已经确定某方向坐标的边缘点bd）
        //输出参数（需要标注的喷头位置点EleDimPosition）
        public static List<List<Point3d>> EleGetDimPoints(ThSprinklerNetGroup netList, List<List<Point3d>> bd)
        {
            List<List<Point3d>> EleDimPosition = new();
            List<Point3d> point3Ds = netList.Pts;
            point3Ds = point3Ds.OrderBy(p => (long)p.X / 10).ThenBy(q => (long)q.Y / 10).ToList();//排序

            //循环体
            bool IsDone = false;
            while (!IsDone)
            {
                List<long> x_all = new();

                //保存已经标注的xy坐标
                List<long> xAllready = new();

                foreach (Point3d pt in point3Ds)if (!x_all.Contains(((long)pt.X / 10))) x_all.Add((long)pt.X / 10);

                int xMaxNum = 0;

                Dictionary<long, List<int>> xcount = new();
                foreach (long x in x_all)
                {
                    List<int> xindex = new();
                    for (int i = 0; i < point3Ds.Count; i++)
                    {
                        var pt = point3Ds[i];
                        if (x == (long)pt.X / 10) xindex.Add(i);
                    }
                    xMaxNum = Math.Max(xindex.Count, xMaxNum);
                    xcount.Add(x, xindex);
                }
                //提取点最多的线上的点
                //判断是靠边缘标注还是在最长边标注
            }
            bd = null;
            return EleDimPosition;
        }

        ////标注某方向的最长线
        //public static List<List<Point3d>> GetLngLines(List<ThSprinklerNetGroup> netList, bool IsxAxis, List<long> AllreadyDim)
        //{
        //    List<List<Point3d>> LongLine = new();//存储各组最长线（最小的x）
        //    for (int j = 0; j < netList.Count; j++)
        //    {
        //        //存储各个值对应的点
        //        List<Point3d> point3Ds = netList[j].Pts;
        //        point3Ds = point3Ds.OrderBy(p => (long)p.X / 10).ThenBy(q => (long)q.Y / 10).ToList();
        //        List<long> all = new();


        //        foreach (Point3d pt in point3Ds)
        //        {
        //            if (!all.Contains(GetValue(pt, IsxAxis))) all.Add(GetValue(pt, IsxAxis));
        //        }

        //        Dictionary<long, List<PtMark>> count = new();
        //        int nummax = 0;
        //        foreach (long value in all)
        //        {
        //            List<PtMark> t = new();
        //            for (int i = 0; i < point3Ds.Count; i++)
        //            {
        //                PtMark pt = new();
        //                pt.Pt = point3Ds[i];
        //                pt.Mark = AllreadyDim.Contains(GetValue(pt.Pt, !IsxAxis));
        //                if (GetValue(pt.Pt, IsxAxis) == value ) t.Add(pt);
        //            }
        //            nummax = Math.Max(nummax, t.Count);
        //            count.Add(value, t);
        //        }
        //        List<long> keymin = new();
        //        foreach (long key in count.Keys)
        //        {
        //            if (count[key].Count == nummax) keymin.Add(key);
        //        }
        //        LongLine.Add(count[keymin.Min()]);
        //    }

        //    return LongLine;

        //}

        //边界标注合并函数
        //输入参数：各组的边界点的列表bd，步长step
        //输出参数：合并后的点的列表BoundaryDimPosition，与能合并的组的编号
        public static List<List<Point3d>> BoundaryMerge(List<Dictionary<long, List<Point3d>>> bd, double step, bool IsxAxis, out List<long> AllreadyDim, out List<int> id)
        {
            //处理可以合并的边界标注
            List<List<Point3d>> BoundaryDimPosition = new();
            List<long> AllreadyDims = new();
            List<int> ids = new();
            for (int i = 0; i < bd.Count-1; i++) 
            {
                List<int> idd = new();
                idd.Add(i);
                foreach(long key in bd[i].Keys)
                {
                    List<Point3d> t = bd[i][key];
                    for (int j = i + 1; j < bd.Count; j++)
                    {
                        if (bd[j].Keys.Contains(key))
                        {
                            t = t.Concat(bd[j][key]).ToList();
                            bd[j].Remove(key);
                            idd.Add(j);
                        }
                    }
                    t = t.OrderBy(p => GetValue(p, IsxAxis)).ThenBy(q => GetValue(q, !IsxAxis)).ToList();
                    for (int k = 0; k < t.Count - 1; k++)
                    {
                        if (t[k].DistanceTo(t[k + 1]) > 2 * step)//判断是否大于两个步长
                        {
                            t = bd[i][key];
                            break;
                        }
                    }
                    if (t.Count != bd[i][key].Count)
                    {
                        BoundaryDimPosition.Add(t);
                        ids = ids.Concat(idd).ToList();
                        for (int k = 0; k < t.Count; k++)
                        {
                            if (!AllreadyDims.Contains(GetValue(t[k], !IsxAxis))) AllreadyDims.Add(GetValue(t[k], !IsxAxis));
                        }
                    }
                }
            }
            id = ids;
            AllreadyDim = AllreadyDims;
            return BoundaryDimPosition;
        }

        public static List<Point3d> GetProperFirstLine(ThSprinklerNetGroup netlist, bool IsxAxis)
        {
            List<Point3d> point3Ds = netlist.Pts;
            point3Ds = point3Ds.OrderBy(p => GetValue(p, IsxAxis)).ThenBy(q => GetValue(q, !IsxAxis)).ToList();
            List<long> all = new();
            foreach (Point3d pt in point3Ds)
            {
                if (!all.Contains(GetValue(pt, IsxAxis))) all.Add(GetValue(pt, IsxAxis));
            }

            List<Point3d> max = new();
            List<Point3d> min = new();
            List<Point3d> Lline = new();

            Dictionary<long, List<Point3d>> count = new();
            List<long> longs = new();
            int nummax = 0;
            foreach (long value in all)
            {
                List<Point3d> t = new();
                for (int i = 0; i < point3Ds.Count; i++)
                {
                    var pt = point3Ds[i];
                    if (GetValue(pt, IsxAxis) == value) t.Add(pt);
                }
                nummax = Math.Max(nummax, t.Count);
                count.Add(value, t);
            }
            foreach (long value in all)
            {
                if (count[value].Count == nummax) longs.Add(value);
            }
            if (IsxAxis) Lline = count[longs.Min()];
            else
            {
                Lline = count[longs.Max()];
            }
            max = count[all.Max()];
            min = count[all.Min()];

            int bdnum1 = 0;
            int bdnum2 = 0;
            List<long> bd_1 = new();
            List<long> bd_2 = new();
            List<long> ll_ = new();
            foreach (Point3d pt in max) bd_1.Add(GetValue(pt, !IsxAxis));
            foreach (Point3d pt in min) bd_2.Add(GetValue(pt, !IsxAxis));
            foreach (Point3d pt in Lline) ll_.Add(GetValue(pt, !IsxAxis));
            foreach (long value in bd_1)
            {
                if (ll_.Contains(value)) bdnum1++;
            }
            foreach (long value in bd_2)
            {
                if (ll_.Contains(value)) bdnum2++;
            }

            if (Lline.Count > 3 * Math.Max(bdnum1, bdnum2)) return Lline;
            else
            {
                if (bdnum1 > bdnum2) return max;
                else
                {
                    return min;
                }
            }
        }

        private static long GetValue(Point3d pt, bool isXAxis)
        {
            if (isXAxis)
                return (long)pt.X / 45;
            else
                return (long)pt.Y / 45;
        }

        //对每个防火片区某一方向做处理
        //得到每个组点最多的线和边缘线
        //将边缘线放入合并函数（BoundaryMerge）当中，能合并的合并，不能合并的选取一条最优的标注线
        //输入“边界点的列表bd”
        //输出“合并后的边界标注位置BoundaryDimPosition”
        public static List<List<Point3d>> GetDimPoints(List<ThSprinklerNetGroup> netList, double step, bool IsxAxis)
        {
            List<Dictionary<long, List<Point3d>>> bd = new();//存储各组边界线
            List<List<Point3d>> DimPoints = new();
            List<ThSprinklerDimGroup> Groups = new();

            for (int j = 0; j < netList.Count; j++)
            {
                List<Point3d> point3Ds = netList[j].Pts;
                point3Ds = point3Ds.OrderBy(p => GetValue(p, IsxAxis)).ThenBy(q => GetValue(q, !IsxAxis)).ToList();
                List<long> all = new();
                foreach (Point3d pt in point3Ds)
                {
                    if (!all.Contains(GetValue(pt, IsxAxis))) all.Add(GetValue(pt, IsxAxis));
                }

                Dictionary<long, List<Point3d>> count = new();
                foreach(long value in all)
                {
                    List<Point3d> t = new();
                    for (int i = 0; i < point3Ds.Count; i++)
                    {
                        var pt = point3Ds[i];
                        if (GetValue(pt, IsxAxis) == value) t.Add(pt);
                    }
                    count.Add(value, t);
                }
                
                foreach(long key in count.Keys)
                {
                    List<Point3d> ts = count[key];
                    while(ts.Count > 0)
                    {
                        ThSprinklerDimGroup group = new();
                        ts = group.GetPoints(ts);
                        Groups.Add(group);
                    }
                }

                List<Point3d> max = new();
                List<Point3d> min = new();
                Dictionary<long, List<Point3d>> bdcount = new();
                bdcount.Add(all.Min(), count[all.Min()]);
                if (all.Count != 1) bdcount.Add(all.Max(), count[all.Max()]);
                bd.Add(bdcount);
            }

            List<long> allreadydim = new();
            List<int> index = new();
            DimPoints = DimPoints.Concat(BoundaryMerge(bd, step, IsxAxis, out allreadydim, out index)).ToList();

            for (int j = 0; j < netList.Count; j++)
            {
                if (index.Contains(j)) continue;
                DimPoints.Add(GetProperFirstLine(netList[j], IsxAxis));
            }



            return DimPoints;
        }

        //绘制出需要标注的喷头位置相连接而成的线段
        public static void DrawDimLines(List<ThSprinklerNetGroup> netList, string printag, double step)
        {
            var DimPositionX = GetDimPoints(netList, step, true);
            var DimPositionY = GetDimPoints(netList, step, false);
            var DimPosition = DimPositionX.Concat(DimPositionY).ToList();
            for (int i = 0; i < DimPosition.Count; i++)
            {
                List<Line> DimLines = new();
                DimPosition[i] = DimPosition[i].OrderBy(p => (long)p.X / 10).ThenBy(q => (long)q.Y / 10).ToList();
                if (DimPosition[i].Count == 1) continue;
                for (int j = 0; j < DimPosition[i].Count - 1; j++)
                {
                    DimLines.Add(new Line(DimPosition[i][j], DimPosition[i][j + 1]));
                }
                DrawUtils.ShowGeometry(DimLines, "huang_test_" + printag, 5);
            }
            //for (int i = 0; i < DimPosition.Count; i++)
            //{
            //    List<Line> DimLines = new();
            //    if (DimPosition[i].Count == 1) continue;
            //    for (int j = 0; j < DimPosition[i].Count - 1; j++)
            //    {
            //        DimLines.Add(new Line(DimPosition[i][j], DimPosition[i][j + 1]));
            //    }
            //    DimLinesFm.Add(DimLines);
            //    DrawUtils.ShowGeometry(DimLines, "h_" + printag);
            //}

        }
    }
}
