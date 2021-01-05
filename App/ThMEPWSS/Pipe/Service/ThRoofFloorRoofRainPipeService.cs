using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPEngineCore.Model.Plumbing;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Service
{
    public class ThRoofFloorRoofRainPipeService
    {
        private List<ThIfcRoofRainPipe> Pipes { get; set; }
        private ThIfcSpace Space { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThRoofFloorRoofRainPipeService(
           ThIfcSpace space,
           List<ThIfcRoofRainPipe> pipes)
        {
            Pipes = pipes;
            Space = space;
            var objs = new DBObjectCollection();
            Pipes.ForEach(o => objs.Add(o.Outline));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        public static List<ThIfcRoofRainPipe> Find(
            ThIfcSpace space,
            List<ThIfcRoofRainPipe> pipes)
        {
            var service = new ThRoofFloorRoofRainPipeService(space, pipes);
            return service.Find();
        }
        private List<ThIfcRoofRainPipe> Find()
        {
            var roofFloorBoundary = Space.Boundary.Clone() as Polyline;
            roofFloorBoundary.Closed=true;
            var crossObjs = SpatialIndex.SelectCrossingPolygon(roofFloorBoundary);
            return Pipes.Where(o => crossObjs.Contains(o.Outline)).ToList();
        }
    }
}
