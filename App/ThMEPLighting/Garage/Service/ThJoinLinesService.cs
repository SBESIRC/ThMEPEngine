using System;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    /// <summary>
    /// 对首尾相连，且共线的线进行连接
    /// </summary>
    public class ThJoinLinesService
    {
        private List<Line> Lines { get; set; }
        private List<List<Line>> Results { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThJoinLinesService(List<Line> lines)
        {
            Lines = new List<Line>();
            lines.ForEach(o => Lines.Add(o.WashClone() as Line));
            Results = new List<List<Line>>();
            SpatialIndex=ThGarageLightUtils.BuildSpatialIndex(Lines);
        }        

        public static List<Tuple<Point3d,Point3d>> Join(List<Line> lines)
        {
            var results = new List<Tuple<Point3d, Point3d>>();
            var instance = new ThJoinLinesService(lines);
            instance.Join();
            instance.Results.ForEach(o =>
            {
                var res = instance.GetLightLinePorts(o);
                if(res.Item1.DistanceTo(res.Item2)>0)
                {
                    results.Add(res);
                }
            });
            return results;
        }
        public static List<Line> GetLines(List<Tuple<Point3d, Point3d>> pts)
        {
            var results = new List<Line>();
            pts.ForEach(o => results.Add(new Line(o.Item1, o.Item2)));
            return results;
        }
        private Tuple<Point3d, Point3d> GetLightLinePorts(List<Line> lines)
        {
            if(lines.Count==1)
            {
                return Tuple.Create(lines[0].StartPoint, lines[0].EndPoint);
            }
            else if(lines.Count > 1)
            {
                var pairPts = new List<Tuple<Point3d, Point3d>>();
                pairPts.Add(Tuple.Create(lines[0].StartPoint, lines[lines.Count - 1].StartPoint));
                pairPts.Add(Tuple.Create(lines[0].StartPoint, lines[lines.Count - 1].EndPoint));
                pairPts.Add(Tuple.Create(lines[0].EndPoint, lines[lines.Count - 1].StartPoint));
                pairPts.Add(Tuple.Create(lines[0].EndPoint, lines[lines.Count - 1].EndPoint));
                return pairPts.OrderByDescending(o => o.Item1.DistanceTo(o.Item2)).First();
            }
            else
            {
                return Tuple.Create(Point3d.Origin, Point3d.Origin);
            }
        }
        private void Join()
        {
            while (Lines.Count > 0)
            {
                var first = Lines[0];
                Lines.RemoveAt(0);
                var links = new List<Line>();
                links.Add(first);
                Find(links, first.StartPoint, true);
                Find(links, first.EndPoint);
                Results.Add(links);
                Lines = Lines.Where(o => !links.Contains(o)).ToList();
            }
        }
        private void Find(List<Line> links, Point3d portPt, bool isPreFind = false)
        {
            var portLines = SearchLines(portPt, ThGarageLightCommon.RepeatedPointDistance);
            portLines = portLines.Where(o => !links.Contains(o)).ToList();
            if (portLines.Count == 0)
            {
                return;
            }
            var nextLine = FilterLines(links[0], portLines);
            if (nextLine.Length == 0)
            {
                return;
            }
            if (isPreFind)
            {
                links.Insert(0, nextLine);
            }
            else
            {
                links.Add(nextLine);
            }
            if (nextLine.StartPoint.DistanceTo(portPt) >
                nextLine.EndPoint.DistanceTo(portPt))
            {
                Find(links, nextLine.StartPoint, isPreFind);
            }
            else
            {
                Find(links, nextLine.EndPoint, isPreFind);
            }
        }        
        private Line FilterLines(Line line, List<Line> linkLines)
        {
            var collinearLines = linkLines.Where(o => ThGeometryTool.IsCollinearEx(
                  line.StartPoint, line.EndPoint, o.StartPoint, o.EndPoint)).ToList();
            if (collinearLines.Count > 0)
            {
                return collinearLines.OrderByDescending(o => o.Length).First();
            }
            return new Line();
        }
        private List<Line> SearchLines(Point3d portPt, double length)
        {
            Polyline envelope = ThDrawTool.CreateSquare(portPt, length);
            var searchObjs = SpatialIndex.SelectCrossingPolygon(envelope);
            return searchObjs
                .Cast<Line>()
                .Where(o => IsLink(o, portPt))
                .ToList();
        }
        private bool IsLink(Line line, Point3d portPt)
        {
            return line.StartPoint.DistanceTo(portPt) <= ThGarageLightCommon.RepeatedPointDistance ||
                line.EndPoint.DistanceTo(portPt) <= ThGarageLightCommon.RepeatedPointDistance;
        }
    }    
}
