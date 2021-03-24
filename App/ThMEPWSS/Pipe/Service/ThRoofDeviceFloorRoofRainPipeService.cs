using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThRoofDeviceFloorRoofRainPipeService
    {
        private List<ThWRoofRainPipe> Pipes { get; set; }
        private ThIfcRoom Space { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThRoofDeviceFloorRoofRainPipeService(
           ThIfcRoom space,
           List<ThWRoofRainPipe> pipes)
        {
            Pipes = pipes;
            Space = space;
            var objs = new DBObjectCollection();
            Pipes.ForEach(o => objs.Add(o.Outline));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        public static List<ThWRoofRainPipe> Find(
            ThIfcRoom space,
            List<ThWRoofRainPipe> pipes)
        {
            var service = new ThRoofDeviceFloorRoofRainPipeService(space, pipes);
            return service.Find();
        }
        private List<ThWRoofRainPipe> Find()
        {
            var roofDeviceFloorBoundary = Space.Boundary as Polyline;
            var crossObjs = SpatialIndex.SelectCrossingPolygon(roofDeviceFloorBoundary);
            return Pipes.Where(o => crossObjs.Contains(o.Outline)).ToList();
        }
    }
}
