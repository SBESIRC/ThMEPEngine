using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public class ThLightLinkWireFactory : LightWireFactory
    {
        public List<ThLightEdge> LightEdges { get; set; }
        public DBObjectCollection Results { get; private set; }
        public ThLightLinkWireFactory(List<ThLightEdge> lightEdges)
        {
            LightEdges = lightEdges;
            Results = new DBObjectCollection();
        }
        public override void Build()
        {
            var breakLines = new List<Line>();
            LightEdges.ForEach(e =>
            {
                var dir = e.Edge.LineDirection();
                e.LightNodes.ForEach(n => breakLines.Add(CalculateBreakLine(n, dir)));
            });

            var spatialIndex = new ThCADCoreNTSSpatialIndex(breakLines.ToCollection());
            Func<Line, List<Line>> Query = (Line l) =>
             {
                 var poly = ThDrawTool.ToRectangle(l.StartPoint, l.EndPoint, 2.0);
                 var objs = spatialIndex.SelectCrossingPolygon(poly);
                 return objs.OfType<Line>().ToList();
             };


            LightEdges.ForEach(e =>
            {
                var lines = Query(e.Edge);
                lines = lines.Where(l => 
                ThGeometryTool.IsCollinearEx(e.Edge.StartPoint, e.Edge.EndPoint, l.StartPoint, l.EndPoint)).ToList();
                var res = e.Edge.Difference(lines);
                res.ForEach(l => Results.Add(l));
            });
        }

        private Line CalculateBreakLine(ThLightNode lightNode,Vector3d dir)
        {
            var sideBreakLength = DefaultNumbers.Contains(lightNode.Number) ? 0.0 : LampSideIntervalLength;
            var pt1 = lightNode.Position - dir.MultiplyBy(LampLength / 2.0 + sideBreakLength);
            var pt2 = lightNode.Position + dir.MultiplyBy(LampLength / 2.0 + sideBreakLength);
            return new Line(pt1, pt2);
        }
    }
}
