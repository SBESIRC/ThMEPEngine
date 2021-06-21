using AcHelper;
using NFox.Cad;
using System.Linq;
using ThCADExtension;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.CAD
{
    public class ThWindowInteraction
    {
        public static Point3dCollection GetPoints(PointCollector.Shape shape,List<string> prompts)
        {
            using (var pc = new PointCollector(shape, prompts))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return new Point3dCollection();
                }
                return pc.CollectedPoints
                    .Cast<Point3d>()
                    .Select(p => p.TransformBy(Active.Editor.UCS2WCS()))
                    .ToCollection();
            }
        }
        public static Polyline GetPolyline(PointCollector.Shape shape, List<string> prompts)
        {
            using (var pc = new PointCollector(shape, prompts))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return new Polyline();
                }
                if(pc.CollectedPoints.Count<2)
                {
                    return new Polyline();
                }
                var poly = ThDrawTool.CreatePolyline(pc.CollectedPoints, true);
                poly.TransformBy(Active.Editor.UCS2WCS());
                return poly;
            }
        }
    }
}
