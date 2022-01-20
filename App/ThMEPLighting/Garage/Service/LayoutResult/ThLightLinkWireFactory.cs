using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

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
            var lights = new DBObjectCollection();
            LightEdges.ForEach(e =>
            {
                var dir = e.Edge.LineDirection();
                e.LightNodes.ForEach(n => lights.Add(CalculateBreakLine(n, dir,LampLength)));
            });
            Results = ThLinkWireBreakService.Break(LightEdges.Select(o => o.Edge).ToCollection(), lights);
        }

        private Line CalculateBreakLine(ThLightNode lightNode,Vector3d dir,double lampLength)
        {
            var pt1 = lightNode.Position - dir.MultiplyBy(lampLength / 2.0);
            var pt2 = lightNode.Position + dir.MultiplyBy(lampLength / 2.0);
            return new Line(pt1, pt2);
        }
       
    }
    internal class ThLinkWireBreakService
    {
        public static DBObjectCollection Break(DBObjectCollection wires, DBObjectCollection lights)
        {
            var results = new DBObjectCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lights);
            wires.OfType<Line>().ForEach(e =>
            {
                var lines = Query(spatialIndex, e);
                lines = lines.Where(l =>
                ThGeometryTool.IsCollinearEx(e.StartPoint, e.EndPoint, l.StartPoint, l.EndPoint)).ToList();
                var res = e.Difference(lines);
                res.ForEach(l => results.Add(l));
            });
            return results;
        }
        private static List<Line> Query(ThCADCoreNTSSpatialIndex spatialIndex, Line line)
        {
            var poly = ThDrawTool.ToRectangle(line.StartPoint, line.EndPoint, 2.0);
            var objs = spatialIndex.SelectCrossingPolygon(poly);
            return objs.OfType<Line>().ToList();
        }
    }
}
