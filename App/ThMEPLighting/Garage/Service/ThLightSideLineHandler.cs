using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    internal class ThLightSideLineHandler
    {
        private double ShortLineLength = 100.0;
        public ThLightSideLineHandler(double shortLinkLineLength)
        {
            ShortLineLength = shortLinkLineLength;
            if(ShortLineLength<1e-6)
            {
                ShortLineLength = 100.0;
            }
        }
        public DBObjectCollection Handle(DBObjectCollection lines)
        {
            var shortLines = lines.OfType<Line>().Where(l => l.Length <= ShortLineLength).ToList();
            for(int i =0;i< shortLines.Count;i++)
            {
                var pair = Find(lines, shortLines[i]);
                if (pair ==null)
                {
                    continue;
                }
                var newPair = Handle(pair);
                if(newPair!=null)
                {
                    lines.Remove(pair.Item1);
                    lines.Remove(pair.Item2);
                    lines.Remove(pair.Item3);

                    lines.Add(newPair.Item2);
                    lines.Add(newPair.Item3);
                }
            }
            return lines;
        }

        private Tuple<Line, Line, Line> Handle(Tuple<Line, Line, Line> pair)
        {
            var shortLine = pair.Item1;
            var firstLink = pair.Item2;
            var secondLink = pair.Item3;
            var inters = firstLink.IntersectWithEx(secondLink,Intersect.ExtendBoth);
            if(inters.Count>0)
            {
                var intersPt = inters[0];
                var firstLinkPtRes = ThGarageUtils.FindLinkPt(shortLine, firstLink);
                var secondLinkPtRes = ThGarageUtils.FindLinkPt(shortLine, secondLink);
                if(firstLinkPtRes.HasValue && secondLinkPtRes.HasValue)
                {
                   bool isCollinear = ThGeometryTool.IsCollinearEx(intersPt, firstLinkPtRes.Value, secondLinkPtRes.Value);
                   if(isCollinear)
                    {
                       var res = Extend(firstLink, secondLink);
                        if(res!=null)
                        {
                            return Tuple.Create(shortLine, res.Item1,res.Item2);
                        }
                    }
                   else
                    {
                        var pts = new Point3dCollection() { intersPt, firstLinkPtRes.Value, secondLinkPtRes.Value };
                        var triangle = pts.CreatePolyline();
                        if(IsSmallerArea(triangle.Area))
                        {
                            var res = Extend(firstLink, secondLink);
                            if (res != null)
                            {
                                return Tuple.Create(shortLine, res.Item1, res.Item2);
                            }
                        }
                    }
                }
            }
            return null;
        }

        private Tuple<Line, Line> Extend(Line first, Line second)
        {
            var inters = first.IntersectWithEx(second,Intersect.ExtendBoth);
            if (inters.Count == 1)
            {
                var firstFarwayPt = ThGarageLightUtils.GetNextLinkPt(inters[0], first.StartPoint, first.EndPoint);
                var secondFarwayPt = ThGarageLightUtils.GetNextLinkPt(inters[0], second.StartPoint, second.EndPoint);
                var firstExtend = new Line(inters[0], firstFarwayPt);
                var secondExtend = new Line(inters[0], secondFarwayPt);
                return Tuple.Create(firstExtend, secondExtend);
            }
            return null;
        }

        private bool IsSmallerArea(double area)
        {
            return area <= ShortLineLength * ShortLineLength;
        }

        private Tuple<Line, Line, Line> Find(DBObjectCollection objs, Line line)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var startObjs = Query(spatialIndex,line.StartPoint, ThGarageLightCommon.RepeatedPointDistance);
            startObjs.Remove(line);

            var endObjs = Query(spatialIndex, line.EndPoint, ThGarageLightCommon.RepeatedPointDistance);
            endObjs.Remove(line);

            if(startObjs.Count==1 && endObjs.Count==1)
            {
                var startLink = startObjs[0] as Line;
                var endLink = endObjs[0] as Line;
                if(startLink.Length> ShortLineLength && endLink.Length> ShortLineLength)
                {
                    return Tuple.Create(line, startLink, endLink);
                }
            }
            return null;
        }

        private DBObjectCollection Query(ThCADCoreNTSSpatialIndex spatialIndex, Point3d port,double width)
        {
            var envelop = CreateEnvelop(port, width);
            return spatialIndex.SelectCrossingPolygon(envelop);
        }

        private Polyline CreateEnvelop(Point3d port, double width)
        {
            return port.CreateSquare(width);
        }
    }
}
