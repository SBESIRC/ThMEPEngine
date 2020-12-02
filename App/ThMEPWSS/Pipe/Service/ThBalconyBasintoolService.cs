using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Service
{
    public class ThBalconyBasintoolService
    {
        /// <summary>
        /// 找到的台盆
        /// </summary>
        public List<ThIfcBasin> Basintools { get; set; }
        private List<ThIfcBasin> BasintoolList { get; set; }
        private ThIfcSpace BalconySpace { get; set; }
        private ThCADCoreNTSSpatialIndex BasintoolSpatialIndex { get; set; }
        private ThBalconyBasintoolService(
            List<ThIfcBasin> basintoolList,
            ThIfcSpace balconySpace,
            ThCADCoreNTSSpatialIndex basintoolSpatialIndex)
        {
            BasintoolList = basintoolList;
            Basintools = new List<ThIfcBasin>();
            BalconySpace = balconySpace;
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
            List<ThIfcBasin> basintoolList,
            ThIfcSpace balconySpace,
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
            var includedBasintools = crossBasintools.Where(o =>
            {
                var block = o.Outline as BlockReference;
                var bufferObjs = block.GeometricExtents.ToNTSPolygon().Buffer(-10.0).ToDbCollection();
                return balconyBoundary.Contains(bufferObjs[0] as Curve);
            });
            includedBasintools.ForEach(o => Basintools.Add(o));
        }
        private bool Contains(Polyline polyline, Polygon polygon)
        {
            return polyline.ToNTSPolygon().Contains(polygon);
        }
    }
}
