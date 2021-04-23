using System.Collections.Generic;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThFindCenterLinePortsService
    {
        private List<Point3d> Ports { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private List<Line> CenterLines { get; set; }

        private ThFindCenterLinePortsService(
            List<Line> centerLines)
        {
            CenterLines = centerLines;
            Ports = new List<Point3d>();
            SpatialIndex = ThGarageLightUtils.BuildSpatialIndex(CenterLines);
        }
        public static List<Point3d> Find(
            List<Line> centerLines)
        {
            var instance = new ThFindCenterLinePortsService(centerLines);
            instance.Find();
            return instance.Ports;
        }
        private void Find()
        {
            CenterLines.ForEach(o =>
            {
                Find(o, o.StartPoint);
                Find(o, o.EndPoint);
            });
        }
        
        private void Find(Line line,Point3d port)
        {
            var spEnvelope = ThDrawTool.CreateSquare(port, 5.0);
            var objs = SpatialIndex.SelectCrossingPolygon(spEnvelope);
            objs.Remove(line);
            if (objs.Count == 0)
            {
                Ports.Add(port);
            }
        }
    }
}
