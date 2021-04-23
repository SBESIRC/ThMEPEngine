using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using Dreambuild.AutoCAD;
using ThMEPWSS.Pipe.Geom;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThToiletFloorDrainService
    {
        private List<ThWFloorDrain> FloorDrainList { get; set; }
        private ThIfcSpace ToiletSpace { get; set; }
        private ThCADCoreNTSSpatialIndex FloorDrainSpatialIndex { get; set; }
        /// <summary>
        /// 找到的坐便器
        /// 目前只支持查找一个
        /// </summary>
        public List<ThWFloorDrain> FloorDrains
        {
            get;
            set;
        }
        private ThToiletFloorDrainService(
            List<ThWFloorDrain> floordrainList,
            ThIfcSpace toiletSpace,
            ThCADCoreNTSSpatialIndex floordrainSpatialIndex)
        {
            FloorDrainList = floordrainList;
            ToiletSpace = toiletSpace;
            FloorDrainSpatialIndex = floordrainSpatialIndex;
            FloorDrains = new List<ThWFloorDrain>();
            if (FloorDrainSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                FloorDrainList.ForEach(o => dbObjs.Add(o.Outline));
                FloorDrainSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
        }
        public static ThToiletFloorDrainService Find(
            List<ThWFloorDrain> floordrains,
            ThIfcSpace toiletSpace,
            ThCADCoreNTSSpatialIndex floordrainSpatialIndex = null)
        {
            var instance = new ThToiletFloorDrainService(floordrains, toiletSpace, floordrainSpatialIndex);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            var drains = new List<ThWFloorDrain>();
            var tolitBoundary = ToiletSpace.Boundary as Polyline;
            var crossObjs = FloorDrainSpatialIndex.SelectCrossingPolygon(tolitBoundary);
            var crossFloordrains = FloorDrainList.Where(o => crossObjs.Contains(o.Outline));

            drains = crossFloordrains.Where(o =>
            {
                var block = o.Outline as BlockReference;
                var bufferObjs = block.GeometricExtents.ToNTSPolygon().Buffer(-10.0).ToDbCollection();
                return tolitBoundary.Contains(bufferObjs[0] as Curve);
            }).ToList();
            foreach (var drain in drains)
            {
                FloorDrains.Add(drain);
            }
            foreach (var drain in FloorDrainList)
            {
                if (!GeomUtils.PtInLoop(tolitBoundary, drain.Outline.GetCenter()) && drain.Outline.GetCenter().DistanceTo(tolitBoundary.GetCenter()) < ThWPipeCommon.MAX_TOILET_TO_FLOORDRAIN_DISTANCE)
                {
                    FloorDrains.Add(drain);
                }
            }
        }
    }
}


