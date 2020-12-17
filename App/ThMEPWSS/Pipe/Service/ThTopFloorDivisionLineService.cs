using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Geom;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThTopFloorDivisionLineService
    {
        private List<Line> DivisionLines { get; set; }
        private ThIfcSpace Space { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThTopFloorDivisionLineService(
           ThIfcSpace space,
           List<Line> divisionLines)
        {
            DivisionLines = divisionLines;
            Space = space;
            var objs = new DBObjectCollection();
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        public static List<Line> Find(
            ThIfcSpace space,
            List<Line> divisionLines)
        {
            var lines = new List<Line>();
            foreach(var line in divisionLines)
            {
                var boundary = space.Boundary as Polyline;
                if (GeomUtils.PtInLoop(boundary, line.StartPoint)&& GeomUtils.PtInLoop(boundary, line.EndPoint))
                {
                    lines.Add(line);
                }
            }            
            return lines;
        }    
    }
}
