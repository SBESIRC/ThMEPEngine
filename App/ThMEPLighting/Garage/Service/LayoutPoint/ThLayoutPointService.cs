﻿using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service.LayoutPoint
{
    public abstract class ThLayoutPointService
    {
        /// <summary>
        /// 预留的边界长度
        /// </summary>
        public double Margin { get; set; }
        /// <summary>
        /// 灯间距
        /// </summary>
        public double Interval { get; set; }
        public double LampLength { get; set; }
        /// <summary>
        /// 双排偏移距离
        /// </summary>
        public double DoubleRowOffsetDis { get; set; }
        public abstract List<Point3d> Layout(List<Line> dxLines);
        public abstract List<Point3d> Layout(List<Line> firstLines,List<Line> secondLines);
        protected List<Point3d> SequencePoints(List<Line> lines)
        {
            // lines必须是首位排序过的
            var results = new List<Point3d>();
            for(int i =0;i<lines.Count;i++)
            {
                results.Add(lines[i].StartPoint);
                if(i == lines.Count-1)
                {
                    results.Add(lines[i].EndPoint);
                }
            }
            return results;
        }

        protected List<Point3d> LinearDistribute(List<Line> lines,double margin,double interval)
        {
            // Lmin = D*(N-1)+1600
            var results = new List<Point3d>();
            lines.ForEach(l =>
            {
                var lineParameter = new ThLineSplitParameter
                {
                    Margin = margin,
                    Interval = interval,
                    Segment = new List<Point3d> { l.StartPoint, l.EndPoint },
                };
                results.AddRange(lineParameter.DistributeLinearSegment());
            });
            return results;
        }

        protected List<Point3d>  GetL2LayoutPointByPass(List<Point3d> L1LayoutPoints,List<Line> L1Lines, List<Line> L2Lines)
        {
            // 把L1布置的点偏移到L2上
            var results = new List<Point3d>();
            var l1LinePointDic = ThQueryPointService.Query(L1LayoutPoints, L1Lines);
            var firstPairService = new ThFirstSecondPairService(L1Lines, L2Lines, DoubleRowOffsetDis);
            L1Lines.ForEach(l =>
            {
                l1LinePointDic[l].ForEach(p =>
                {
                    foreach (var second in firstPairService.Query(l))
                    {
                        var position = p.GetProjectPtOnLine(second.StartPoint, second.EndPoint);
                        if (position.IsPointOnCurve(second,1.0) && IsFitToInstall(position,
                            second.StartPoint, second.EndPoint))
                        {
                            results.Add(position);
                            break;
                        }
                    }
                });
            });

            return results;
        } 

        protected bool IsFitToInstall(Point3d lightPt,Point3d lineSp,Point3d lineEp,double sideTolerance=2.0)
        {
            var vec = lineSp.GetVectorTo(lineEp).GetNormal();
            var lightSp = lightPt - vec.MultiplyBy(sideTolerance);
            var lightEp = lightPt + vec.MultiplyBy(sideTolerance);
            return ThGeometryTool.IsPointOnLine(lineSp, lineEp, lightSp) &&
                ThGeometryTool.IsPointOnLine(lineSp, lineEp, lightEp);
        }
        /// <summary>
        /// 计算1、2号线公共部分；1号线私有的部分；2号线私有的部分
        /// </summary>
        /// <param name="L1Lines">1号线</param>
        /// <param name="L2Lines">2号线</param>
        /// <returns>公共部分。1号线私有部分</returns>
        protected L1L2LinesInfo CalculatePubExclusiveLines(List<Line> L1Lines, List<Line> L2Lines)
        {
            return new L1L2LinesInfo(L1Lines, L2Lines, DoubleRowOffsetDis);
        }
    }

    public class L1L2LinesInfo
    {
        /// <summary>
        /// L1和L2公共的部分（线在L1上）
        /// </summary>
        public List<Line> L1Pubs { get; private set; }
        /// <summary>
        /// L1私有的部分
        /// </summary>
        public List<Line> L1Exclusives { get; private set; }
        /// <summary>
        /// L2和L1公共的部分（线在L2上）
        /// (暂时不需要，未能生成)
        /// </summary>
        public List<Line> L2Pubs { get; private set; } 
        /// <summary>
        /// L2私有的部分
        /// </summary>
        public List<Line> L2Exclusives { get; private set; } 

        internal L1L2LinesInfo(List<Line> l1Lines,List<Line> l2Lines,double doubleRowOffsetDis)
        {
            L1Pubs = new List<Line>();
            L2Pubs = new List<Line>();
            L1Exclusives = new List<Line>();
            L2Exclusives = new List<Line>();
            Calculate(l1Lines, l2Lines, doubleRowOffsetDis);
        }
        private void Calculate(List<Line> L1Lines, List<Line> L2Lines, double doubleRowOffsetDis)
        {
            var firstSecondPairService1 = new ThFirstSecondPairService(
                L1Lines, L2Lines, doubleRowOffsetDis);
            var firstPublicLines = firstSecondPairService1.Intersection(); // 1号线和2号线的公共部分
            var firstExclusiveLines = firstSecondPairService1.Difference();// 1号线减去公共部分剩下的

            var firstSecondPairService2 = new ThFirstSecondPairService(
                L2Lines, L1Lines, doubleRowOffsetDis);
            var secondExclusiveLines = firstSecondPairService2.Difference(); // 2号线减去公共部分剩下的
            //var secondPubLines = firstSecondPairService2.Intersection(); // 2号线和1号线公共的部分   

            this.L1Pubs = firstPublicLines;
            L1Exclusives = firstExclusiveLines;
            L2Exclusives = secondExclusiveLines;
            //this.L2Pubs = secondPubLines;
        }
        public void Print()
        {
            L1Pubs.OfType<Entity>().ToList().CreateGroup(AcHelper.Active.Database, 1);
            L1Exclusives.OfType<Entity>().ToList().CreateGroup(AcHelper.Active.Database, 2);
            L2Exclusives.OfType<Entity>().ToList().CreateGroup(AcHelper.Active.Database, 3);
        }
    }
}
