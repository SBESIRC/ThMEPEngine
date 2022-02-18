using AcHelper.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.Pipe.Model
{
    public class WaterWellIdentifyConfigInfo
    {
        public List<string> WhiteList = new List<string>();//白名单
        public List<string> BlackList = new List<string>();//黑名单
        public WaterWellIdentifyConfigInfo()
        {
            WhiteList.Add("A-Well-1");
            WhiteList.Add("集水坑");
            WhiteList.Add("沉砂隔油池");

            BlackList.Add("");
            BlackList.Add("");
            BlackList.Add("");
        }
    }
    public class WaterWellConfigInfo
    {
        public WaterWellIdentifyConfigInfo identifyInfo = new WaterWellIdentifyConfigInfo();//集水井图块识别信息
        public bool isWaterWellSizeFilter = true;//集水井尺寸过滤
        public double fMinacreage = 1.0;//最小面积
        public string strFloorlocation = "B1";//楼层
    }
    public class PumpConfigInfo
    {
        public string strNumberPrefix = "A";//编号前缀
        public string strPipeDiameter = "DN80";//管径
        public string strMapScale = "1:150";//出图比例
        public int PumpsNumber = 2;//单井水泵数量
        public bool isCoveredWaterWell = false;//覆盖已布置水井
    }

    public class WaterWellPumpConfigInfo
    {
        public PumpConfigInfo PumpInfo = new PumpConfigInfo();//水泵配置信息
        public WaterWellConfigInfo WaterWellInfo = new WaterWellConfigInfo();//水井配置信息
    }

    public class ThWaterWellPumpArrangement : IAcadCommand, IDisposable
    {
        public WaterWellPumpConfigInfo ConfigInfo = new WaterWellPumpConfigInfo();
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
        }
    }
}
