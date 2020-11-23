using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using NetTopologySuite.Geometries;
using ThMEPEngineCore.Model.Plumbing;


namespace ThMEPWSS.Pipe.Service
{
    public class ThDevicePlatformFloorDrainService
    {
        private List<ThIfcFloorDrain> FloorDrainList { get; set; }
        private ThIfcSpace DevicePlatformSpace { get; set; }
        private ThCADCoreNTSSpatialIndex FloorDrainSpatialIndex { get; set; }
        /// <summary>
        public List<ThIfcFloorDrain> FloorDrains
        {
            get;

            set;
        }
        private ThDevicePlatformFloorDrainService(
          List<ThIfcFloorDrain> floordrainList,
          ThIfcSpace devicePlatformSpace,
          ThCADCoreNTSSpatialIndex floordrainSpatialIndex)
        {
            FloorDrainList = floordrainList;
            DevicePlatformSpace = devicePlatformSpace;
            FloorDrainSpatialIndex = floordrainSpatialIndex;
            FloorDrains = new List<ThIfcFloorDrain>();
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
            var devicePlatformBoundary = DevicePlatformSpace.Boundary as Polyline;
            var crossObjs = FloorDrainSpatialIndex.SelectCrossingPolygon(devicePlatformBoundary);
            var crossFloordrains = FloorDrainList.Where(o => crossObjs.Contains(o.Outline));
            var includedFloordrains = crossFloordrains.Where(o =>
            {
                var block = o.Outline as BlockReference;
                var bufferObjs = block.GeometricExtents.ToNTSPolygon().Buffer(-10.0).ToDbCollection();
                return devicePlatformBoundary.Contains(bufferObjs[0] as Curve);
            });         
            includedFloordrains.ForEach(o => FloorDrains.Add(o));
        }
        private bool Contains(Polyline polyline, Polygon polygon)
        {
            return polyline.ToNTSPolygon().Contains(polygon);
        }
    }
}


