using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.AlarmSensorLayout.Data
{
    public enum EquipmentType
    {
        TEMPERATURESENSOR, // 温感传感器
        SMOKESENSOR, // 烟感传感器
        LIGHTING, //灯
    }
    public class EquipmentParameter
    {
        public double ProtectRadius = 5.8 * 1e3;//保护半径
        public EquipmentType equipmentType = EquipmentType.LIGHTING; //盲区类型
        public double MinGap = 5400;
        public double MaxGap = 7500;
        public double AdjustGap = 8000;


        public EquipmentParameter(double paraRadius = 5.8 * 1e3, EquipmentType paraType = EquipmentType.LIGHTING,
            double paraMin = 5400, double paraAdjust = 7500, double paraMax = 8200)
        {
            ProtectRadius = paraRadius;
            equipmentType = paraType;
            MinGap = paraMin;
            MaxGap = paraMax;
            AdjustGap = paraAdjust;
        }
    }
}
