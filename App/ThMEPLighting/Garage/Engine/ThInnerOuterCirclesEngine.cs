using System;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Engine
{
    public class ThInnerOuterCirclesEngine : IDisposable
    {
        public ThInnerOuterCirclesEngine()
        {            
        } 
        public void Dispose()
        {           
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mergeCurves">合并之后的车道中心线</param>
        /// <param name="halfCableTraySpace">双排间距(如:2700)的一半</param>
        /// <returns></returns>
        public List<ThWireOffsetData> Reconize(List<Curve> mergeCurves, double halfCableTraySpace)
        {
            //通过中心线往两侧偏移            
            var offsetCurves = mergeCurves.Offset(halfCableTraySpace);

            //让1号线、2号线连接
            offsetCurves = ThExtendService.Extend(offsetCurves, halfCableTraySpace);            

            //为中心线找到对应的1号线和2号线
            var dxWireOffsetDatas=ThFindFirstLinesService.Find(offsetCurves, halfCableTraySpace);
            return dxWireOffsetDatas;
        }  
    }
}
