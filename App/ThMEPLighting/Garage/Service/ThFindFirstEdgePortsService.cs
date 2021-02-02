using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    public class ThFindFirstEdgePortsService
    {
        public List<Point3d> Ports { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private List<Line> FirstLines { get; set; }
        private List<Point3d> CenterPorts { get; set; }
        private double OffsetDis { get; set; }

        private ThFindFirstEdgePortsService(
            List<Line> firstLines,
            List<Point3d> centerPorts,
            double offsetDis)
        {
            FirstLines = firstLines;
            CenterPorts = centerPorts;
            OffsetDis = offsetDis;
            Ports = new List<Point3d>();
            SpatialIndex = ThGarageLightUtils.BuildSpatialIndex(FirstLines);
        }
        public static ThFindFirstEdgePortsService Find(
            List<Line> firstLines,
            List<Point3d> centerPorts,
            double offsetDis)
        {
            var instance = new ThFindFirstEdgePortsService(firstLines, centerPorts, offsetDis);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            FirstLines.ForEach(o =>
            {
                Find(o, o.StartPoint);
                Find(o, o.EndPoint);
            });
            Sort();
        }
        private void Sort()
        {
            var sortPts = new List<Point3d>();
            CenterPorts.ForEach(o =>
            {
                var result = FindFirstPtByCenterPt(o);
                if(result.HasValue)
                {
                    sortPts.Add(result.Value);
                }
            });
            Ports = sortPts;
        }
        public Point3d? FindFirstPtByCenterPt(Point3d centerPt)
        {
            var results = Ports.Where(o => Math.Abs(centerPt.DistanceTo(o) - OffsetDis) <= 5.0);
            if (results.Count() == 1)
            {
                return results.First();
            }
            else
            {
                return null;
            }
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
