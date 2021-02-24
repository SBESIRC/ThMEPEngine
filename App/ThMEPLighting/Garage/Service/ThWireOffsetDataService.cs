﻿using System.Linq;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    /// <summary>
    /// 中心线产生偏移后
    /// 对1号线分割
    /// 根据中心线能找到对应的1号线、2号线
    /// </summary>
    public class ThWireOffsetDataService
    {
        /// <summary>
        /// 灯线中心线按灯槽间距Offset对应的数据
        /// </summary>
        public List<ThWireOffsetData> WireOffsetDatas { get; private set; }
        /// <summary>
        /// 1号线所有分割线的索引
        /// </summary>
        public ThQueryLineService FirstQueryInstance { get; private set; }
        public ThWireOffsetDataService(List<ThWireOffsetData> wireOffsetDatas)
        {
            WireOffsetDatas = wireOffsetDatas;
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
            var results = WireOffsetDatas.Where(o => o.First.IsCoincide(first, ThGarageLightCommon.LineCoincideTolerance));
            return results.Count() > 0 ? results.First().Second : new Line();
        }
        public Line FindFirstByPt(Point3d pt)
        {
            var firstLines = FirstQueryInstance.Query(pt,20.0,false);
            return firstLines[0];
        }
    }
}
