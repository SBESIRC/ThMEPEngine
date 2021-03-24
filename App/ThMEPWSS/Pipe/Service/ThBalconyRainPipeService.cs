using System;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThBalconyRainPipeService
    {
        public List<ThWRainPipe> RainPipes { get; private set; }
        private List<ThWRainPipe> RainPipeList { get; set; }
        private ThIfcRoom BalconySpace { get; set; }
        private ThCADCoreNTSSpatialIndex RainPipeSpatialIndex { get; set; }
        private ThBalconyRainPipeService(
            List<ThWRainPipe> rainPipeList,
            ThIfcRoom balconySpace,
            ThCADCoreNTSSpatialIndex rainPipeSpatialIndex)
        {
            BalconySpace = balconySpace;
            RainPipeList = rainPipeList;
            RainPipeSpatialIndex = rainPipeSpatialIndex;
            if (RainPipeSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                RainPipeList.ForEach(o => dbObjs.Add(o.Outline));
                RainPipeSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }

        }
        public static ThBalconyRainPipeService Find(
            List<ThWRainPipe> rainPipeList,
            ThIfcRoom balconySpace,
            ThCADCoreNTSSpatialIndex rainPipeSpatialIndex = null)
        {
            var instance = new ThBalconyRainPipeService(rainPipeList, balconySpace, rainPipeSpatialIndex);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            var balconyBoundary = BalconySpace.Boundary as Polyline;
            var crossObjs = RainPipeSpatialIndex.SelectCrossingPolygon(balconyBoundary);
            var crossRainPipe = RainPipeList.Where(o => crossObjs.Contains(o.Outline));
            RainPipes = crossRainPipe.Where(o => balconyBoundary.Contains(o.Outline as Curve)).ToList();
        }
    }
}
