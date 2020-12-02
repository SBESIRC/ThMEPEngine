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
   public class ThBalconyFloorDrainService
    {
        private List<ThIfcFloorDrain> FloorDrainList { get; set; }
        private ThIfcSpace BalconySpace { get; set; }
        private ThCADCoreNTSSpatialIndex FloorDrainSpatialIndex { get; set; }
        /// <summary>
 
        public List<ThIfcFloorDrain> FloorDrains
        {
            get;
            set;
        }
        private ThBalconyFloorDrainService(
           List<ThIfcFloorDrain> floordrainList,
           ThIfcSpace balconySpace,
           ThCADCoreNTSSpatialIndex floordrainSpatialIndex)
        {
            FloorDrainList = floordrainList;
            BalconySpace = balconySpace;
            FloorDrainSpatialIndex = floordrainSpatialIndex;
            FloorDrains = new List<ThIfcFloorDrain>();
            if (FloorDrainSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                FloorDrainList.ForEach(o => dbObjs.Add(o.Outline));
                FloorDrainSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
        }
        public static ThBalconyFloorDrainService Find(
          List<ThIfcFloorDrain> floordrains,
          ThIfcSpace balconySpace,
          ThCADCoreNTSSpatialIndex floordrainSpatialIndex = null)
        {
            var instance = new ThBalconyFloorDrainService(floordrains, balconySpace, floordrainSpatialIndex);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            var balconyBoundary = BalconySpace.Boundary as Polyline;
            var crossObjs = FloorDrainSpatialIndex.SelectCrossingPolygon(balconyBoundary);
            var crossFloordrains = FloorDrainList.Where(o => crossObjs.Contains(o.Outline));
            var includedFloordrains = crossFloordrains.Where(o =>
            {
                var block = o.Outline as BlockReference;
                var bufferObjs = block.GeometricExtents.ToNTSPolygon().Buffer(-10.0).ToDbCollection();
                return balconyBoundary.Contains(bufferObjs[0] as Curve);
            });      
            includedFloordrains.ForEach(o => FloorDrains.Add(o));
        }
  
        private bool Contains(Polyline polyline, Polygon polygon)
        {
            return polyline.ToNTSPolygon().Contains(polygon);
        }
    }
}
