using System;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThToiletClosestoolService
    {
        /// <summary>
        /// 找到的坐便器
        /// </summary>
        public List<ThWClosestool> Closestools { get; private set; }
        private List<ThWClosestool> ClosestoolList { get; set; }
        private ThIfcSpace ToiletSpace { get; set; }
        private ThCADCoreNTSSpatialIndex ClosestoolSpatialIndex { get; set; }
        private ThToiletClosestoolService(
            List<ThWClosestool> closestoolList,
            ThIfcSpace toiletSpace,
            ThCADCoreNTSSpatialIndex closestoolSpatialIndex)
        {
            ClosestoolList = closestoolList;
            Closestools = new List<ThWClosestool>();
            ToiletSpace = toiletSpace;
            ClosestoolSpatialIndex = closestoolSpatialIndex;
            if (ClosestoolSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                ClosestoolList.ForEach(o => dbObjs.Add(o.Outline));
                ClosestoolSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
        }
        /// <summary>
        /// 创建查找卫生间内查找坐便器的实例
        /// </summary>
        /// <param name="closestools">查找范围内的所有坐便器</param>
        /// <param name="toiletSpace">卫生间空间</param>
        /// <param name="closestoolSpatialIndex">卫生间索引空间</param>
        /// <returns></returns>
        public static ThToiletClosestoolService Find(
            List<ThWClosestool> closestoolList,
            ThIfcSpace toiletSpace,
            ThCADCoreNTSSpatialIndex closestoolSpatialIndex = null)
        {
            var instance = new ThToiletClosestoolService(closestoolList, toiletSpace, closestoolSpatialIndex);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            var tolitBoundary = ToiletSpace.Boundary as Polyline;
            var crossObjs = ClosestoolSpatialIndex.SelectCrossingPolygon(tolitBoundary);
            var crossClosestools = ClosestoolList.Where(o => crossObjs.Contains(o.Outline));
            Closestools = crossClosestools.Where(o =>
            {
                var block = o.Outline as Polyline;
                var bufferObjs = block.GeometricExtents.ToNTSPolygon().Buffer(-20.0).ToDbCollection();
                return tolitBoundary.Contains(bufferObjs[0] as Curve);
            }).ToList();

        }
    }
}
