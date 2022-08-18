using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    internal class ThCutCableTrayUnlinkWireService
    {
        private Dictionary<Line, Point3dCollection> WireDict { get; set; }
        private ThCADCoreNTSSpatialIndex WireSpatialIndex { get; set; }
        private double PointTolerance = ThGarageLightCommon.RepeatedPointDistance * 10;
        private double LampLength { get; set; }
        private ThCADCoreNTSSpatialIndex FdxSpatialIndex { get; set; }
        public ThCutCableTrayUnlinkWireService(Dictionary<Line, Point3dCollection> wireDict,
            double lampLength, List<Line> fdxLines)
        {
            WireDict = wireDict;
            LampLength = lampLength;
            FdxSpatialIndex = new ThCADCoreNTSSpatialIndex(fdxLines.ToCollection());
            InitWireSpatialIndex();
        }
        public DBObjectCollection Cut()
        {
            // 过滤末端没有布置灯点的灯线，且有一端没连接其它物体
            var objs = Filter();

            // update
            objs.OfType<Line>().ForEach(l => WireDict.Remove(l));
            InitWireSpatialIndex();

            // 将布灯点的线，有一端未连接灯线的切断
            var pairs = Find();

            // 切断
            var results = WireDict.Keys.ToCollection();
            pairs.ForEach(p =>
            {
                var newLine = Cut(p.Item1, p.Item2, p.Item3, LampLength);
                results.Remove(p.Item1);
                results.Add(newLine);
            });

            return results;
        }

        private Line Cut(Line line, bool isStartLink, bool isEndLink, double lampLength)
        {
            var pts = WireDict[line];
            if (pts.Count == 0)
            {
                return CutWireWithoutLight(line, isStartLink, isEndLink);
            }
            else
            {
                return CutWireWithLights(line, isStartLink, isEndLink, lampLength);
            }
        }
        private Line CutWireWithoutLight(Line line, bool isStartLink, bool isEndLink)
        {
            var newSp = line.StartPoint;
            var newEp = line.EndPoint;
            var dir = newSp.GetVectorTo(newEp).GetNormal();
            var fdxs = QueryFdxs(line);
            var fdxIntersectPts = fdxs.OfType<Line>().SelectMany(o => GetIntersectPts(o, line).OfType<Point3d>()).ToCollection();
            if (fdxIntersectPts.Count == 0)
            {
                return new Line(newSp, newEp);
            }
            if (!isStartLink)
            {
                // 找出距离StartPoint最近的点
                var closePt = fdxIntersectPts.OfType<Point3d>().OrderBy(p => line.StartPoint.DistanceTo(p)).First();
                if (closePt.IsPointOnLine(line))
                {
                    newSp = closePt;
                }
            }
            if (!isEndLink)
            {
                // 找出距离EndPoint最近的点
                var closePt = fdxIntersectPts.OfType<Point3d>().OrderBy(p => line.EndPoint.DistanceTo(p)).First();
                if (closePt.IsPointOnLine(line))
                {
                    newEp = closePt;
                }
            }
            return new Line(newSp, newEp);
        }
        private Line CutWireWithLights(Line line, bool isStartLink, bool isEndLink, double lampLength)
        {
            var pts = WireDict[line];
            var newSp = line.StartPoint;
            var newEp = line.EndPoint;
            var dir = newSp.GetVectorTo(newEp).GetNormal();
            var fdxs = QueryFdxs(line);
            var fdxIntersectPts = fdxs.OfType<Line>().SelectMany(o => GetIntersectPts(o, line).OfType<Point3d>()).ToCollection();
            if (!isStartLink)
            {
                // 找出距离StartPoint最近的点
                var closePt = pts.OfType<Point3d>().OrderBy(p => line.StartPoint.DistanceTo(p)).First();
                var portPt = closePt + dir.Negate().MultiplyBy(lampLength / 2.0);
                // 检查newSp与portPt有没有非灯线点
                if (fdxIntersectPts.Count > 0)
                {
                    var fdxCloseSp = fdxIntersectPts.OfType<Point3d>().OrderBy(p => line.StartPoint.DistanceTo(p)).First();
                    if (ThGeometryTool.IsPointOnLine(newSp, portPt, fdxCloseSp, PointTolerance))
                    {
                        newSp = fdxCloseSp;
                    }
                    else
                    {
                        newSp = portPt;
                    }
                }
                else
                {
                    newSp = portPt;
                }
            }
            if (!isEndLink)
            {
                // 找出距离EndPoint最近的点
                var closePt = pts.OfType<Point3d>().OrderBy(p => line.EndPoint.DistanceTo(p)).First();
                var portPt = closePt + dir.MultiplyBy(lampLength / 2.0);
                // 检查newEp与portPt有没有非灯线点
                if (fdxIntersectPts.Count > 0)
                {
                    var fdxCloseEp = fdxIntersectPts.OfType<Point3d>().OrderBy(p => line.EndPoint.DistanceTo(p)).First();
                    if (ThGeometryTool.IsPointOnLine(newEp, portPt, fdxCloseEp, PointTolerance))
                    {
                        newEp = fdxCloseEp;
                    }
                    else
                    {
                        newEp = portPt;
                    }
                }
                else
                {
                    newEp = portPt;
                }
            }
            return new Line(newSp, newEp);
        }
        private Point3dCollection GetIntersectPts(Line first, Line second)
        {
            return first.IntersectWithEx(second, Intersect.ExtendBoth);
        }
        private List<Tuple<Line, bool, bool>> Find()
        {
            var results = new List<Tuple<Line, bool, bool>>();
            WireDict.Where(o => o.Value.Count > 0).ForEach(o =>
                {
                    bool isStartLink = IsPortLinkObjs(o.Key, true);
                    bool isEndLink = IsPortLinkObjs(o.Key, false);
                    if (!isStartLink || !isEndLink)
                    {
                        results.Add(Tuple.Create(o.Key, isStartLink, isEndLink));
                    }
                });
            return results;
        }

        private void InitWireSpatialIndex()
        {
            WireSpatialIndex = new ThCADCoreNTSSpatialIndex(WireDict.Keys.ToCollection());
        }

        private DBObjectCollection Filter()
        {
            // 过滤末端没有灯点的线
            var results = new DBObjectCollection();
            WireDict.Where(o => o.Value.Count == 0).ForEach(o =>
                {
                    if (!results.Contains(o.Key))
                    {
                        if (!IsPortLinkObjs(o.Key, true))
                        {
                            var links = new List<Line> { o.Key };
                            Traverse(o.Key.EndPoint, links);
                            links.ForEach(l => results.Add(l));
                        }
                        else if (!IsPortLinkObjs(o.Key, false))
                        {
                            var links = new List<Line> { o.Key };
                            Traverse(o.Key.StartPoint, links);
                            links.ForEach(l => results.Add(l));
                        }
                    }
                });
            return results;
        }

        private void Traverse(Point3d portPt, List<Line> links)
        {
            if (IsBodyLinkFdxs(links.Last()))
            {
                // 灯线上没有灯点，但是自身与非灯线相交，停止遍历下去
                return;
            }
            var wires = QueryWires(portPt);
            links.ForEach(l => wires.Remove(l));
            if (wires.Count == 1)
            {
                var current = wires[0] as Line;
                if (WireDict[current].Count > 0)
                {
                    // 当前线是布点的线，则返回
                    return;
                }
                else
                {
                    links.Add(current);
                    var nextPt = portPt.DistanceTo(current.StartPoint) <
                        portPt.DistanceTo(current.EndPoint) ?
                        current.EndPoint : current.StartPoint;
                    Traverse(nextPt, links);
                }
            }
            else
            {
                return;
            }
        }

        private bool IsPortLinkObjs(Line line, bool isSp)
        {
            // 看起点是否连接物体
            var port = isSp ? line.StartPoint : line.EndPoint;
            var linkWires = QueryWires(port);
            linkWires.Remove(line);
            var fdxWires = QueryFdxs(port);
            return linkWires.Count > 0 || fdxWires.Count > 0;
        }

        private bool IsBodyLinkFdxs(Line line)
        {
            return QueryFdxs(line).Count > 0;
        }

        private DBObjectCollection QueryWires(Point3d port)
        {
            var outline = port.CreateSquare(PointTolerance);
            var results = WireSpatialIndex.SelectCrossingPolygon(outline);
            outline.Dispose();
            return results;
        }
        private DBObjectCollection QueryFdxs(Point3d port)
        {
            var outline = port.CreateSquare(PointTolerance);
            var results = QueryFdxs(outline);
            outline.Dispose();
            return results;
        }
        private DBObjectCollection QueryFdxs(Polyline outline)
        {
            return FdxSpatialIndex.SelectCrossingPolygon(outline);
        }
        private DBObjectCollection QueryFdxs(Line line)
        {
            var outline = ThDrawTool.ToRectangle(line.StartPoint,
                line.EndPoint, PointTolerance * 2.0);
            return FdxSpatialIndex.SelectCrossingPolygon(outline);
        }
    }
}
