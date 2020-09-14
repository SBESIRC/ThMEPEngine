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
using TianHua.AutoCAD.Utility.ExtensionTools;
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
                allBeam.AddRange(GetArcBeamObject(arcs, 1500));
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

        private List<Tuple<Arc, Arc>> ArcBeamPairsExtract(List<Arc> arcs, double DistThreshold)
        {
            var arcSegments = new List<Tuple<Arc, Arc>>();

            // 检测两条Arc是否构成Arc对
            for (int i = 0, count = arcs.Count; i < count; i++)
            {
                // 将Arc_i转换成polyline，并取出每一个端点
                var polyline = arcs[i].TessellateWithChord(arcs[i].Radius * (Math.Sin(Math.PI / 360.0))).ToDbPolyline();
                var polylineSegments = new PolylineSegmentCollection(polyline);
                var pt1 = new List<Point2d>();
                foreach (var segment in polylineSegments)
                {
                    pt1.Add(segment.StartPoint);
                }
                pt1.Add(polylineSegments.EndPoint);
                for (int j = i + 1; j < count; j++)
                {
                    // 计算两个Arc间的距离（近似）
                    var dist = pt1.Min(pt => arcs[j].GetDistToPoint(pt.ToPoint3d()));
                    //判断两条曲边是否构成同一曲梁 => 判定条件： 曲边间距 <= DistThreshold，曲边不相交，扇形区域有重合且重合范围大于小段弧的一半
                    if (dist <= DistThreshold && dist > 1 && ArcOverlapAngle(arcs[i], arcs[j]) >= 0.5 * Math.Min(arcs[i].TotalAngle, arcs[j].TotalAngle))
                    {
                        arcSegments.Add(Tuple.Create(arcs[i], arcs[j]));
                    }
                }
            }
            // 合并可合并的Arcs
            var arcsMerge = ArcMergeEx(arcSegments);
            return arcsMerge;
        }

        // 检测Arc对中，是否有可合并的两段Arc
        private List<Tuple<Arc, Arc>> ArcMergeEx(List<Tuple<Arc, Arc>> arcs)
        {
            for (int i = 0; i < arcs.Count; i++)
            {
                for (int j = i + 1; j < arcs.Count; j++)
                {
                    if (arcs[i].Item1 == arcs[j].Item1 
                        && arcs[i].Item2.Center.DistanceTo(arcs[j].Item2.Center) <= 1e-4
                        && (arcs[i].Item2.Radius - arcs[j].Item2.Radius) <= 1e-4)
                    {
                        arcs[i] = Tuple.Create(arcs[i].Item1,ArcMerge(arcs[i].Item2, arcs[j].Item2));
                        arcs.Remove(arcs[j]);
                    }
                    else if (arcs[i].Item1 == arcs[j].Item2
                        && arcs[i].Item2.Center.DistanceTo(arcs[j].Item1.Center) <= 1e-4
                        && (arcs[i].Item2.Radius - arcs[j].Item1.Radius) <= 1e-4)
                    {
                        arcs[i] = Tuple.Create(arcs[i].Item1, ArcMerge(arcs[i].Item2, arcs[j].Item1));
                        arcs.Remove(arcs[j]);
                    }
                    else if (arcs[i].Item2 == arcs[j].Item1 
                        && arcs[i].Item1.Center.DistanceTo(arcs[j].Item2.Center) <= 1e-4
                        && (arcs[i].Item1.Radius - arcs[j].Item2.Radius) <= 1e-4)
                    {
                        arcs[i] = Tuple.Create(ArcMerge(arcs[i].Item1, arcs[j].Item2), arcs[i].Item2);
                        arcs.Remove(arcs[j]);
                    }
                    else if (arcs[i].Item2 == arcs[j].Item2
                        && arcs[i].Item1.Center.DistanceTo(arcs[j].Item1.Center) <= 1e-4
                        && (arcs[i].Item1.Radius - arcs[j].Item1.Radius) <= 1e-4)
                    {
                        arcs[i] = Tuple.Create(ArcMerge(arcs[i].Item1, arcs[j].Item1), arcs[i].Item2);
                        arcs.Remove(arcs[j]);
                    }
                }
            }
            return arcs;
        }

        // 合并两段共圆心同半径的Arc
        private Arc ArcMerge(Arc arc1, Arc arc2)
        {
            var startAngle = Math.Min(arc1.StartAngle, arc2.StartAngle);
            var endAngle_1 = (arc1.StartAngle > arc1.EndAngle) ? (arc1.EndAngle + 8 * Math.Atan(1) - startAngle) : arc1.EndAngle - startAngle;
            var endAngle_2 = (arc2.StartAngle > arc2.EndAngle) ? (arc2.EndAngle + 8 * Math.Atan(1) - startAngle) : arc2.EndAngle - startAngle;
            var endAngle = Math.Max(endAngle_1, endAngle_2) + startAngle;
            endAngle = (endAngle > 8 * Math.Atan(1)) ? (endAngle - 8 * Math.Atan(1)) : endAngle;
            return new Arc(arc1.Center, arc1.Radius, startAngle, endAngle);
        }

        // 计算两段弧的重合角度
        private double ArcOverlapAngle(Arc arc1, Arc arc2)
        {
            var startAngle = Math.Min(arc1.StartAngle, arc2.StartAngle);
            var startAngle_1 = arc1.StartAngle - startAngle;
            var startAngle_2 = arc2.StartAngle - startAngle;
            var endAngle_1 = (arc1.StartAngle > arc1.EndAngle) ? (arc1.EndAngle + 8 * Math.Atan(1) - startAngle) : arc1.EndAngle - startAngle;
            var endAngle_2 = (arc2.StartAngle > arc2.EndAngle) ? (arc2.EndAngle + 8 * Math.Atan(1) - startAngle) : arc2.EndAngle - startAngle;
            return Math.Min(endAngle_1, endAngle_2) - Math.Max(startAngle_1, startAngle_2);
        }

        /// <summary>
        /// 获取弧梁（扩展）
        /// </summary>
        /// <param name="arcs"></param>
        /// <returns></returns>
        private List<ArcBeam> GetArcBeamObject(List<Arc> arcs, double distThreshold)
        {
            List<ArcBeam> beam = new List<ArcBeam>();
            var arcPairs = ArcBeamPairsExtract(arcs, distThreshold);
            arcPairs.ForEach(o => beam.Add(new ArcBeam(o.Item1, o.Item2)));
            return beam;
        }
    }
}
