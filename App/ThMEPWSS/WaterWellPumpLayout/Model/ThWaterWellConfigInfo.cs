using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.WaterWellPumpLayout.Model
{
    public class ThWaterWellConfigInfo
    {
        public int WellCount { set; get; }//数量
        public bool IsDisplay { set; get; }//是否显示
        public double WellArea { set; get; }//集水井面积
        public string PumpCount { set; get; }//泵数量
        public string BlockName { set; get; }//图块名称
        public string WellSize { set; get; }//集水井尺寸
        public string PumpNumber { set; get; }//泵编号
        public List<ThWaterWellModel> WellModelList { set; get; }//集水井Model
        public ThWaterWellConfigInfo()
        {
            WellCount = 0;
            PumpCount = "2";
            IsDisplay = false;
            WellArea = 0.00;
            BlockName = "";
            WellSize = "0*0";
            PumpNumber = "A1";
            WellModelList = new List<ThWaterWellModel>();
        }

    }
}
