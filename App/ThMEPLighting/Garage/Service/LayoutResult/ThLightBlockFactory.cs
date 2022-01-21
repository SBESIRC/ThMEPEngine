using System;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public class ThLightBlockFactory : LightWireFactory
    {
        private List<ThLightEdge> LightEdges { get; set; }
        public Dictionary<Point3d, Tuple<double,string>> Results { get; set; }
        public ThLightBlockFactory(List<ThLightEdge> lightEdges)
        {
            LightEdges = lightEdges;
            Results = new Dictionary<Point3d, Tuple<double, string>>();
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
                        Results.Add(n.Position, Tuple.Create(normalLine.Angle, n.Number));
                    }
                });
            });
        }
    }
}
