using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPArchitecture.ParkingStallArrangement.Method;

namespace ThMEPArchitecture.ParkingStallArrangement.Model
{
    public static class PointAreaSeg
    {
        public static double Gap = 15700;

        public static List<Polyline> PtAreaSeg(Polyline area, List<int> ptIndex, List<int> direction, List<Polyline> building,
            List<Point3d> intersectPt)
        {
            var pts = new List<Point3d>();
            for (int i = 0; i < ptIndex.Count; i++)
            {
                pts.Add(intersectPt[ptIndex[i]]);//交点提取
            }
            if (ptIndex.Count == 1)
            {
                return Pt1Seg(area, pts[0], direction[0], building);
            }
            if (ptIndex.Count == 2)
            {
                return Pt2Seg(area, pts, direction, building);
            }
            if (ptIndex.Count == 4)
            {
                return Pt4Seg(area, pts, direction, building);
            }

            return new List<Polyline>() { area };
        }

        private static List<Polyline> Pt1Seg(Polyline area, Point3d pt, int direction, List<Polyline> building)
        {
            var splitAreas = new List<Polyline>() { area };
            double tor = 10000;
            var areaPts = area.GetPoints();//拿到所有的点
            var areaPtsVerticalSort = areaPts.OrderBy(p => p.Y);
            var areaPtsHorizontalSort = areaPts.OrderBy(p => p.X);
            var buildPts = new List<Point3d>();//所有建筑物的点
            building.ForEach(pline => buildPts.AddRange(pline.GetPoints()));

            var centerPt = area.GetCenter();
            var line = new Line();
            if (direction == 1)//竖向分割
            {
                buildPts = buildPts.OrderBy(p => p.X).ToList();
                var spt = new Point3d(pt.X, areaPtsVerticalSort.Last().Y + tor, 0);
                var ept = new Point3d(pt.X, areaPtsVerticalSort.First().Y - tor, 0);
                line = new Line(spt, ept);
                if (centerPt.X < pt.X)//区域在分割线左边
                {
                    var bound = buildPts.Last().X;
                    int gapNums = (int)((Math.Abs(pt.X - bound) / Gap));
                    if (gapNums > 0)
                    {
                        var splitLine = line.OffSetX(-gapNums * Gap);
                        splitAreas = splitLine.SplitByLine(area);
                    }
                }
                else//区域在分割线右边
                {
                    var bound = buildPts.First().X;
                    int gapNums = (int)((Math.Abs(pt.X - bound) / Gap));
                    if (gapNums > 0)
                    {
                        var splitLine = line.OffSetX(gapNums * Gap);
                        splitAreas = splitLine.SplitByLine(area);
                    }
                }
            }
            else//横向分割
            {
                buildPts = buildPts.OrderBy(p => p.Y).ToList();
                var spt = new Point3d(areaPtsHorizontalSort.First().X - tor, pt.Y, 0);
                var ept = new Point3d(areaPtsHorizontalSort.Last().X + tor, pt.Y, 0);
                line = new Line(spt, ept);
                if (centerPt.Y < pt.Y)//区域在分割线下边
                {
                    var bound = buildPts.Last().Y;
                    int gapNums = (int)((Math.Abs(pt.Y - bound) / Gap));
                    if (gapNums > 0)
                    {
                        var splitLine = line.OffSetY(-gapNums * Gap);
                        splitAreas = splitLine.SplitByLine(area);
                    }
                }
                else//区域在分割线上边
                {
                    var bound = buildPts.First().Y;
                    int gapNums = (int)((Math.Abs(pt.Y - bound) / Gap));
                    if (gapNums > 0)
                    {
                        var splitLine = line.OffSetY(gapNums * Gap);
                        splitAreas = splitLine.SplitByLine(area);
                    }
                }
            }
            return splitAreas;
        }

        private static List<Polyline> Pt1Seg(Polyline area, Point3d pt, int direction, List<Polyline> building, out Point3d offsetPt)
        {
            offsetPt = new Point3d(pt.X, pt.Y, 0);
            var splitAreas = new List<Polyline>() { area };
            double tor = 10000;
            var areaPts = area.GetPoints();//拿到所有的点
            var areaPtsVerticalSort = areaPts.OrderBy(p => p.Y);
            var areaPtsHorizontalSort = areaPts.OrderBy(p => p.X);
            var buildPts = new List<Point3d>();//所有建筑物的点
            building.ForEach(pline => buildPts.AddRange(pline.GetPoints()));

            var centerPt = area.GetCenter();
            var line = new Line();
            if (direction == 1)//竖向分割
            {
                buildPts = buildPts.OrderBy(p => p.X).ToList();
                var spt = new Point3d(pt.X, areaPtsVerticalSort.Last().Y + tor, 0);
                var ept = new Point3d(pt.X, areaPtsVerticalSort.First().Y - tor, 0);
                line = new Line(spt, ept);
                if (centerPt.X < pt.X)//区域在分割线左边
                {
                    var bound = buildPts.Last().X;
                    int gapNums = (int)((Math.Abs(pt.X - bound) / Gap));
                    if (gapNums > 0)
                    {
                        var splitLine = line.OffSetX(-gapNums * Gap);
                        splitAreas = splitLine.SplitByLine(area);
                        offsetPt = pt.OffSetX(-gapNums * Gap);//返回偏移点
                    }
                }
                else//区域在分割线右边
                {
                    var bound = buildPts.First().X;
                    int gapNums = (int)((Math.Abs(pt.X - bound) / Gap));
                    if (gapNums > 0)
                    {
                        var splitLine = line.OffSetX(gapNums * Gap);
                        splitAreas = splitLine.SplitByLine(area);
                        offsetPt = pt.OffSetX(gapNums * Gap);//返回偏移点
                    }
                }
            }
            else//横向分割
            {
                buildPts = buildPts.OrderBy(p => p.Y).ToList();
                var spt = new Point3d(areaPtsHorizontalSort.First().X - tor, pt.Y, 0);
                var ept = new Point3d(areaPtsHorizontalSort.Last().X + tor, pt.Y, 0);
                line = new Line(spt, ept);
                if (centerPt.Y < pt.Y)//区域在分割线下边
                {
                    var bound = buildPts.Last().Y;
                    int gapNums = (int)((Math.Abs(pt.Y - bound) / Gap));
                    if (gapNums > 0)
                    {
                        var splitLine = line.OffSetY(-gapNums * Gap);
                        splitAreas = splitLine.SplitByLine(area);
                        offsetPt = pt.OffSetY(-gapNums * Gap);//返回偏移点
                    }
                }
                else//区域在分割线上边
                {
                    var bound = buildPts.First().Y;
                    int gapNums = (int)((Math.Abs(pt.Y - bound) / Gap));
                    if (gapNums > 0)
                    {
                        var splitLine = line.OffSetY(gapNums * Gap);
                        splitAreas = splitLine.SplitByLine(area);
                        offsetPt = pt.OffSetY(gapNums * Gap);//返回偏移点
                    }
                }
            }
            return splitAreas;
        }

        private static List<Polyline> Pt2Seg(Polyline area, List<Point3d> pts, List<int> directions, List<Polyline> building)
        {
            var splitAreas = new List<Polyline>() { };
            var areaPts = area.GetPoints();//拿到所有的点
            var areaPtsVerticalSort = areaPts.OrderBy(p => p.Y);
            var areaPtsHorizontalSort = areaPts.OrderBy(p => p.X);
            var buildPts = new List<Point3d>();//所有建筑物的点
            building.ForEach(pline => buildPts.AddRange(pline.GetPoints()));

            var centerPt = area.GetCenter();
            var line = new Line();

            var dir1 = directions[0];
            var dir2 = directions[1];
            var ptDir = pts[0].GetPtDir(pts[1]);//纵向为true
            if (ptDir == -1)//正常不会出现
            {
                ;
            }
            if (dir1 == dir2 && dir1 != ptDir)//00 竖 或 11 横, 切两刀
            {
                return Pt3Seg(area, pts, directions, building);
            }
            else if (dir1 == dir2 && dir1 == ptDir)//00 横 或 11 竖, 切一刀
            {
                return Pt1Seg(area, pts[0], directions[0], building);
            }
            else//有顺序的切两刀
            {
                var sortPts = SumEq01Sort(pts, directions, out bool flag);
                var num = Convert.ToInt32(flag);
                return Pt3Seg(area, sortPts, new List<int>() { 1 - num, num }, building);

            }
        }

        private static List<Point3d> SumEq01Sort(List<Point3d> pts, List<int> directions, out bool flag)
        {
            //flag表示 输出的点顺序， 01为true，10为false
            var ptDir = pts[0].GetPtDir(pts[1]);
            if (ptDir == 1)//竖向，0点优先
            {
                flag = true;
                if (directions[0] == 0)
                {
                    return pts;
                }
                else
                {
                    return new List<Point3d>() { pts[1], pts[0] };
                }
            }
            else//横向， 1点优先
            {
                flag = false;
                if (directions[0] == 1)
                {
                    return pts;
                }
                else
                {
                    return new List<Point3d>() { pts[1], pts[0] };
                }
            }
        }

        private static List<Polyline> Pt3Seg(Polyline area, List<Point3d> pts, List<int> directions, List<Polyline> building)
        {
            var splitAreas = new List<Polyline>() { area };

            for (int i = 0; i < pts.Count; i++)
            {
                foreach (var tmp in splitAreas)
                {
                    if (!tmp.Contains(pts[i]))
                    {
                        splitAreas.Remove(tmp);
                        splitAreas.AddRange(Pt1Seg(tmp, pts[i], directions[i], building));
                        break;
                    }
                }
            }
            return splitAreas;
        }
        
        private static List<Polyline> Pt4Seg(Polyline area, List<Point3d> pts, List<int> directions, List<Polyline> building)
        {
            var splitAreas = new List<Polyline>() { area };
            var areaPts = area.GetPoints();//拿到所有的点
            var areaPtsVerticalSort = areaPts.OrderBy(p => p.Y);
            var areaPtsHorizontalSort = areaPts.OrderBy(p => p.X);
            var buildPts = new List<Point3d>();//所有建筑物的点
            building.ForEach(pline => buildPts.AddRange(pline.GetPoints()));

            var centerPt = area.GetCenter();
            var line = new Line();
            var sum = directions.Sum();
            if (sum == 0)//全0 横向切两刀
            {
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    var pt1 = pts[i];
                    for (int j = i + 1; j < pts.Count; j++)
                    {
                        var pt2 = pts[j];
                        var ptDir = pt1.GetPtDir(pt2);//纵向为true
                        if (ptDir == 1)//纵向两点
                        {
                            return Pt2Seg(area, new List<Point3d>() { pt1, pt2 }, new List<int>() { 0, 0 }, building);
                        }
                    }
                }
            }
            if (sum == 1 || sum == 3)
            {
                var flag = sum > 2;
                var num = Convert.ToInt32(sum > 2);
                var sortPts = SumEqSort(pts, directions, flag);
                return Pt3Seg(area, sortPts, new List<int>() { num, 1 - num, num }, building);
            }
            if (sum == 4)//全1 纵向切两刀
            {
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    var pt1 = pts[i];
                    for (int j = i + 1; j < pts.Count; j++)
                    {
                        var pt2 = pts[j];
                        var ptDir = pt1.GetPtDir(pt2);//纵向为true
                        if (ptDir == 0)//横向两点
                        {
                            return Pt2Seg(area, new List<Point3d>() { pt1, pt2 }, new List<int>() { 1, 1 }, building);
                        }
                    }
                }
            }
            if (sum == 2)
            {
                var sortPts = SumEq2Sort(pts, directions, out int flag);
                if (flag == 0)
                {
                    return Pt3Seg(area, sortPts, new List<int>() { flag, 1 - flag, 1 - flag }, building);
                }
                if (flag == 1)
                {
                    return Pt3Seg(area, sortPts, new List<int>() { flag, 1 - flag, 1 - flag }, building);
                }
                if (flag == -1)//0101的循环排列
                {
                    return PtSeg0101(area, pts, directions, building);
                }
            }

            return splitAreas;
        }

        private static List<Polyline> PtSeg0101(Polyline area, List<Point3d> pts, List<int> directions, List<Polyline> building)
        {
            var splitAreas = new List<Polyline>() { };
            var newPts = new List<Point3d>();
            for (int i = 0; i < pts.Count; i++)
            {
                var pt = pts[i];
                var dir = directions[i];
                Pt1Seg(area, pt, dir, building, out Point3d offsetPt);//尝试分割
                newPts.Add(offsetPt);
            }
            splitAreas.Add(PtSegCenter(area, pts, directions, building));
            for (int i = 0; i < pts.Count - 1; i++)
            {
                for (int j = 1; j < pts.Count; j++)
                {
                    if (directions[i] != directions[j])//不同向的点做连接
                    {
                        var pline = GetArea(newPts[i], newPts[j]);
                        if (pline != null)
                        {
                            splitAreas.Add(pline);
                        }
                    }
                }
            }

            return splitAreas;
        }

        private static Polyline PtSegCenter(Polyline area, List<Point3d> pts, List<int> directions, List<Polyline> building)
        {
            var splitAreas = new List<Polyline>() { area };

            for (int i = 0; i < pts.Count; i++)
            {
                foreach (var tmp in splitAreas)
                {
                    if (!tmp.Contains(pts[i]))
                    {
                        splitAreas.Remove(tmp);
                        splitAreas.AddRange(Pt1Seg(tmp, pts[i], directions[i], building));
                        break;
                    }
                }
            }

            var centerArea = new Polyline();
            foreach (var split in splitAreas)
            {
                var centerFlag = true;
                foreach (var pt in pts)
                {
                    if (split.Contains(pt))
                    {
                        centerFlag = false;
                        break;
                    }
                }
                if (centerFlag)
                {
                    centerArea = split;
                    break;
                }
            }
            return centerArea;
        }

        private static Polyline GetArea(Point3d pt1, Point3d pt2)
        {
            var pts = new Point2dCollection();
            var pline = new Polyline();
            if (pt1.GetPtDir(pt2) == -1)
            {
                pts.Add(new Point2d(pt1.X, pt1.Y));
                pts.Add(new Point2d(pt1.X, pt2.Y));
                pts.Add(new Point2d(pt2.X, pt2.Y));
                pts.Add(new Point2d(pt2.X, pt1.Y));
                pts.Add(new Point2d(pt1.X, pt1.Y));
                pline.CreatePolyline(pts);
                return pline;
            }
            return null;
        }

        /// <summary>
        /// 对 1个1 或者 3个1 的情况进行排序，flag为true表示3个1
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="directions"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        private static List<Point3d> SumEqSort(List<Point3d> pts, List<int> directions, bool flag)
        {
            int firstDir = Convert.ToInt32(!flag);
            int secondDir = Convert.ToInt32(flag);
            var sortPt = new List<Point3d>();
            var curPt = new Point3d();
            for (int i = 0; i < directions.Count; i++)
            {
                if (directions[i] == firstDir)
                {
                    curPt = pts[i];//获取方向0的点
                    break;
                }
            }
            for (int i = 0; i < directions.Count; i++)
            {
                if (directions[i] == secondDir)
                {
                    if (curPt.GetPtDir(pts[i]) == firstDir)
                    {
                        sortPt.Add(pts[i]);//获取优先分割点
                    }
                }
            }
            for (int i = 0; i < directions.Count; i++)
            {
                if (directions[i] == secondDir)
                {
                    if (curPt.GetPtDir(pts[i]) == secondDir)
                    {
                        if (sortPt.Count == 0)
                        {
                            sortPt.Add(curPt);//添加为1分割点
                        }

                        sortPt.Add(pts[i]);//获取最后分割点
                    }
                }
            }
            return sortPt;
        }

        private static List<Point3d> SumEq2Sort(List<Point3d> pts, List<int> directions, out int flag)
        {
            var sortPt = new List<Point3d>();
            var zeroPt = new List<Point3d>();
            var onePt = new List<Point3d>();
            for (int i = 0; i < directions.Count; i++)
            {
                if (directions[i] == 0)
                {
                    zeroPt.Add(pts[i]);//获取方向0的点
                }
                else
                {
                    onePt.Add(pts[i]);//获取方向1的点
                }
            }
            if (zeroPt[0].GetPtDir(zeroPt[1]) == 0)//横向两个0
            {
                sortPt.Add(zeroPt[0]);
                sortPt.AddRange(onePt);
                flag = 0;
            }
            else if (zeroPt[0].GetPtDir(zeroPt[1]) == 1)//纵向两个 0
            {
                sortPt.Add(onePt[0]);
                sortPt.AddRange(zeroPt);
                flag = 1;
            }
            else
            {
                //TODO: 0101交替的情况暂无好的解决方案
                flag = -1;
            }
            return sortPt;
        }
    }
}
