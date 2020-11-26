using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Service
{
    public class ThDevicePlatformRoofRainPipeService
    {
        public List<ThIfcRoofRainPipe> RoofRainPipe { get; set; }
        private List<ThIfcRoofRainPipe> RoofRainPipeList { get; set; }
        private ThIfcSpace DevicePlatformSpace { get; set; }
        private ThCADCoreNTSSpatialIndex RainPipeSpatialIndex { get; set; }
        private ThDevicePlatformRoofRainPipeService(
            List<ThIfcRoofRainPipe> rainPipeList,
            ThIfcSpace devicePlatformSpace,
            ThCADCoreNTSSpatialIndex rainPipeSpatialIndex)
        {
            RoofRainPipeList = rainPipeList;
            RoofRainPipe = new List<ThIfcRoofRainPipe>();
            DevicePlatformSpace = devicePlatformSpace;
            RainPipeSpatialIndex = rainPipeSpatialIndex;
            if (RainPipeSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                RoofRainPipeList.ForEach(o => dbObjs.Add(o.Outline));
                RainPipeSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }

        }
        public static ThDevicePlatformRoofRainPipeService Find(
            List<ThIfcRoofRainPipe> rainPipeList,
            ThIfcSpace devicePlatformSpace,
            ThCADCoreNTSSpatialIndex rainPipeSpatialIndex = null)
        {
            var instance = new ThDevicePlatformRoofRainPipeService(rainPipeList, devicePlatformSpace, rainPipeSpatialIndex);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            var devicePlatformBoundary = DevicePlatformSpace.Boundary as Polyline;
            var crossObjs = RainPipeSpatialIndex.SelectCrossingPolygon(devicePlatformBoundary);
            var crossRainPipe = RoofRainPipeList.Where(o => crossObjs.Contains(o.Outline));
            var includedRoofRainPipe = crossRainPipe.Where(o => devicePlatformBoundary.Contains(o.Outline as Curve));
            includedRoofRainPipe.ForEach(o => RoofRainPipe.Add(o));
        }
    }
}
