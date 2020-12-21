﻿using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Service
{
    public class ThToiletFloorDrainService
    {        
        private List<ThIfcFloorDrain> FloorDrainList { get; set; }
        private ThIfcSpace ToiletSpace { get; set; }
        private ThCADCoreNTSSpatialIndex FloorDrainSpatialIndex { get; set; }
        /// <summary>
        /// 找到的坐便器
        /// 目前只支持查找一个
        /// </summary>
        public List<ThIfcFloorDrain> FloorDrains
        { 
            get;
            set; 
        } 
        private ThToiletFloorDrainService(
            List<ThIfcFloorDrain> floordrainList, 
            ThIfcSpace toiletSpace, 
            ThCADCoreNTSSpatialIndex floordrainSpatialIndex)
        {
            FloorDrainList = floordrainList;
            ToiletSpace = toiletSpace;
            FloorDrainSpatialIndex = floordrainSpatialIndex;
            FloorDrains = new List<ThIfcFloorDrain>();
            if (FloorDrainSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                FloorDrainList.ForEach(o => dbObjs.Add(o.Outline));
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
            var crossFloordrains = FloorDrainList.Where(o => crossObjs.Contains(o.Outline));

            FloorDrains = crossFloordrains.Where(o =>
            {
                var block = o.Outline as BlockReference;
                var bufferObjs = block.GeometricExtents.ToNTSPolygon().Buffer(-10.0).ToDbCollection();
                return tolitBoundary.Contains(bufferObjs[0] as Curve);
            }).ToList();          
        }     
    }
}


