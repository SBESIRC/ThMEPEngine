using System;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.BeamInfo.Model;
using ThMEPEngineCore.BeamInfo.Utils;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Interface;

namespace ThMEPEngineCore.BeamInfo.Business
{
    public class CalBeamStruService : ICalculateBeam
    {
        /// <summary>
        /// 将梁线按照规则分类成很多类
        /// </summary>
        /// <param name="dBObjects"></param>
        /// <returns></returns>
        public List<Beam> GetBeamInfo(DBObjectCollection dBObjects)
        {
            List<Beam> allBeam = new List<Beam>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                List<Arc> arcs = new List<Arc>();
                Dictionary<Vector3d, List<Line>> groupDic = new Dictionary<Vector3d, List<Line>>();
                foreach (DBObject obj in dBObjects)
                {
                    if (obj is Line line)
                    {
                        // 忽略Z值不为零的情况
                        var lNormal = Direction(line);
                        if (!lNormal.IsEqualTo(new Vector3d(lNormal.X, lNormal.Y, 0.0)))
                        {
                            continue;
                        }

                        var norComp = groupDic.Keys.Where(x => x.IsParallelToEx(lNormal)).ToList();
                        if (norComp.Count > 0)
                        {
                            groupDic[norComp.First()].Add(line);
                        }
                        else
                        {
                            groupDic.Add(lNormal, new List<Line>() { line });
                        }
                    }
                    else if (obj is Arc arcLine)
                    {
                        arcs.Add(arcLine);
                    }
                }

                // TODO: Z归零应该在前面预处理时完成
                //将所有线的法相z值归零（不为0构建坐标系会出错
                groupDic = groupDic.ToDictionary(x => x.Key.Z == 0 ? x.Key : new Vector3d(x.Key.X, x.Key.Y, 0), k => k.Value);

                // 根据组内所有的线段识别出所有梁
                foreach (var lineDic in groupDic)
                {
                    //合并同组内重叠的线
                    var objs = new DBObjectCollection();
                    lineDic.Value.ForEachDbObject(o => objs.Add(o));
                    var results = objs.GetMergeOverlappingCurves(new Tolerance(1, 1));
                    var res = GetLineBeamObject(results.Cast<Line>().ToList(), lineDic.Key, 100);
                    allBeam.AddRange(res);
                }

                // 处理弧梁
                allBeam.AddRange(GetArcBeamObject(arcs, 1500, 0.5));
            }

            return allBeam;
        }

        private Vector3d Direction(Line line)
        {
            return line.StartPoint.GetVectorTo(line.EndPoint).GetNormal();
        }

        /// <summary>
        /// 获取直线梁
        /// </summary>
        /// <param name="linDic"></param>
        /// <param name="lineDir"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        private List<LineBeam> GetLineBeamObject(List<Line> linList, Vector3d lineDir, double tolerance)
        {
            Vector3d zDir = Vector3d.ZAxis;
            Vector3d yDir = Vector3d.ZAxis.CrossProduct(lineDir);
            Matrix3d trans = new Matrix3d(new double[]{
                    lineDir.X, yDir.X, zDir.X, 0,
                    lineDir.Y, yDir.Y, zDir.Y, 0,
                    lineDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0});

            //将所有线转到自建坐标系,方便比较
            linList = linList.OrderBy(x =>
            {
                x.TransformBy(trans.Inverse());
                return x.StartPoint.Y;
            }).ToList();

            var linePair = linList.First();
            List<LineBeam> beamLst = new List<LineBeam>();
            while (linList.Count > 0)
            {
                Line firLine = linePair;
                linList.Remove(linePair);
                double lMaxX = firLine.StartPoint.X;
                double lMinX = firLine.EndPoint.X;
                if (firLine.StartPoint.X < firLine.EndPoint.X)
                {
                    lMaxX = firLine.EndPoint.X;
                    lMinX = firLine.StartPoint.X;
                }
                var paraLines = linList.Where(x =>
                {
                    double xMaxX = x.StartPoint.X;
                    double xMinX = x.EndPoint.X;
                    if (x.StartPoint.X < x.EndPoint.X)
                    {
                        xMaxX = x.EndPoint.X;
                        xMinX = x.StartPoint.X;
                    }

                    //两根线距离太宽或者太近也不认为是一组梁
                    if (Math.Abs(firLine.StartPoint.Y - x.StartPoint.Y) > 1500 || Math.Abs(firLine.StartPoint.Y - x.StartPoint.Y) < 10)
                    {
                        return false;
                    }

                    //  两根线有重叠关系(大部分重叠)
                    if ((xMinX <= lMaxX && xMaxX >= lMinX) &&
                        (Math.Abs(xMaxX - lMaxX) < firLine.Length / 2 ||
                        Math.Abs(xMinX - lMinX) < firLine.Length / 2))
                    {
                        return true;
                    }

                    // 不可能配成梁的线
                    return false;
                }).ToList();

                if (paraLines.Count > 0)
                {
                    if (paraLines.First().Length > firLine.Length)  //如果梁下边线长度大于上边线，那么就会有其他上边线遗漏
                    {
                        linList.Add(linePair);
                        linList = linList.OrderBy(x => x.StartPoint.Y).ToList();
                        linePair = paraLines.First();
                        continue;
                    }

                    firLine.TransformBy(trans);
                    double sum = 0;
                    List<Line> matchLines = new List<Line>();
                    foreach (var plineDic in paraLines)
                    {
                        var thisLine = plineDic;
                        sum += thisLine.Length;
                        if ((sum > firLine.Length && Math.Abs(sum - firLine.Length) > tolerance))
                        {
                            break;
                        }

                        var tempMacthNum = matchLines.Where(x =>
                        {
                            double xMaxX = x.StartPoint.X;
                            double xMinX = x.EndPoint.X;
                            if (x.StartPoint.X < x.EndPoint.X)
                            {
                                xMaxX = x.EndPoint.X;
                                xMinX = x.StartPoint.X;
                            }

                            double mMaxX = thisLine.StartPoint.X;
                            double mMinX = thisLine.EndPoint.X;
                            if (thisLine.StartPoint.X < thisLine.EndPoint.X)
                            {
                                mMaxX = thisLine.EndPoint.X;
                                mMinX = thisLine.StartPoint.X;
                            }

                            if (mMaxX <= xMinX || mMinX >= xMaxX)
                            {
                                return false;
                            }
                            return true;
                        });
                        if (tempMacthNum.Count() > 0)
                        {
                            break;
                        }

                        matchLines.Add(thisLine.Clone() as Line);
                        thisLine.TransformBy(trans);
                        LineBeam beam = new LineBeam(firLine, thisLine)
                        {
                            UpBeamLine = linePair,
                            DownBeamLine = plineDic
                        };
                        beamLst.Add(beam);
                        linList.Remove(plineDic);
                    }
                }
                else
                {
                    firLine.TransformBy(trans);
                }

                if (linList.Count > 0)
                {
                    linePair = linList.First();
                }
            }

            return beamLst;
        }

        private List<Tuple<Arc, Arc>> ArcBeamPairsExtract(List<Arc> arcs, double DistThreshold, double overlapAngleThreshold)
        {
            var arcSegments = new List<Tuple<Arc, Arc>>();

            // 过滤过短的Arc
            foreach (var item in arcs)
            {
                if (item.Length <= 10) arcs.Remove(item);
            }

            // 如果出现重复弧线，则删除重复段，防止出现重复弧梁
            for (int i = 0; i < arcs.Count; i++)
            {
                for (int j = i + 1; j < arcs.Count; j++)
                {
                    if (IsRepeteArcs(arcs[i], arcs[j]))
                    {
                        arcs[i] = (arcs[i].Length >= arcs[j].Length) ? (GetObjectUtils.ArcMerge(arcs[i], arcs[j])) : (arcs[i] = GetObjectUtils.ArcMerge(arcs[j], arcs[i]));
                        arcs.RemoveAt(j);
                    }
                }
            }

            // 检测两条Arc是否构成Arc对
            for (int i = 0; i < arcs.Count; i++)
            {
                // 将Arc_i转换成polyline，并取出每一个端点
                var polyline = arcs[i].TessellateWithChord(arcs[i].Radius * (Math.Sin(Math.PI / 1440.0))).ToDbPolyline();
                var polylineSegments = new PolylineSegmentCollection(polyline);
                var pt1 = new List<Point2d>();
                foreach (var segment in polylineSegments)
                {
                    pt1.Add(segment.StartPoint);
                }
                pt1.Add(polylineSegments.EndPoint);
                for (int j = i + 1; j < arcs.Count; j++)
                {
                    // 计算两个Arc间的距离（近似）
                    var dist = pt1.Min(pt => arcs[j].GetDistToPoint(pt.ToPoint3d()));
                    var overlapEstimate = GetObjectUtils.OverlapAngle(arcs[i], arcs[j]);
                    //判断两条曲边是否构成同一曲梁 => 判定条件： 曲边间距 <= DistThreshold，曲边不相交，扇形区域有重合且重合范围大于小段弧长的overlapAngleThreshold
                    if ((dist <= DistThreshold) && (dist > arcs[i].Radius * (Math.Sin(Math.PI / 1440.0))) && overlapEstimate.Item1 &&
                        overlapEstimate.Item4 >= overlapAngleThreshold * Math.Min(arcs[i].TotalAngle, arcs[j].TotalAngle))
                    {
                        arcSegments.Add(Tuple.Create(arcs[i], arcs[j]));
                    }
                }
            }
            // 合并可合并的Arcs
            return MergeArcs(arcSegments);
        }

        // 检测Arc对中，是否有可合并的两对Arc
        private List<Tuple<Arc, Arc>> MergeArcs(List<Tuple<Arc, Arc>> arcs)
        {
            for (int i = 0; i < arcs.Count; i++)
            {
                for (int j = i + 1; j < arcs.Count; j++)
                {
                    if (arcs[i].Item1 == arcs[j].Item1
                        && arcs[i].Item2.Center.DistanceTo(arcs[j].Item2.Center) <= 1e-4
                        && (arcs[i].Item2.Radius - arcs[j].Item2.Radius) <= 1e-4
                        && GetObjectUtils.OverlapAngle(arcs[i].Item2, arcs[j].Item2).Item1)
                    {
                        arcs[i] = Tuple.Create(arcs[i].Item1, GetObjectUtils.ArcMerge(arcs[i].Item2, arcs[j].Item2));
                        arcs.Remove(arcs[j]);
                    }
                    else if (arcs[i].Item1 == arcs[j].Item2
                        && arcs[i].Item2.Center.DistanceTo(arcs[j].Item1.Center) <= 1e-4
                        && (arcs[i].Item2.Radius - arcs[j].Item1.Radius) <= 1e-4
                        && GetObjectUtils.OverlapAngle(arcs[i].Item2, arcs[j].Item1).Item1)
                    {
                        arcs[i] = Tuple.Create(arcs[i].Item1, GetObjectUtils.ArcMerge(arcs[i].Item2, arcs[j].Item1));
                        arcs.Remove(arcs[j]);
                    }
                    else if (arcs[i].Item2 == arcs[j].Item1
                        && arcs[i].Item1.Center.DistanceTo(arcs[j].Item2.Center) <= 1e-4
                        && (arcs[i].Item1.Radius - arcs[j].Item2.Radius) <= 1e-4
                        && GetObjectUtils.OverlapAngle(arcs[i].Item1, arcs[j].Item2).Item1)
                    {
                        arcs[i] = Tuple.Create(GetObjectUtils.ArcMerge(arcs[i].Item1, arcs[j].Item2), arcs[i].Item2);
                        arcs.Remove(arcs[j]);
                    }
                    else if (arcs[i].Item2 == arcs[j].Item2
                        && arcs[i].Item1.Center.DistanceTo(arcs[j].Item1.Center) <= 1e-4
                        && (arcs[i].Item1.Radius - arcs[j].Item1.Radius) <= 1e-4
                        && GetObjectUtils.OverlapAngle(arcs[i].Item1, arcs[j].Item1).Item1)
                    {
                        arcs[i] = Tuple.Create(GetObjectUtils.ArcMerge(arcs[i].Item1, arcs[j].Item1), arcs[i].Item2);
                        arcs.Remove(arcs[j]);
                    }
                }
            }
            return arcs;
        }

        // 判断Arcs是否重复
        private bool IsRepeteArcs(Arc arc1, Arc arc2)
        {
            var overlapEstimate = GetObjectUtils.OverlapAngle(arc1, arc2);
            var startAngle = overlapEstimate.Item2;
            var endAngle = overlapEstimate.Item3;

            // 排除两段Arc没有重叠区域的情况
            if (!overlapEstimate.Item1) return false;

            // 排除两段Arc仅仅首尾相接的情况
            if (overlapEstimate.Item4 == 0.0 && startAngle == endAngle) return false;

            // 计算两段Arc重叠范围内的最大距离 
            // 目前取出重叠范围的算法有问题，应当保证取出的重叠范围的length相近
            var arc1_new = new Arc(arc1.Center, arc1.Radius, startAngle, endAngle);
            var arc2_new = new Arc(arc2.Center, arc2.Radius, startAngle, endAngle);

            // 排除重叠长度过小的情况
            if (arc1_new.Length <= 10 || arc2_new.Length <= 10)  return false;

            if (arc1_new.Length > arc2_new.Length)
            {
                var arc_temp = arc2_new;
                arc2_new = arc1_new;
                arc1_new = arc_temp;
            }

            var polyline = arc1_new.TessellateWithChord(arc1_new.Radius * (Math.Sin(Math.PI / 1440.0))).ToDbPolyline();
            var polylineSegments = new PolylineSegmentCollection(polyline);
            var pt1 = new List<Point2d>();
            foreach (var segment in polylineSegments)
            {
                pt1.Add(segment.StartPoint);
            }
            pt1.Add(polylineSegments.EndPoint);
            var dist_max = pt1.Max(pt => arc2_new.GetDistToPoint(pt.ToPoint3d()));
            //var dist_start = arc1_new.StartPoint.DistanceTo(arc2_new.StartPoint);
            //var dist_end = arc1_new.EndPoint.DistanceTo(arc2_new.EndPoint);
            if (dist_max <= arc1_new.Radius * (Math.Sin(Math.PI / 1440.0)))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取弧梁（扩展）
        /// </summary>
        /// <param name="arcs"></param>
        /// <returns></returns>
        private List<ArcBeam> GetArcBeamObject(List<Arc> arcs, double distThreshold, double overlapAngleThreshold)
        {
            List<ArcBeam> beam = new List<ArcBeam>();
            var arcPairs = ArcBeamPairsExtract(arcs, distThreshold, overlapAngleThreshold);
            arcPairs.ForEach(o => beam.Add(new ArcBeam(o.Item1, o.Item2)));
            return beam;
        }
    }
}
