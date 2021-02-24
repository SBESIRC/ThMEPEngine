using System;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThDevicePlatformRainPipeService
    {
        public List<ThWRainPipe> RainPipes { get; private set; }
        private List<ThWRainPipe> RainPipeList { get; set; }
        private ThIfcSpace DevicePlatformSpace { get; set; }
        private ThCADCoreNTSSpatialIndex RainPipeSpatialIndex { get; set; }
        private ThDevicePlatformRainPipeService(
            List<ThWRainPipe> rainPipeList,
            ThIfcSpace devicePlatformSpace,
            ThCADCoreNTSSpatialIndex rainPipeSpatialIndex)
        {
            RainPipeList = rainPipeList;         
            DevicePlatformSpace = devicePlatformSpace;
            RainPipeSpatialIndex = rainPipeSpatialIndex;
            if (RainPipeSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                RainPipeList.ForEach(o => dbObjs.Add(o.Outline));
                RainPipeSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
        }
        public static ThDevicePlatformRainPipeService Find(
            List<ThWRainPipe> rainPipeList,
            ThIfcSpace devicePlatformSpace,
            ThCADCoreNTSSpatialIndex rainPipeSpatialIndex = null)
        {
            var instance = new ThDevicePlatformRainPipeService(rainPipeList, devicePlatformSpace, rainPipeSpatialIndex);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            var devicePlatformBoundary = DevicePlatformSpace.Boundary as Polyline;
            var crossObjs = RainPipeSpatialIndex.SelectCrossingPolygon(devicePlatformBoundary);
            var crossRainPipe = RainPipeList.Where(o => crossObjs.Contains(o.Outline));
            RainPipes = crossRainPipe.Where(o => devicePlatformBoundary.Contains(o.Outline as Curve)).ToList();           
        }
    }
}
