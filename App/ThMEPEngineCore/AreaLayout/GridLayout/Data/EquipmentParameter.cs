using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.AreaLayout.GridLayout.Data
{
    public enum BlindType
    {
        VisibleArea, // 可见区域
        CoverArea, // 覆盖区域
    }
    public class EquipmentParameter
    {
        public double ProtectRadius = 5.8 * 1e3;//保护半径
        public BlindType blindType = BlindType.VisibleArea; //盲区类型
        public double MinGap = 5300;
        public double MaxGap = 8200;
        public double AdjustGap = 7600;


        public EquipmentParameter(double paraRadius = 5.8 * 1e3, BlindType paraType = BlindType.VisibleArea)
        {
            ProtectRadius = paraRadius;
            blindType = paraType;
            MinGap = paraRadius / 58 * 53;
            MaxGap = paraRadius / 58 * 82;
            AdjustGap = paraRadius / 58 * 76;
        }
    }
}
