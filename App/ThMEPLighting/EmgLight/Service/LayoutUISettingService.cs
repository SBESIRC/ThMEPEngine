using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.EmgLight.Service
{
    public class LayoutUISettingService
    {
        public double scale { get; set; }
        public int singleSide { get; set; } //0:double side, 1:single side
        public int blkType { get; set; }

        public static LayoutUISettingService Instance = new LayoutUISettingService();

        public LayoutUISettingService()
        {
            scale = EmgLightCommon.BlockScaleNum;
            blkType = 0;
            singleSide = 0;
        }
    }
}
