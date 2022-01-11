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
using ThCADExtension;
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;
using ThMEPArchitecture.ParkingStallArrangement.General;
using ThMEPArchitecture.ParkingStallArrangement.Method;

namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{
    class SegBreak
    {
        List<List<Line>> ListOfBreakedLines;
        List<List<int>> ListOfLowerBounds;
        List<List<int>> ListOfUpperBounds;
        //输入，初始分割线，以及打断的方向。输出，分割线与其交点
        public void BreakLines(List<Line> SegLines,bool VerticalDirection,bool GoPositive)
        {
            // GoPositive 从下至上打断，从左至右打断（坐标增加顺序）
            List<Line> VertLines = new List<Line>();//垂直线
            List<Line> HorzLines = new List<Line>();//水平线
            foreach (Line line in SegLines)
            {
                if (line.StartPoint.X== line.EndPoint.X)
                {
                    //横坐标相等，平行线
                    HorzLines.Add(new Line(line.StartPoint, line.EndPoint));
                }
                else
                {
                    VertLines.Add(new Line(line.StartPoint, line.EndPoint));
                }
            }
            List<List<Line>> ListOfBreakedLines = new List<List<Line>>();
            List<List<int>> ListOfLowerBounds = new List<List<int>>();// 打断线的下边界
            List<List<int>> ListOfUpperBounds = new List<List<int>>();// 打断线的上边界
            if (VerticalDirection)
            {
                //打断纵向线
                foreach (Line line1 in VertLines)
                {
                    List<Point3d> ptlist = new List<Point3d>();//断点列表
                    List<Line> IntersectLines = new List<Line>();//交叉线列表
                    foreach (Line line2 in HorzLines)
                    {
                        var templ = line1.Intersect(line2, Intersect.OnBothOperands);
                        if (templ.Count != 0)
                        {
                            ptlist.Add(templ.First());// 添加打断点
                            IntersectLines.Add(new Line(line2.StartPoint, line2.EndPoint));// 添加线的复制
                        }
                    }
                    if (ptlist.Count > 2) 
                    {
                        List<Line> BreakedLines = new List<Line>();// 打断后的纵线list
                        List<int> LowerBounds = new List<int>();// 打断线的下边界
                        List<int> UpperBounds = new List<int>();// 打断线的上边界
                        // 该纵线打断
                        if (GoPositive)
                        {
                            // 1.TODO:sort ptlist and IntersectLines(base on pt list)按照纵坐标排序
                            // 2. 确定打断后的纵线
                                
                            Point3d spt;
                            if (line1.StartPoint.Y > line1.EndPoint.Y)
                            {
                                spt = line1.StartPoint;
                            }
                            else
                            {
                                spt = line1.EndPoint;
                            }
                            int CurCount = 0;
                            List < int > InnerIndex = new List<int>();// 记录在当前断线上所有横线的索引
                            Line BreakLine ;
                            for (int i = 0; i < ptlist.Count; ++i)
                            {
                                // 双指针确定断线

                                if (CurCount < 1)
                                {
                                    CurCount += 1;
                                    InnerIndex.Add(i);
                                }
                                else
                                {
                                    // 新断线
                                    InnerIndex.Add(i);
                                    if (i != ptlist.Count - 2)
                                    {
                                        BreakLine = new Line(spt, ptlist[i]);
                                        //不为倒数第二个点，则直接添加
                                        BreakedLines.Add(BreakLine);
                                        spt = ptlist[i];
                                            
                                        // TO DO: 确定断线范围，buffer，取建筑或者buffer值（添加 Lower & Upper Bound to Lower & upper Bounds)
                                        // TO DO: 更新断线上所有横向线（拉伸，覆盖断线的最大范围）

                                        //重新计数
                                        InnerIndex = new List<int>();
                                        InnerIndex.Add(i);
                                        CurCount = 0;
                                    }

                                }
                            }
                        }
                        else
                        {
                            ;//对称逻辑
                        }
                        ListOfBreakedLines.Add(BreakedLines);
                        ListOfLowerBounds.Add(LowerBounds);
                        ListOfUpperBounds.Add(UpperBounds);
                    }
                    
                }
            }
            else
            {
                ;//与上面对称
            }
        }
    }
}
