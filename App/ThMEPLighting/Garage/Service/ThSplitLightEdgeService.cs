using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThSplitLightEdgeService
    {
        private List<ThLightEdge> Results { get; set; }
        private List<ThLightEdge> LightEdges { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private List<Line> Lines { get; set; }
        private ThSplitLightEdgeService(List<ThLightEdge> lightEdges,List<Line> fdxLines)
        {
            Lines = new List<Line>();
            LightEdges = lightEdges;
            Lines.AddRange(fdxLines);
            Lines.AddRange(LightEdges.Select(o => o.Edge).ToList());
            Results = new List<ThLightEdge>();
        }
        public static List<ThLightEdge> Split(List<ThLightEdge> lightEdges, List<Line> fdxLines)
        {
            var instance = new ThSplitLightEdgeService(lightEdges, fdxLines);
            instance.Split();
            return instance.Results;
        }
        private void Split()
        {
            LightEdges.ForEach(o => Split(o));
        }
        private void Split(ThLightEdge lightEdge)
        {
            var splitLines=ThSplitLineService.Split(Lines, lightEdge.Edge);
            if (splitLines.Count==0)
            {
                Results.Add(lightEdge);
            }
            else
            {
                var splitEdges = SplitLightEdge(lightEdge, splitLines);
                if(splitEdges.Count==0)
                {
                    Results.Add(lightEdge);
                }
                else
                {
                    Results.AddRange(splitEdges);
                }
            }
        }
        private List<ThLightEdge> SplitLightEdge(ThLightEdge lightEdge,List<Line> lines)
        {
            var results = new List<ThLightEdge>();
            var lightNodes = lightEdge.LightNodes;            
            lines.ForEach(m =>
            {
                var vec = m.StartPoint.GetVectorTo(m.EndPoint);
                var filterNodes = lightNodes.Where(n =>
                 {
                     var dis = vec.ProjectionDis(m.StartPoint.GetVectorTo(n.Position));
                     return dis <= m.Length;
                 }).ToList();
                filterNodes.ForEach(n => lightNodes.Remove(n));
                var newEdge = new ThLightEdge
                {
                    Edge = m,
                    LightNodes = filterNodes
                };
                results.Add(newEdge);
            });
            return results;
        }
    }
}
