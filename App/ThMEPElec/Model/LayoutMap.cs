using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Layout;

namespace ThMEPElectrical.Model
{
    /// <summary>
    /// 主次梁信息和对应的布置逻辑的映射关系
    /// </summary>
    public class LayoutMap
    {
        public PlaceInputProfileData InputProfileData;
        public SensorLayout LayoutSelect;

        public PlaceParameter SensorParameter;

        public LayoutMap(PlaceInputProfileData inputProfileData, SensorLayout layoutSelect, PlaceParameter placeParameter)
        {
            InputProfileData = inputProfileData;
            LayoutSelect = layoutSelect;
            SensorParameter = placeParameter;
        }
    }
}
