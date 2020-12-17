﻿using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Service
{
    public class ThKitchenBasintoolService
    {
        /// <summary>
        /// 找到的台盆
        /// </summary>
        public List<ThIfcBasin> Basintools { get; private set; }
        private List<ThIfcBasin> BasintoolList { get; set; }
        private ThIfcSpace KitchenSpace { get; set; }
        private ThCADCoreNTSSpatialIndex BasintoolSpatialIndex { get; set; }
        private ThKitchenBasintoolService(
            List<ThIfcBasin> basintoolList,
            ThIfcSpace kitchenSpace,
            ThCADCoreNTSSpatialIndex basintoolSpatialIndex)
        {
            BasintoolList = basintoolList;
            Basintools = new List<ThIfcBasin>();
            KitchenSpace = kitchenSpace;
            BasintoolSpatialIndex = basintoolSpatialIndex;
            if (BasintoolSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                BasintoolList.ForEach(o => dbObjs.Add(o.Outline));
                BasintoolSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
        }
        /// <summary>
        /// 创建查找厨房内查找台盆的实例
        /// </summary>
        /// <param name="basintools">查找范围内的所有台盆</param>
        /// <param name="kitchenSpace">厨房空间</param>
        /// <param name="basintoolSpatialIndex">厨房索引空间</param>
        /// <returns></returns>
        public static ThKitchenBasintoolService Find(
            List<ThIfcBasin> basintoolList,
            ThIfcSpace kitchenSpace,
            ThCADCoreNTSSpatialIndex basintoolSpatialIndex = null)
        {
            var instance = new ThKitchenBasintoolService(basintoolList, kitchenSpace, basintoolSpatialIndex);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            var kitchenBoundary = KitchenSpace.Boundary as Polyline;
            var crossObjs = BasintoolSpatialIndex.SelectCrossingPolygon(kitchenBoundary);
            var crossBasintools = BasintoolList.Where(o => crossObjs.Contains(o.Outline));
            var includedBasintools = crossBasintools.Where(o =>
            {
                var block = o.Outline as BlockReference;
                var bufferObjs = block.GeometricExtents.ToNTSPolygon().Buffer(-10.0).ToDbCollection();
                return kitchenBoundary.Contains(bufferObjs[0] as Curve);
            });
            includedBasintools.ForEach(o => Basintools.Add(o));
        }
    }
}

    

