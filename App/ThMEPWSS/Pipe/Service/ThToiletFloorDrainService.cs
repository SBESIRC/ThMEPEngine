using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThToiletFloorDrainService
    {        
        private List<ThIfcFloorDrain> FloorDrains { get; set; }
        private ThIfcSpace ToiletSpace { get; set; }
        private ThCADCoreNTSSpatialIndex FloorDrainSpatialIndex { get; set; }
        public bool IsFinded
        {
            get
            {
                return FloorDrainCollection.Count>0;
            }
        }
        /// <summary>
        /// 找到的坐便器
        /// 目前只支持查找一个
        /// </summary>
        public List<ThIfcFloorDrain> FloorDrainCollection
        { 
            get;
            set; 
        } 
        private ThToiletFloorDrainService(
            List<ThIfcFloorDrain> floordrains, 
            ThIfcSpace toiletSpace, 
            ThCADCoreNTSSpatialIndex floordrainSpatialIndex)
        {
            FloorDrains = floordrains;
            ToiletSpace = toiletSpace;
            FloorDrainSpatialIndex = floordrainSpatialIndex;
            FloorDrainCollection = new List<ThIfcFloorDrain>();
            if (FloorDrainSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                FloorDrains.ForEach(o => dbObjs.Add(o.Outline));
                FloorDrainSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
        }
        public static ThToiletFloorDrainService Find(
            List<ThIfcFloorDrain> floordrains, 
            ThIfcSpace toiletSpace, 
            ThCADCoreNTSSpatialIndex floordrainSpatialIndex = null)
        {
            var instance = new ThToiletFloorDrainService(floordrains, toiletSpace, floordrainSpatialIndex);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            var tolitBoundary = ToiletSpace.Boundary as Polyline;
            var crossObjs = FloorDrainSpatialIndex.SelectCrossingPolygon(tolitBoundary);            
            var crossFloordrains = FloorDrains.Where(o => crossObjs.Contains(o.Outline));
            var includedFloordrains = crossFloordrains.Where(o => tolitBoundary.Contains(o.Outline as Curve));
            includedFloordrains.ForEach(o => FloorDrainCollection.Add(o));
        }        
    }
}
