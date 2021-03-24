using System;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThDevicePlatformRoofRainPipeService
    {
        public List<ThWRoofRainPipe> RoofRainPipes { get; private set; }
        private List<ThWRoofRainPipe> RoofRainPipeList { get; set; }
        private ThIfcRoom DevicePlatformSpace { get; set; }
        private ThCADCoreNTSSpatialIndex RainPipeSpatialIndex { get; set; }
        private ThDevicePlatformRoofRainPipeService(
            List<ThWRoofRainPipe> rainPipeList,
            ThIfcRoom devicePlatformSpace,
            ThCADCoreNTSSpatialIndex rainPipeSpatialIndex)
        {
            RoofRainPipeList = rainPipeList;
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
            List<ThWRoofRainPipe> rainPipeList,
            ThIfcRoom devicePlatformSpace,
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
            RoofRainPipes = crossRainPipe.Where(o => devicePlatformBoundary.Contains(o.Outline as Curve)).ToList();
        }
    }
}
