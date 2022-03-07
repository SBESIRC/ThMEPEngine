using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.HydrantLayout.Model
{
    public class HydrantLayoutSetting
    {
        public int LayoutObject { get; set; }//消火栓（0）灭火器（1）两者都考虑（2）
        public int SearchRadius { get; set; }
        public int LayoutMode { get; set; }//一字（0） L字（1） 两者都考虑（2）

        public static HydrantLayoutSetting Instance = new HydrantLayoutSetting();
        public HydrantLayoutSetting()
        {
            LayoutObject = 2;
            SearchRadius = 3000;
            LayoutMode = 2;
        }
    }
}
