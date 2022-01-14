using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using System.Collections.Generic;
using ThCADExtension;

namespace TianHua.Hvac.UI.Command
{
    class ThHvacIndoorFanService
    {
        public Polyline SelectWindowRect()
        {
            Polyline poly = null;
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return poly;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                poly = new Polyline();
                poly.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                poly.TransformBy(Active.Editor.UCS2WCS());
                return poly;
            }
        }
    }
}
