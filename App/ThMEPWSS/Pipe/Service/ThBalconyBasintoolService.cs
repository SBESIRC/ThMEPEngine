using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThBalconyBasintoolService
    {
        public List<ThWBasin> Basintools { get; private set; }
        private List<ThWBasin> BasintoolList { get; set; }
        private ThIfcRoom BalconySpace { get; set; }
        private ThCADCoreNTSSpatialIndex BasintoolSpatialIndex { get; set; }
        private ThBalconyBasintoolService(
            List<ThWBasin> basintoolList,
            ThIfcRoom balconySpace,
            ThCADCoreNTSSpatialIndex basintoolSpatialIndex)
        {
            BalconySpace = balconySpace;
            BasintoolList = basintoolList;
            BasintoolSpatialIndex = basintoolSpatialIndex;
            if (BasintoolSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                BasintoolList.ForEach(o => dbObjs.Add(o.Outline));
                BasintoolSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
        }
        /// <summary>
        /// 创建查找阳台内查找台盆的实例
        /// </summary>
        /// <param name="basintools">查找范围内的所有台盆</param>
        /// <param name="balconySpace">阳台空间</param>
        /// <param name="basintoolSpatialIndex">厨房索引空间</param>
        /// <returns></returns>
        public static ThBalconyBasintoolService Find(
            List<ThWBasin> basintoolList,
            ThIfcRoom balconySpace,
            ThCADCoreNTSSpatialIndex basintoolSpatialIndex = null)
        {
            var instance = new ThBalconyBasintoolService(basintoolList, balconySpace, basintoolSpatialIndex);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            var balconyBoundary = BalconySpace.Boundary as Polyline;
            var crossObjs = BasintoolSpatialIndex.SelectCrossingPolygon(balconyBoundary);
            var crossBasintools = BasintoolList.Where(o => crossObjs.Contains(o.Outline));
            Basintools = crossBasintools.Where(o =>
            {
                var block = o.Outline as BlockReference;
                var bufferObjs = block.GeometricExtents.ToNTSPolygon().Buffer(-10.0).ToDbCollection();
                return balconyBoundary.Contains(bufferObjs[0] as Curve);
            }).ToList();
        }
    }
}
