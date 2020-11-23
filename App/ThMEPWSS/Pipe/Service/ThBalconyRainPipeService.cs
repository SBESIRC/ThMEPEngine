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
    public class ThBalconyRainPipeService
    {
        public List<ThIfcRainPipe> RainPipe { get; set; }
        private List<ThIfcRainPipe> RainPipeList { get; set; }
        private ThIfcSpace BalconySpace { get; set; }
        private ThCADCoreNTSSpatialIndex RainPipeSpatialIndex { get; set; }
        private ThBalconyRainPipeService(
            List<ThIfcRainPipe> rainPipeList,
            ThIfcSpace balconySpace,
            ThCADCoreNTSSpatialIndex rainPipeSpatialIndex)
        {
            RainPipeList = rainPipeList;
            RainPipe = new List<ThIfcRainPipe>();
            BalconySpace = balconySpace;
            RainPipeSpatialIndex = rainPipeSpatialIndex;
            if (RainPipeSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                RainPipeList.ForEach(o => dbObjs.Add(o.Outline));
                RainPipeSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }

        }
        public static ThBalconyRainPipeService Find(
            List<ThIfcRainPipe> rainPipeList,
            ThIfcSpace balconySpace,
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
            var includedRainPipe = crossRainPipe.Where(o => balconyBoundary.Contains(o.Outline as Curve));
            includedRainPipe.ForEach(o => RainPipe.Add(o));
        }
    }
}
