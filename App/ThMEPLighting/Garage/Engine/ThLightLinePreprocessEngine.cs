using Autodesk.AutoCAD.DatabaseServices;
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

        public List<Line> Preprocess(List<Line> lines)
        {
            //Merge 
            var mergeLines = ThLineMerger.Merge(lines);
            var objs = new DBObjectCollection();
            mergeLines.ForEach(o => objs.Add(o));

            //Union
            //var unionLines = ThLaneLineUnionEngine.Union(objs);

            //Join
            var joinLines = ThLaneLineJoinEngine.Join(objs);

            //Snap

            //过滤短线
            return FilterShortLines(joinLines.Cast<Line>().ToList());
        }

        public Tuple<List<Line>, List<Line>> Cut(List<Line> firstLines, List<Line> secondLines)
        {
            var objs = new List<Line>();
            objs.AddRange(firstLines);
            objs.AddRange(secondLines);
            var results = ThLineNodeService.Node(objs);
            var firstSplitLines = new List<Line>();
            foreach (var item in results.Where(o=> firstLines.IsContains(o.Item1)))
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
