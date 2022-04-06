using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundWaterSystem.Model
{
    /// <summary>
    /// 楼层信息，包含楼层所有的元素和信息
    /// </summary>
    public class ThFloorInfo
    {
        /// <summary>
        /// 横管线
        /// </summary>
        public List<Line> PipeLines { set; get; }
        /// <summary>
        /// 标注数据表
        /// </summary>
        public List<ThMarkModel> MarkList { set; get; }
        /// <summary>
        /// 立管数据表
        /// </summary>
        public List<ThRiserModel> RiserList { set; get; }
        /// <summary>
        /// 管径标注
        /// </summary>
        public List<ThDimModel> DimList { set; get; }
        //ToDo1:水角阀平面数据
        //ToDo2:阀门数据
    }
}
