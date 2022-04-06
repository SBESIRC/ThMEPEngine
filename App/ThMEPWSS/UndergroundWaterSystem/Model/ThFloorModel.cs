using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundWaterSystem.Model
{
    public class ThFloorModel
    {
        /// <summary>
        /// 楼层编号
        /// </summary>
        public string FloorName { set; get; }
        /// <summary>
        /// 楼层范围
        /// </summary>
        public Polyline FloorArea { set; get; }
        public ThFloorInfo FloorInfo { set; get; }
        public double FloorNumber()
        {
            var str = Regex.Replace(FloorName, @"[^\d.\d]", "");
            double resDouble = double.Parse(str);
            if(FloorName.Contains("B"))
            {
                resDouble = -resDouble;
            }
            return resDouble;
        }
    }
}
