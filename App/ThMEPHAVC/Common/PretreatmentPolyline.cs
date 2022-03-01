using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;

namespace ThMEPHVAC.Common
{
    class PretreatmentPolyline
    {
        private List<Polyline> closedCurves = new List<Polyline>();
        private List<Curve> unClosedCurves = new List<Curve>();// 非闭合数据
        private List<Curve> targetCurves = new List<Curve>();
        private ThMEPOriginTransformer originTransformer = null;
        public PretreatmentPolyline(List<Curve> selectCurves) 
        {
            targetCurves = new List<Curve>();
            
            var pt = selectCurves.First().StartPoint;
            originTransformer = new ThMEPOriginTransformer(pt);
            selectCurves.ForEach(c => 
            {
                var copy = c.Clone() as Curve;
                originTransformer.Transform(copy);
                targetCurves.Add(copy);
            });
        }
        void GetClosePolylines(double headTailTolerance =1000) 
        {
            closedCurves = new List<Polyline>();
            unClosedCurves = new List<Curve>();
            foreach (var curve in targetCurves)
            {
                if (curve is Polyline poly)
                {
                    if (poly.Closed)
                    {
                        closedCurves.Add(poly);
                    }
                    else 
                    {
                        if (poly.Area < 10)
                        {
                            var obj = new DBObjectCollection();
                            poly.Explode(obj);
                            unClosedCurves.AddRange(obj.OfType<Curve>().ToList());
                        }
                        else if (!poly.Closed && poly.StartPoint.DistanceTo(poly.EndPoint) < headTailTolerance)
                        {
                            poly.Closed = true;
                            closedCurves.Add(poly);
                        }
                        else 
                        {
                            var obj = new DBObjectCollection();
                            poly.Explode(obj);
                            unClosedCurves.AddRange(obj.OfType<Curve>().ToList());
                        }
                    }
                }
                else
                {
                    unClosedCurves.Add(curve);
                }
            }
        }
        void ExtendInnerCurves(double maxExtendLength) 
        {
            //这里只延长闭合区域内部的线
            if (closedCurves.Count<1 || unClosedCurves.Count < 1)
                return;
            var objs = new DBObjectCollection();
            unClosedCurves.ForEach(x =>
            {
                objs.Add(x);
            });
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            foreach (var polyline in closedCurves) 
            {
                var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();
                sprayLines = sprayLines.SelectMany(x => polyline.Trim(x).OfType<Curve>().ToList()).ToList();
                if (sprayLines.Count < 1)
                    continue;
                foreach (var line in sprayLines) 
                {
                    var lineSp = line.StartPoint;
                    var lineEp = line.EndPoint;
                    var lineDir = line.CurveDirection();
                    var nearSPoint = polyline.GetClosestPointTo(lineSp, false);
                    var nearEPoint = polyline.GetClosestPointTo(lineEp, false);
                    var nearSDis = double.MaxValue;
                    var nearEDis = double.MaxValue;
                    var nearSP = Point3d.Origin;
                    var nearEP = Point3d.Origin;
                    foreach (var target in sprayLines) 
                    {
                        if (target == line)
                            continue;
                        var targetNearSPoint = target.GetClosestPointTo(lineSp,false);
                        var targetNearEPoint = target.GetClosestPointTo(lineEp, false);
                        var sDis = targetNearSPoint.DistanceTo(lineSp);
                        var eDis = targetNearEPoint.DistanceTo(lineEp);
                        if (sDis < nearSDis) 
                        {
                            var dotDir = (targetNearSPoint - lineSp).DotProduct(lineDir);
                            if (dotDir < 0.01)
                            {
                                nearSDis = sDis;
                                nearSP = targetNearSPoint;
                            }
                        }
                        if (eDis < nearEDis) 
                        {
                            var dotDir = (targetNearSPoint - lineSp).DotProduct(lineDir);
                            if (dotDir > -0.01)
                            {
                                nearEDis = eDis;
                                nearEP = targetNearEPoint;
                            }
                        }
                    }
                    var lineSDis = lineSp.DistanceTo(nearSPoint);
                    var lineEDis = lineEp.DistanceTo(nearEPoint);
                    if (nearSDis < lineSDis && nearSDis < maxExtendLength) 
                    {
                        lineSp = nearSP;
                    }
                    else if (lineSDis < maxExtendLength)
                        lineSp = nearSPoint;
                    if (nearEDis < lineEDis && nearEDis < maxExtendLength)
                    {
                        lineEp = nearEP;
                    }
                    else if (nearEPoint.DistanceTo(lineEp) < maxExtendLength)
                        lineEp = nearEPoint;
                    unClosedCurves.Add(new Line(lineSp, lineEp));
                }
            }
        }
        public Dictionary<Polyline, List<Polyline>> CalcFrameHoles(double headTailTolerance = 1000) 
        {
            var resDic = new Dictionary<Polyline, List<Polyline>>();
            GetClosePolylines(headTailTolerance);
            // 清洗外框线（MakeValid)
            closedCurves = closedCurves.Select(o => ThMEPFrameService.Normalize(o)).ToList();
            ExtendInnerCurves(headTailTolerance);
            //分割外包框线
            var cuvFrames = closedCurves.Cast<Curve>().ToList();
            foreach (var item in unClosedCurves) 
            {
                //这里只处理线
                if (item is Line line)
                {
                    var sp = line.StartPoint;
                    var ep = line.EndPoint;
                    var dir = line.CurveDirection();
                    cuvFrames.Add(new Line(sp - dir.MultiplyBy(2), ep + dir.MultiplyBy(2)));
                }
                else 
                {
                    cuvFrames.Add(item);
                }
            }
            var objs = cuvFrames.OfType<DBObject>().ToCollection();
            var obLst = objs.PolygonsEx();

            List<Polyline> resFrames = new List<Polyline>();
            foreach (var ob in obLst)
            {
                if (ob is Polyline resPoly)
                {
                    resFrames.Add(resPoly);
                }
                else if (ob is MPolygon mPolygon) 
                {
                    var outPLine = mPolygon.Shell();
                    resFrames.Add(outPLine);
                }
            }
            var holeInfo = CalHoles(resFrames);

            foreach (var pline in holeInfo)
            {
                var copyOut = (Polyline)pline.Key.Clone();
                originTransformer.Reset(copyOut);
                var innerPLines = new List<Polyline>();
                if (pline.Value != null)
                {
                    foreach (var item in pline.Value)
                    {
                        var copyInner = (Polyline)item.Clone();
                        originTransformer.Reset(copyInner);
                        innerPLines.Add(copyInner);
                    }
                }
                resDic.Add(copyOut, innerPLines);
            }
            return resDic;
        }
        /// <summary>
        /// 计算外包框和其中的洞
        /// </summary>
        /// <param name="frames"></param>
        /// <returns></returns>
        Dictionary<Polyline, List<Polyline>> CalHoles(List<Polyline> frames)
        {
            frames = frames.OrderByDescending(x => x.Area).ToList();

            Dictionary<Polyline, List<Polyline>> holeDic = new Dictionary<Polyline, List<Polyline>>(); //外包框和洞口
            while (frames.Count > 0)
            {
                var firFrame = frames[0];
                frames.Remove(firFrame);

                var bufferFrames = firFrame.Buffer(1)[0] as Polyline;
                var holes = frames.Where(x => bufferFrames.Contains(x)).ToList();
                frames.RemoveAll(x => holes.Contains(x));

                holeDic.Add(firFrame, holes);
            }

            return holeDic;
        }
    }
}
