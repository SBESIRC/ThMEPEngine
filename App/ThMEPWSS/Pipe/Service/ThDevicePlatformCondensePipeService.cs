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
   public  class ThDevicePlatformCondensePipeService
    {
        public List<ThIfcCondensePipe> CondensePipe { get; set; }
        private List<ThIfcCondensePipe> CondensePipeList { get; set; }
        private ThIfcSpace DevicePlatformSpace { get; set; }
        private ThCADCoreNTSSpatialIndex CondensePipeSpatialIndex { get; set; }
        private ThDevicePlatformCondensePipeService(
            List<ThIfcCondensePipe> condensePipeList,
            ThIfcSpace devicePlatformSpace,
            ThCADCoreNTSSpatialIndex condensePipeSpatialIndex)
        {
            CondensePipeList = condensePipeList;
            CondensePipe = new List<ThIfcCondensePipe>();
            DevicePlatformSpace = devicePlatformSpace;
            CondensePipeSpatialIndex = condensePipeSpatialIndex;
            if (CondensePipeSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                CondensePipeList.ForEach(o => dbObjs.Add(o.Outline));
                CondensePipeSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }

        }
        public static ThDevicePlatformCondensePipeService Find(
            List<ThIfcCondensePipe> condensePipeList,
            ThIfcSpace devicePlatformSpace,
            ThCADCoreNTSSpatialIndex condensePipeSpatialIndex = null)
        {
            var instance = new ThDevicePlatformCondensePipeService(condensePipeList, devicePlatformSpace, condensePipeSpatialIndex);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            var devicePlatformBoundary = DevicePlatformSpace.Boundary as Polyline;
            var crossObjs = CondensePipeSpatialIndex.SelectCrossingPolygon(devicePlatformBoundary);
            var crossCondensePipe = CondensePipeList.Where(o => crossObjs.Contains(o.Outline));
            var includedCondensePipe = crossCondensePipe.Where(o => devicePlatformBoundary.Contains(o.Outline as Curve));
            includedCondensePipe.ForEach(o => CondensePipe.Add(o));
        }
    }
}
