using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPEngineCore.Model.Plumbing;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Service
{
    public class ThRoofDeviceFloorRoofRainPipeService
    {
        private List<ThIfcRoofRainPipe> Pipes { get; set; }
        private ThIfcSpace Space { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThRoofDeviceFloorRoofRainPipeService(
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
            var service = new ThRoofDeviceFloorRoofRainPipeService(space, pipes);
            return service.Find();
        }
        private List<ThIfcRoofRainPipe> Find()
        {
            var roofDeviceFloorBoundary = Space.Boundary as Polyline;
            var crossObjs = SpatialIndex.SelectCrossingPolygon(roofDeviceFloorBoundary);
            return Pipes.Where(o => crossObjs.Contains(o.Outline)).ToList();
        }
    }
}
