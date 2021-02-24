using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThRoofFloorRoofRainPipeService
    {
        private List<ThWRoofRainPipe> Pipes { get; set; }
        private ThIfcSpace Space { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThRoofFloorRoofRainPipeService(
           ThIfcSpace space,
           List<ThWRoofRainPipe> pipes)
        {
            Pipes = pipes;
            Space = space;
            var objs = new DBObjectCollection();
            Pipes.ForEach(o => objs.Add(o.Outline));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        public static List<ThWRoofRainPipe> Find(
            ThIfcSpace space,
            List<ThWRoofRainPipe> pipes)
        {
            var service = new ThRoofFloorRoofRainPipeService(space, pipes);
            return service.Find();
        }
        private List<ThWRoofRainPipe> Find()
        {
            var roofFloorBoundary = Space.Boundary.Clone() as Polyline;
            roofFloorBoundary.Closed=true;
            var crossObjs = SpatialIndex.SelectCrossingPolygon(roofFloorBoundary);
            return Pipes.Where(o => crossObjs.Contains(o.Outline)).ToList();
        }
    }
}
