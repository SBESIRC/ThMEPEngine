using System;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;
using ThMEPWSS.Assistant;

namespace ThMEPWSS.Pipe.Service
{
    public class ThDevicePlatformFloorDrainService
    {
        public List<ThIfcFloorDrain> FloorDrains { get; private set; }
        private List<ThIfcFloorDrain> FloorDrainList { get; set; }
        private ThIfcSpace DevicePlatformSpace { get; set; }
        private ThCADCoreNTSSpatialIndex FloorDrainSpatialIndex { get; set; }
        /// <summary>
        
        private ThDevicePlatformFloorDrainService(
          List<ThIfcFloorDrain> floordrainList,
          ThIfcSpace devicePlatformSpace,
          ThCADCoreNTSSpatialIndex floordrainSpatialIndex)
        {
            FloorDrainList = floordrainList;
            DevicePlatformSpace = devicePlatformSpace;
            FloorDrainSpatialIndex = floordrainSpatialIndex;         
            if (FloorDrainSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                FloorDrainList.ForEach(o => dbObjs.Add(o.Outline));
                FloorDrainSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
        }
        public static ThDevicePlatformFloorDrainService Find(
         List<ThIfcFloorDrain> floordrains,
         ThIfcSpace devicePlatformSpace,
         ThCADCoreNTSSpatialIndex floordrainSpatialIndex = null)
        {
            var instance = new ThDevicePlatformFloorDrainService(floordrains, devicePlatformSpace, floordrainSpatialIndex);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            // to do curve 封闭逻辑放到模型那边统一预处理，这边只是暂时处理
            var devicePlatformBoundary = DevicePlatformSpace.Boundary.Clone() as Polyline;
            devicePlatformBoundary.Closed = true;                   
            var crossObjs = FloorDrainSpatialIndex.SelectCrossingPolygon(devicePlatformBoundary);
            var crossFloordrains = FloorDrainList.Where(o => crossObjs.Contains(o.Outline));
            FloorDrains = crossFloordrains.Where(o =>
            {
                var block = o.Outline as BlockReference;
                var bufferObjs = block.GeometricExtents.ToNTSPolygon().Buffer(-10.0).ToDbCollection();
                return devicePlatformBoundary.Contains(bufferObjs[0] as Curve);
            }).ToList();                    
        }     
    }
}


