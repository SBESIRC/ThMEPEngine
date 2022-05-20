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
        private double PointTolerance = ThGarageLightCommon.RepeatedPointDistance*2.5; 
        private double LampLength { get; set; }
        private ThCADCoreNTSSpatialIndex FdxSpatialIndex { get; set; }
        public ThCutCableTrayUnlinkWireService(Dictionary<Line,Point3dCollection> wireDict,
            double lampLength,List<Line> fdxLines)
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

        private Line Cut(Line line,bool isStartLink,bool isEndLink,double lampLength)
        {
            var pts = WireDict[line];
            var newSp = line.StartPoint;
            var newEp = line.EndPoint;
            var dir = newSp.GetVectorTo(newEp).GetNormal();
            if (!isStartLink)
            {
                // 找出距离StartPoint最近的点
                var closePt = pts.OfType<Point3d>().OrderBy(p => line.StartPoint.DistanceTo(p)).First();
                var portPt = closePt + dir.Negate().MultiplyBy(lampLength/2.0);
                if(portPt.IsPointOnLine(line))
                {
                    newSp = portPt;
                }
            }
            if (!isEndLink)
            {
                // 找出距离EndPoint最近的点
                var closePt = pts.OfType<Point3d>().OrderBy(p => line.EndPoint.DistanceTo(p)).First();
                var portPt = closePt + dir.MultiplyBy(lampLength / 2.0);
                if (portPt.IsPointOnLine(line))
                {
                    newEp = portPt;
                }
            }
            return new Line(newSp, newEp);
        }

       private List<Tuple<Line,bool,bool>> Find()
        {
            var results = new List<Tuple<Line, bool, bool>>();
            WireDict.Where(o => o.Value.Count > 0).ForEach(o =>
                {
                    bool isStartLink = IsLinkObjs(o.Key, true);
                    bool isEndLink = IsLinkObjs(o.Key, false);
                    if (!isStartLink || !isEndLink)
                    {
                        results.Add(Tuple.Create(o.Key,isStartLink,isEndLink));
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
                    if(!results.Contains(o.Key))
                    {
                        if (!IsLinkObjs(o.Key, true))
                        {
                            var links = new List<Line> { o.Key };
                            Traverse(o.Key.EndPoint, links);
                            links.ForEach(l => results.Add(l));
                        }
                        else if (!IsLinkObjs(o.Key, false))
                        {
                            var links = new List<Line> { o.Key };
                            Traverse(o.Key.StartPoint, links);
                            links.ForEach(l => results.Add(l));
                        }
                    }
                });
            return results;
        }

        private void Traverse(Point3d portPt,List<Line> links)
        {
            var wires = QueryWires(portPt);
            links.ForEach(l => wires.Remove(l));
            if(wires.Count == 1)
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

        private bool IsLinkObjs(Line line,bool isSp)
        {
            // 看起点是否连接物体
            var port = isSp ? line.StartPoint : line.EndPoint;
            var linkWires = QueryWires(port);           
            linkWires.Remove(line);
            var fdxWires = QueryFdxs(port);
            return linkWires.Count > 0 || fdxWires.Count>0;
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
            var results = FdxSpatialIndex.SelectCrossingPolygon(outline);
            outline.Dispose();
            return results;
        }
    }
}
