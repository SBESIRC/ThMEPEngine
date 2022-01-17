using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using System;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public class ThLightBlockFactory : LightWireFactory
    {
        private List<ThLightEdge> LightEdges { get; set; }
        public Dictionary<Point3d, double> Results { get; set; }
        public ThLightBlockFactory(List<ThLightEdge> lightEdges)
        {
            LightEdges = lightEdges;
            Results = new Dictionary<Point3d, double>();
        }
        public override void Build()
        {
            LightEdges.Where(o => o.IsDX).ForEach(m =>
            {
                var normalLine = m.Edge.NormalizeLaneLine();
                m.LightNodes.ForEach(n =>
                {   
                    if (!string.IsNullOrEmpty(n.Number) && !Results.ContainsKey(n.Position))
                    {
                        Results.Add(n.Position, m.Edge.Angle);
                    }
                });
            });
        }
        public List<Tuple<Point3d,double,string>> BuildLightPosInf()
        {
            var results = new List<Tuple<Point3d, double, string>>();
            LightEdges.Where(o => o.IsDX).ForEach(m =>
            {
                var normalLine = m.Edge.NormalizeLaneLine();
                m.LightNodes.ForEach(n =>
                {
                    if (!string.IsNullOrEmpty(n.Number) && !results.Select(o=>o.Item1).ToList().IsContains(n.Position))
                    {
                        results.Add(Tuple.Create(n.Position, m.Edge.Angle,n.Number));
                    }
                });
            });
            return results;
        }
    }
}
