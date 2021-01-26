using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.LaneLine;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.Garage.Service
{
    public class ThLineNodeService
    {
        public List<Tuple<Line, List<Line>>> Results { get; set; }
        private List<Line> Lines { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }

        private ThLineNodeService(List<Line> lines)
        {
            Lines = lines;
            Results = new List<Tuple<Line, List<Line>>>();
        }
        public static List<Tuple<Line, List<Line>>> Node(List<Line> lines)
        {
            var instance = new ThLineNodeService(lines);
            instance.Node();
            return instance.Results;
        }
        private void Node()
        {
            var objs = new DBObjectCollection();
            Lines.ForEach(o => objs.Add(o));
            var nodedLines = ThLaneLineNodingEngine.Noding(objs);
            SpatialIndex = new ThCADCoreNTSSpatialIndex(nodedLines);
            Lines.ForEach(o =>
            {
                var subs = FindSubLines(o);
                if (subs.Count > 0)
                {
                    Results.Add(Tuple.Create(o, subs));
                }
                else
                {
                    subs = new List<Line> { new Line(o.StartPoint, o.EndPoint) };
                    Results.Add(Tuple.Create(o, subs));
                }
            });
        }
        private List<Line> FindSubLines(Line origin)
        {
            var vec = origin.StartPoint.GetVectorTo(origin.EndPoint).GetNormal();
            var outline = ThDrawTool.ToRectangle(origin.StartPoint, origin.EndPoint, 1.0);
            var objs = SpatialIndex.SelectCrossingPolygon(outline);
            var sp = origin.StartPoint - vec.MultiplyBy(0.1);
            var ep = origin.EndPoint + vec.MultiplyBy(1.0);
            var newLine = new Line(sp, ep);
            var subLines=objs.Cast<Line>()
                .Where(o => vec.IsParallelToEx(o.StartPoint.GetVectorTo(o.EndPoint).GetNormal()))
                .Where(o => o.StartPoint.IsPointOnLine(newLine) && o.EndPoint.IsPointOnLine(newLine))
                .ToList();
            if(Validate(origin, subLines))
            {
                return subLines;
            }
            else
            {
                return new List<Line>();
            }
        }
        private bool Validate(Line orgin,List<Line> subLines)
        {
            double sum = subLines.Sum(o => o.Length);
            return Math.Abs(orgin.Length - sum) <= 1.0;
        }
    }
}
