using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    internal class ThSharpAngleHandleService
    {
        /// <summary>
        /// 线槽间距
        /// </summary>
        private double D { get; set; }
        private double PointTolerance;
        // 小于此值的都是锐角
        private double ShapeAngleUpperValue = 90.0;
        public DBObjectCollection Dxs { get; private set; }
        public DBObjectCollection Fdxs { get; private set; }
        public DBObjectCollection SingleRowLines { get; private set; }

        public ThSharpAngleHandleService(List<Line> dxs, List<Line> fdxs, List<Line> singleRowLines, double d)
        {
            D = d;
            Dxs = dxs.Clone().ToCollection();
            Fdxs = fdxs.Clone().ToCollection();
            SingleRowLines = singleRowLines.Clone().ToCollection();
            PointTolerance = ThGarageLightCommon.RepeatedPointDistance;
        }

        public void Handle()
        {
            //https://www.tapd.cn/45084940/prong/stories/view_des/1145084940001002411
            var objs = new DBObjectCollection();
            objs = objs.Union(Dxs);
            objs = objs.Union(Fdxs);
            objs = objs.Union(SingleRowLines);
            var points = ThLightingCrossPointService.GetNoRepeatedPoints(objs, 1.0);

            // 过滤按度的范围过滤点
            points = ThLightingCrossPointService.FilterByDegree(points, objs, 2, 4, PointTolerance);

            // 锐角处理
            Handle(points, objs);
        }

        private void Handle(Point3dCollection pts, DBObjectCollection nodedLines)
        {
            var mainVector = GetMainVector(nodedLines);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(nodedLines);
            while (pts.Count > 0)
            {
                var pt = pts[0];
                var objs = Query(pt, spatialIndex, PointTolerance);
                if (objs.Count == 2)
                {
                    var results = RemoveElbowSharp(objs[0] as Line, objs[1] as Line, pt, mainVector);
                    spatialIndex = UpdateSpatialIndex(results, spatialIndex);
                    UpdateData(results);
                }
                else if (objs.Count == 3)
                {
                    var results = RemoveThreewaySharp(objs[0] as Line, objs[1] as Line, objs[2] as Line, pt, mainVector);
                    spatialIndex = UpdateSpatialIndex(results, spatialIndex);
                    UpdateData(results);
                }
                else if (objs.Count == 4)
                {
                    var results = RemoveCrosswaySharp(objs[0] as Line, objs[1] as Line, objs[2] as Line, objs[3] as Line, pt, mainVector);
                    spatialIndex = UpdateSpatialIndex(results, spatialIndex);
                    UpdateData(results);
                }
                pts.Remove(pt);
            }
        }

        private Dictionary<Line, List<Line>> RemoveElbowSharp(Line first, Line second, Point3d joint, Vector3d mainVector)
        {
            var firstVec = GetPointedVector(first.StartPoint, first.EndPoint, joint);
            var secondVec = GetPointedVector(second.StartPoint, second.EndPoint, joint);
            if (!IsSharp(firstVec, secondVec))
            {
                return new Dictionary<Line, List<Line>>();
            }
            var firstProduct = firstVec.DotProduct(mainVector);
            var secondProduct = secondVec.DotProduct(mainVector);
            if (firstProduct < Math.Sin(1 / 180.0 * Math.PI) || firstProduct > Math.Cos(1 / 180.0 * Math.PI))
            {
                return RemoveSharp(second, first, joint, true);
            }
            else if (secondProduct < Math.Sin(1 / 180.0 * Math.PI) || secondProduct > Math.Cos(1 / 180.0 * Math.PI))
            {
                return RemoveSharp(first, second, joint, true);
            }
            else if (first.Length > second.Length)
            {
                return RemoveSharp(second, first, joint, true);
            }
            else
            {
                return RemoveSharp(first, second, joint, true);
            }
        }

        private Dictionary<Line, List<Line>> RemoveThreewaySharp(Line first, Line second, Line third, Point3d joint, Vector3d mainVector)
        {
            var infos = new List<Tuple<Line, Vector3d>>
            {
                Tuple.Create(first, GetPointedVector(first.StartPoint, first.EndPoint, joint)),
                Tuple.Create(second, GetPointedVector(second.StartPoint, second.EndPoint, joint)),
                Tuple.Create(third, GetPointedVector(third.StartPoint, third.EndPoint, joint))
            };
            var pairAngs = GetEachAngles(infos).OrderByDescending(o => o.Item3);
            var mainPair = pairAngs.First();
            if (IsSharp(mainPair.Item3) || !IsSharp(pairAngs.Last().Item3))
            {
                return new Dictionary<Line, List<Line>>();
            }
            var maxItemAng = mainPair.Item3.RadToAng();
            var equalItems = pairAngs.Where(o => Math.Abs(o.Item3.RadToAng() - maxItemAng) <= 1.0);
            if (equalItems.Count() > 1)
            {
                foreach (var pair in equalItems)
                {
                    var product = Math.Abs(infos[pair.Item1].Item1.LineDirection().DotProduct(mainVector));
                    if (product < Math.Sin(1 / 180.0 * Math.PI) || product > Math.Cos(1 / 180.0 * Math.PI))
                    {
                        mainPair = pair;
                        break;
                    }
                    var secondProduct = Math.Abs(infos[pair.Item2].Item1.LineDirection().DotProduct(mainVector));
                    if (secondProduct < Math.Sin(1 / 180.0 * Math.PI) || product > Math.Cos(1 / 180.0 * Math.PI))
                    {
                        mainPair = pair;
                        break;
                    }
                }
            }
            var main1 = infos[mainPair.Item1];
            var main2 = infos[mainPair.Item2];
            var indexes = new List<int> { 0, 1, 2 };
            indexes.Remove(mainPair.Item1);
            indexes.Remove(mainPair.Item2);
            var branch = infos[indexes[0]];
            return RemoveSharp(main1, main2, branch, joint);
        }

        private Dictionary<Line, List<Line>> RemoveCrosswaySharp(Line first, Line second, Line third, Line fourth, Point3d joint, Vector3d mainVector)
        {
            var infos = new List<Tuple<Line, Vector3d>>
            {
                Tuple.Create(first, GetPointedVector(first.StartPoint, first.EndPoint, joint)),
                Tuple.Create(second, GetPointedVector(second.StartPoint, second.EndPoint, joint)),
                Tuple.Create(third, GetPointedVector(third.StartPoint, third.EndPoint, joint)),
                Tuple.Create(fourth, GetPointedVector(fourth.StartPoint, fourth.EndPoint, joint))
            };
            var pairAngs = GetEachAngles(infos).OrderByDescending(o => o.Item3);
            var mainPair = pairAngs.First();
            if (IsSharp(mainPair.Item3) || !IsSharp(pairAngs.Last().Item3))
            {
                return new Dictionary<Line, List<Line>>();
            }
            var maxItemAng = mainPair.Item3.RadToAng();
            var equalItems = pairAngs.Where(o => Math.Abs(o.Item3.RadToAng() - maxItemAng) <= 1.0);
            if (equalItems.Count() > 1)
            {
                foreach (var pair in equalItems)
                {
                    var product = Math.Abs(infos[pair.Item1].Item1.LineDirection().DotProduct(mainVector));
                    if (product < Math.Sin(1 / 180.0 * Math.PI) || product > Math.Cos(1 / 180.0 * Math.PI))
                    {
                        mainPair = pair;
                        break;
                    }
                    var secondProduct = Math.Abs(infos[pair.Item2].Item1.LineDirection().DotProduct(mainVector));
                    if (secondProduct < Math.Sin(1 / 180.0 * Math.PI) || product > Math.Cos(1 / 180.0 * Math.PI))
                    {
                        mainPair = pair;
                        break;
                    }
                }
            }
            var main1 = infos[mainPair.Item1];
            var main2 = infos[mainPair.Item2];
            var indexes = new List<int> { 0, 1, 2, 3 };
            indexes.Remove(mainPair.Item1);
            indexes.Remove(mainPair.Item2);
            var branch1 = infos[indexes[0]];
            var branch2 = infos[indexes[1]];
            var results = new Dictionary<Line, List<Line>>();
            var results1 = RemoveSharp(main1, main2, branch1, joint);
            var results2 = RemoveSharp(main1, main2, branch2, joint);
            results1.ForEach(o => results.Add(o.Key, o.Value));
            results2.ForEach(o => results.Add(o.Key, o.Value));
            return results;
        }

        private Dictionary<Line, List<Line>> RemoveSharp(Tuple<Line, Vector3d> main1, Tuple<Line, Vector3d> main2,
            Tuple<Line, Vector3d> branch, Point3d joint)
        {
            var m1bJiaJiao = branch.Item2.GetAngleTo(main1.Item2);
            var m2bJiaJiao = branch.Item2.GetAngleTo(main2.Item2);
            if (m1bJiaJiao < m2bJiaJiao)
            {
                return IsSharp(m1bJiaJiao) ? RemoveSharp(branch.Item1, main1.Item1, joint)
                    : new Dictionary<Line, List<Line>>();
            }
            else
            {
                return IsSharp(m2bJiaJiao) ? RemoveSharp(branch.Item1, main2.Item1, joint) :
                    new Dictionary<Line, List<Line>>();
            }
        }

        private Dictionary<Line, List<Line>> RemoveSharp(Line first, Line second, Point3d joint, bool forElbow = false)
        {
            // 调整first
            var results = new Dictionary<Line, List<Line>>();
            if (ThGeometryTool.IsCollinearEx(first.StartPoint, first.EndPoint,
                second.StartPoint, second.EndPoint))
            {
                return results;
            }
            var firstVec = GetPointedVector(first.StartPoint, first.EndPoint, joint);
            var secondVec = GetPointedVector(second.StartPoint, second.EndPoint, joint);
            bool isFirstSp = IsStart(first, joint);
            bool isSeconSp = IsStart(second, joint);
            var rad = firstVec.GetAngleTo(secondVec);
            var l = CalculateL(Math.PI - rad);
            var pt1 = joint + firstVec.MultiplyBy(l);
            var projectionPt = pt1.GetProjectPtOnLine(second.StartPoint, second.EndPoint);
            if (projectionPt.IsPointOnLine(second, 1.0))
            {
                if (first.Length > l)
                {
                    var firstLines = new List<Line>();
                    firstLines.Add(new Line(projectionPt, pt1));
                    firstLines.Add(new Line(pt1, isFirstSp ? first.EndPoint : first.StartPoint));
                    results.Add(first, firstLines);
                    if (forElbow)
                    {
                        var secondLines = new List<Line>();
                        secondLines.Add(new Line(isSeconSp ? second.EndPoint : second.StartPoint, projectionPt));
                        results.Add(second, secondLines);
                    }
                }
                else
                {
                    var firstLines = new List<Line>();
                    firstLines.Add(new Line(projectionPt, isFirstSp ? first.EndPoint : first.StartPoint));
                    if (forElbow)
                    {
                        var secondLines = new List<Line>();
                        secondLines.Add(new Line(isSeconSp ? second.EndPoint : second.StartPoint, projectionPt));
                        results.Add(second, secondLines);
                    }
                }
            }
            return results;
        }

        private Vector3d GetMainVector(DBObjectCollection curves)
        {
            var dictionary = new Dictionary<Vector3d, double>();
            foreach (Line curve in curves)
            {
                var vector = curve.LineDirection();
                var pair = Tuple.Create(vector, curve.Length);
                var contains = false;
                foreach (var item in dictionary)
                {
                    var product = Math.Abs(item.Key.DotProduct(vector));
                    if (product < Math.Sin(1 / 180.0 * Math.PI) || product > Math.Cos(1 / 180.0 * Math.PI))
                    {
                        pair = Tuple.Create(item.Key, item.Value + curve.Length);
                        contains = true;
                        break;
                    }
                }
                if (!contains)
                {
                    dictionary.Add(pair.Item1, pair.Item2);
                }
                else
                {
                    dictionary.Remove(pair.Item1);
                    dictionary.Add(pair.Item1, pair.Item2);
                }
            }

            var orderList = dictionary.OrderByDescending(pair => pair.Value);
            return orderList.First().Key;
        }

        private bool IsDx(Line line)
        {
            return Dxs.Contains(line);
        }

        private bool IsFdx(Line line)
        {
            return Fdxs.Contains(line);
        }

        private bool IsSingleRowLine(Line line)
        {
            return SingleRowLines.Contains(line);
        }

        private bool IsStart(Line line, Point3d port)
        {
            return port.DistanceTo(line.StartPoint) < port.DistanceTo(line.EndPoint);
        }

        private Vector3d GetPointedVector(Point3d sp, Point3d ep, Point3d joint)
        {
            return sp.DistanceTo(joint) < sp.DistanceTo(ep) ?
                sp.GetVectorTo(ep).GetNormal() :
                ep.GetVectorTo(sp).GetNormal();
        }

        private List<Tuple<int, int, double>> GetEachAngles(List<Tuple<Line, Vector3d>> infos)
        {
            // 索引，索引，弧度
            var results = new List<Tuple<int, int, double>>();
            for (int i = 0; i < infos.Count - 1; i++)
            {
                for (int j = i + 1; j < infos.Count; j++)
                {
                    var jiajiao = infos[i].Item2.GetAngleTo(infos[j].Item2);
                    results.Add(Tuple.Create(i, j, jiajiao));
                }
            }
            return results;
        }

        private bool IsSharp(Vector3d first, Vector3d second)
        {
            return IsSharp(first.GetAngleTo(second));
        }

        private bool IsSharp(double rad)
        {
            // rad 是弧度
            return rad.RadToAng() < ShapeAngleUpperValue - 1.0;
        }

        private DBObjectCollection Query(Point3d pt, ThCADCoreNTSSpatialIndex spatialIndex, double pointRange)
        {
            var outline = pt.CreateSquare(pointRange);
            var objs = spatialIndex.SelectCrossingPolygon(outline);
            outline.Dispose();
            return objs;
        }

        private DBObjectCollection FilerSmallLines(DBObjectCollection lines, double limitedLength)
        {
            return lines
                .OfType<Line>()
                .Where(l => l.Length >= limitedLength)
                .ToCollection();
        }

        private double CalculateL(double rad)
        {
            //L=0.5D[1+cot(β/2)]+700（式中D为线槽间距，单排布置时D=0）
            var v1 = Math.Cos(rad / 2.0) / Math.Sin(rad / 2.0);
            return 0.5 * D * (1 + v1) + 700;
        }

        private ThCADCoreNTSSpatialIndex UpdateSpatialIndex(
            Dictionary<Line, List<Line>> lineDict,
            ThCADCoreNTSSpatialIndex spatialIndex)
        {
            if (lineDict.Count > 0)
            {
                lineDict.ForEach(o =>
                {
                    spatialIndex.Update(o.Value.ToCollection(), new DBObjectCollection { o.Key });
                });
            }
            return spatialIndex;
        }

        private void UpdateData(Dictionary<Line, List<Line>> lineDict)
        {
            lineDict.ForEach(o =>
            {
                if (IsDx(o.Key))
                {
                    Dxs.Remove(o.Key);
                    o.Value.ForEach(v => Dxs.Add(v));
                }
                else if (IsFdx(o.Key))
                {
                    Fdxs.Remove(o.Key);
                    o.Value.ForEach(v => Fdxs.Add(v));
                }
                else if (IsSingleRowLine(o.Key))
                {
                    SingleRowLines.Remove(o.Key);
                    o.Value.ForEach(v => SingleRowLines.Add(v));
                }
            });
        }
    }
}
