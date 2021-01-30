using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Service;

namespace ThMEPLighting.Garage.Engine
{
    public class ThLightLinePreprocessEngine:IDisposable
    {
        public double RemoveLength { get; set; }
        public ThLightLinePreprocessEngine()
        {
            RemoveLength = 5.0;
        }
        public void Dispose()
        {            
        }

        public List<Line> Preprocess(List<Line> curves)
        {
            var lines = curves.ToCollection();
            //lines = ThLaneLineUnionEngine.Union(lines);
            //lines = ThLaneLineExtendEngine.Extend(lines);
            //lines = ThLaneLineMergeExtension.Merge(lines);
            return lines.Cast<Line>().ToList();
        }

        public Tuple<List<Line>, List<Line>> Cut(List<Line> firstLines, List<Line> secondLines)
        {
            var objs = new List<Line>();
            objs.AddRange(firstLines);
            objs.AddRange(secondLines);
            var results = ThLineNodeService.Node(objs);
            var firstSplitLines = new List<Line>();
            foreach (var item in results.Where(o => firstLines.IsContains(o.Item1)))
            {
                firstSplitLines.AddRange(item.Item2);
            }
            var secondSplitLines = new List<Line>();
            foreach (var item in results.Where(o => secondLines.IsContains(o.Item1)))
            {
                secondSplitLines.AddRange(item.Item2);
            }
            return Tuple.Create(FilterShortLines(firstSplitLines),
                FilterShortLines(secondSplitLines));
        }
        private List<Line> FilterShortLines(List<Line> lines)
        {
            return lines.Cast<Line>().Where(o => o.Length > RemoveLength).ToList();
        }
    }
}
