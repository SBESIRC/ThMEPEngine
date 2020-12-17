using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Service
{
    public class ThToiletClosestoolService
    {
        /// <summary>
        /// 找到的坐便器
        /// </summary>
        public List<ThIfcClosestool> Closestools { get; private set; }
        private List<ThIfcClosestool> ClosestoolList { get; set; }
        private ThIfcSpace ToiletSpace { get; set; }
        private ThCADCoreNTSSpatialIndex ClosestoolSpatialIndex { get; set; }
        private ThToiletClosestoolService(
            List<ThIfcClosestool> closestoolList, 
            ThIfcSpace toiletSpace, 
            ThCADCoreNTSSpatialIndex closestoolSpatialIndex)
        {
            ClosestoolList = closestoolList;
            Closestools = new List<ThIfcClosestool>();
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
            List<ThIfcClosestool> closestoolList, 
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
            Closestools = crossClosestools.Where(o => tolitBoundary.Contains(o.Outline as Curve)).ToList();            
        }        
    }
}
