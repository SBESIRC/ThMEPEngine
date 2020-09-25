using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Model
{
    public enum SensorType
    {
        TEMPERATURESENSOR, // 温感传感器
        SMOKESENSOR, // 烟感传感器
    }

    /// <summary>
    /// 布置参数
    /// </summary>
    public class PlaceParameter
    {
        public double ProtectArea = 60 * 1e6;
        public double ProtectRadius = 5.8 * 1e3;

        public double FirstBottomProtectRadius = 5.3 * 1e3;

        public double VerticalMaxGap = 10.5 * 1e3;

        public double HorizontalMaxGap = 10.5 * 1e3;

        public SensorType sensorType; // 传感器类型
        
        public PlaceParameter(SensorType sensorType = SensorType.SMOKESENSOR, double paraArea = 60 * 1e6, double paraRadius = 5.8 * 1e3, double verticalGap = 10.5 * 1e3, double vertexProtectRadius = 5.3 * 1e3)
        {
            ProtectArea = paraArea;
            ProtectRadius = paraRadius;
            VerticalMaxGap = verticalGap;
            HorizontalMaxGap = VerticalMaxGap;
            FirstBottomProtectRadius = vertexProtectRadius;
            this.sensorType = sensorType;
        }
    }
}
