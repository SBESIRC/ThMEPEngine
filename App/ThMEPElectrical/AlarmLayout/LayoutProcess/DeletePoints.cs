﻿
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Collections;
using NetTopologySuite.Operation.Overlay.Snap;
using Dreambuild.AutoCAD;
using ThMEPElectrical.AlarmLayout.Utils;

namespace ThMEPElectrical.AlarmLayout.LayoutProcess
{
    public static class DeletePoints
    {
        /// <summary>
        /// 删点测试的预先操作，排除极大可能不会被删除的点
        /// </summary>
        /// <param name="ht">记录点是否被删</param>
        /// <param name="points"></param>
        /// <param name="radius"></param>
        public static void ReducePoints(Hashtable ht, List<Point3d> points, double radius)
        {
            //ht<Point3d, int>:0要删除的点，1需要进行删除测试的点，2"孤独点"不需要进行删除测试的点
            int cntNear, cntMiddle;
            double distence;
            int middleCmp = radius < 3300 ? 10 : 1;//20 : 2//越小越准确：两个值至少为1：1
            double centerCmp = radius < 3300 ? 0.5 : 0.8;//越大越准确：两个值最大为1
            foreach (Point3d pt in points)
            {
                cntNear = 0;
                cntMiddle = 0;
                foreach (Point3d p in points)
                {
                    distence = pt.DistanceTo(p);
                    if (distence < radius * centerCmp)
                    {
                        ++cntNear;//此处可以优化 提前break（cntNear > 1） 但优化与否影响不大
                    }
                    else if (distence < radius * 1.1)//* 1.2
                    {
                        ++cntMiddle;
                    }
                }
                if (cntNear == 1 && cntMiddle <= middleCmp)
                {
                    ht[pt] = 2;
                }
                if (ht.Contains(pt))
                {
                    continue;
                }
                else ht.Add(pt, 1);
            }
        }

        /// <summary>
        /// 删点的核心操作，尝试依次删除点集中的点
        /// </summary>
        /// <param name="mPolygon"></param>
        /// <param name="ht">记录点是否被删</param>
        /// <param name="points"></param>
        /// <param name="radius"></param>
        public static void RemovePoints(MPolygon mPolygon, Hashtable ht, List<Point3d> points, double radius)
        {
            //计算过当前剩余总面积
            double totalUncoverArea = SnapIfNeededOverlayOp.Difference(mPolygon.ToNTSPolygon(), AreaCaculator.GetUnion(points, radius)).Area;

            foreach (Point3d pt in points)
            {
                if ((int)ht[pt] == 1)
                {
                    List<Point3d> tmpPt = new List<Point3d>();
                    ht[pt] = 0;
                    foreach (DictionaryEntry xx in ht)
                    {
                        if ((int)xx.Value != 0)
                        {
                            tmpPt.Add((Point3d)xx.Key);
                        }
                    }
                    //获取删除这个点后的得到的未覆盖区域
                    var unCoverRegion = SnapIfNeededOverlayOp.Difference(mPolygon.ToNTSPolygon(), AreaCaculator.GetUnion(tmpPt, radius));

                    bool flag = true;//默认删点,false不删点
                    foreach (Entity obj in unCoverRegion.ToDbCollection())
                    {
                        if (obj.GetArea() > 500000.0 && unCoverRegion.Area - totalUncoverArea > 500000.0)//如果删除后面积变大超过要求
                        {
                            flag = false;
                        }
                    }
                    if (flag == true)
                    {
                        //ShowPointAsX(pt, 1);
                        continue;
                    }
                    ht[pt] = 1;
                }
            }
        }

        /// <summary>
        /// 获取哈希表中不能被删除的点
        /// </summary>
        /// <param name="ht"></param>
        /// <returns></returns>
        public static List<Point3d> SummaryPoints(Hashtable ht)
        {
            List<Point3d> points = new List<Point3d>();
            foreach (DictionaryEntry x in ht)
            {
                if ((int)x.Value > 0)
                {
                    /*
                    //用O显示被优化的点
                    if ((int)x.Value == 2)
                    {
                        ShowPointAsO((Point3d)x.Key);
                    }
                    */
                    points.Add((Point3d)x.Key);
                }
            }
            return points;
        }
    }
}
