using ThCADCore.NTS;
using ThMEPWSS.Pipe.Geom;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Service
{
    public class ThTopFloorDivisionLineService
    {
        private List<Line> DivisionLines { get; set; }
        private ThIfcRoom Room { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThTopFloorDivisionLineService(
           ThIfcRoom room,
           List<Line> divisionLines)
        {
            DivisionLines = divisionLines;
            Room = room;
            var objs = new DBObjectCollection();
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        public static List<Line> Find(
            ThIfcRoom room,
            List<Line> divisionLines)
        {
            var lines = new List<Line>();
            foreach(var line in divisionLines)
            {
                var boundary = room.Boundary as Polyline;
                if (GeomUtils.PtInLoop(boundary, line.StartPoint)&& GeomUtils.PtInLoop(boundary, line.EndPoint))
                {
                    lines.Add(line);
                }
            }            
            return lines;
        }    
    }
}
