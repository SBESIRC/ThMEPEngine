using System;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThBuildCableTrayService
    {
        private DBObjectCollection BufferObjs { get; set; }
        private DBObjectCollection LineObjs { get; set; }
        /// <summary>
        /// 偏移距离
        /// </summary>
        private double Distance { get; set; }
        private ThBuildCableTrayService(DBObjectCollection objs,double distance)
        {
            LineObjs = ToLines(objs);
            Distance = distance;
            BufferObjs = new DBObjectCollection();
        }
        public static DBObjectCollection Build(DBObjectCollection objs, double distance)
        {
            var instance = new ThBuildCableTrayService(objs, distance);
            instance.Build();
            return instance.BufferObjs;
        }
        private void Build()
        {            
            var bufferObjs = LineObjs.Buffer(Distance);
            var bufferlines = Preprocess(bufferObjs);
            var sideLines = FilterSideLines(bufferlines);
            sideLines.ForEach(m =>
            {
                m.Item2.ForEach(n =>
                {
                    var alignService = ThAlignLineHeadService.Align(bufferlines, m.Item1, n, Distance);
                    alignService.OldEdgeLines.ForEach(o => bufferlines.Remove(o));
                    alignService.OldSideLines.ForEach(o => bufferlines.Remove(o));
                    alignService.NewEdgeLines.ForEach(o => bufferlines.Add(o));
                    alignService.NewSideLines.ForEach(o => bufferlines.Add(o));
                });
            });
            bufferlines.ForEach(o => BufferObjs.Add(o));
        }
        private List<Tuple<Line,List<Line>>> FilterSideLines(List<Line> bufferLines)
        {
            var sideLines = new List<Tuple<Line, List<Line>>>();
            var dbObjs = new DBObjectCollection();
            bufferLines.ForEach(o => dbObjs.Add(o));
            var spaticalIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            LineObjs.Cast<Line>().ForEach(o =>
            {
                List<Line> portSides = new List<Line>();
                var vec = o.LineDirection();
                var sp = o.StartPoint - vec.MultiplyBy(Distance);
                var ep = o.EndPoint + vec.MultiplyBy(Distance);
                var spOutline = ThDrawTool.CreateSquare(sp, 1.0);
                var epOutline = ThDrawTool.CreateSquare(ep, 1.0);
                var spObjs = spaticalIndex.SelectCrossingPolygon(spOutline);
                var epObjs = spaticalIndex.SelectCrossingPolygon(epOutline);
                portSides.AddRange(spObjs.Cast<Line>()
                .Where(m => ThGeometryTool.IsPerpendicular(vec, m.LineDirection()))
                .Where(m => Math.Abs(m.Length - 2 * Distance) <= 1.0)
                .ToList());
                portSides.AddRange(epObjs.Cast<Line>()
                .Where(m => ThGeometryTool.IsPerpendicular(vec, m.LineDirection()))
                .Where(m => Math.Abs(m.Length - 2 * Distance) <= 1.0)
                .ToList());
                if(portSides.Count>0)
                {
                    sideLines.Add(Tuple.Create(o, portSides));
                }
            });
            return sideLines;
        }        
        private DBObjectCollection ToLines(DBObjectCollection objs)
        {
            DBObjectCollection results = new DBObjectCollection();
            var lines=ThMEPLineExtension.ExplodeCurves(objs);
            lines.ForEach(o => results.Add(o));
            return results;
        }
        private List<Line> Preprocess(DBObjectCollection bufferObjs)
        {
            return ThMEPLineExtension.LineSimplifier(bufferObjs, 1.0, 1.0,1.0,Math.PI / 180.0);
        }
        private List<Line> FilterLines(DBObjectCollection objs)
        {
            List<Line> lines = new List<Line>();
            objs.Cast<Curve>().ForEach(o =>
            {
                if(o is Line line1)
                {
                    lines.Add(line1);
                }
                else if(o is Polyline polyline)
                {
                    var subObjs = new DBObjectCollection();
                    polyline.Explode(subObjs);
                    subObjs.Cast<Curve>().ForEach(m =>
                    {
                        if(m is Line line2)
                        {
                            lines.Add(line2);
                        }
                    });
                }
            });
            return lines;
        }
    }
}
