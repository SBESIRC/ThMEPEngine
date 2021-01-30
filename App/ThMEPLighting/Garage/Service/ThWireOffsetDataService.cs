using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Engine;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.Garage.Service
{
    /// <summary>
    /// 中心线产生偏移后
    /// 对1号线分割
    /// 根据中心线能找到对应的1号线、2号线
    /// </summary>
    public class ThWireOffsetDataService
    {
        private double coincideTolerance { get; set; }
        /// <summary>
        /// 灯线中心线按灯槽间距Offset对应的数据
        /// </summary>
        public List<ThWireOffsetData> WireOffsetDatas { get; private set; }
        /// <summary>
        /// 1号线所有分割线的索引
        /// </summary>
        public ThQueryLineService FirstQueryInstance { get; private set; }
        private ThWireOffsetDataService(List<ThWireOffsetData> wireOffsetDatas)
        {
            WireOffsetDatas = wireOffsetDatas;
            coincideTolerance = ThGarageLightCommon.LineCoincideTolerance;
        }
        public static ThWireOffsetDataService Create(List<ThWireOffsetData> wireOffsetDatas)
        {
            var instance = new ThWireOffsetDataService(wireOffsetDatas);
            instance.Create();
            return instance;
        }
        private void Create()
        {
            var firstLines = WireOffsetDatas.Select(o => o.First).ToList();
            FirstQueryInstance = ThQueryLineService.Create(firstLines);
        }
        /// <summary>
        /// 通过1号线找到对应的2号线
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        public Line FindSecondByFirst(Line first)
        {
            var results = WireOffsetDatas.Where(o => o.First.IsCoincide(first, coincideTolerance));
            return results.Count() > 0 ? results.First().Second : new Line();
        }
        public Line FindFirstByPt(Point3d pt)
        {
            var firstLines = FirstQueryInstance.Query(pt, 2.0,false);
            return firstLines[0];
        }
    }
}
