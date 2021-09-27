using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FanLayout.ViewModel
{
    public class ThFanConfigInfo
    {
        public string FanNumber { set; get; }//设备编号
        public double FanVolume { set; get; }//风量
        public double FanPressure { set; get; }//全压
        public double FanPower { set; get; }//电量=功率
        public double FanWeight { set; get; }//重量
        public double FanNoise { set; get; } //噪音
        public double FanDepth { set; get; }//深度
        public double FanWidth { set; get; }//宽度
        public double FanLength { set; get; }//长度
        public ThFanConfigInfo()
        {
            FanNumber = "DZ-1100";
            FanVolume = 1100;
            FanPressure = 50;
            FanPower = 50;
            FanWeight = 50;
            FanNoise = 37;
            FanDepth = 305;
            FanWidth = 370;
            FanLength = 370;
        }
    }

    public class ThFanCEXHConfigInfo
    {
        public int HoleMarkHeigthType { set; get; }//洞口标高类型
        public bool IsInsertHole { set; get; }//是否插入洞口
        public bool IsInsertValve { set; get; }//是否插入防火阀
        public bool IsInsertAirPortAndPipe { set; get; }//是否插入风管和排风口
        public double HoleMarkHeigth { set; get; }//洞口标高
        public double AirPipeLength { set; get; }//风管长度
        public double AirPipeWidth { set; get; }//风管宽度
        public double AirPipeWindSpeed { set; get; }//风管风速
        public double AirPipeMarkHeigth { set; get; }//底部标高
        public double ExAirPortLength { set; get; }//排风百叶长度
        public double ExAirPortWidth { set; get; }//排风百叶宽
        public double ExAirPortWindSpeed { set; get; }//排风百叶风速
        public double InAirPortLength { set; get; }//补风百叶长度
        public double InAirPortWidth { set; get; }//补风百叶宽度
        public double InAirPortWindSpeed { set; get; }//补风百叶风速
        public ThFanConfigInfo FanConfigInfo { get; }//选择的风机属性
        public List<ThFanConfigInfo> FanInfoList { set; get; }
        public ThFanCEXHConfigInfo()
        {
            HoleMarkHeigthType = 1;
            IsInsertHole = true;
            IsInsertValve = true;
            IsInsertAirPortAndPipe = true;
            HoleMarkHeigth = 2.5;
            AirPipeLength = 800.0;
            AirPipeWidth = 300.0;
            AirPipeWindSpeed = 100.0;
            AirPipeMarkHeigth = 2.5;
            ExAirPortLength = 800;
            ExAirPortWidth = 300;
            ExAirPortWindSpeed = 100;
            InAirPortLength = 800;
            InAirPortWidth = 300;
            InAirPortWindSpeed = 100;
            FanInfoList = new List<ThFanConfigInfo>();
            FanConfigInfo = new ThFanConfigInfo();
        }
    }
    public class ThFanWAFConfigInfo
    {
        public int FanMarkHeigthType { set; get; }
        public int AirPortMarkHeigthType { set; get; }
        public bool IsInsertHole { set; get; }
        public bool IsInsertValve { set; get; }
        public bool IsInsertAirPort { set; get; }
        public double FanMarkHeight { set; get; }
        public double AirPortLength { set; get; }
        public double AirPortHeight { set; get; }
        public double AirPortDeepth { set; get; }
        public double AirPortMarkHeight { set; get; }
        public double AirPortWindSpeed { set; get; }
        public ThFanConfigInfo FanConfigInfo { get; }//选择的风机属性
        public List<ThFanConfigInfo> FanInfoList { set; get; }
        public ThFanWAFConfigInfo()
        {
            FanMarkHeigthType = 1;
            AirPortMarkHeigthType = 1;
            IsInsertHole = true;
            IsInsertValve = true;
            IsInsertAirPort = true;
            FanMarkHeight = 2.5;
            AirPortLength = 320;
            AirPortHeight = 150;
            AirPortDeepth = 200;
            AirPortMarkHeight = 2.5;
            AirPortWindSpeed = 0.0;
            FanInfoList = new List<ThFanConfigInfo>();
            FanConfigInfo = new ThFanConfigInfo();
        }
    }
    public class ThFanWEXHConfigInfo
    {
        public int FanMarkHeigthType { set; get; }
        public int AirPortMarkHeigthType { set; get; }
        public bool IsInsertHole { set; get; }
        public bool IsInsertValve { set; get; }
        public bool IsInsertAirPort { set; get; }
        public double FanMarkHeight { set; get; }
        public double AirPortLength { set; get; }
        public double AirPortHeight { set; get; }
        public double AirPortDeepth { set; get; }
        public double AirPortMarkHeight { set; get; }
        public double AirPortWindSpeed { set; get; }
        public ThFanConfigInfo FanConfigInfo { get; }//选择的风机属性
        public List<ThFanConfigInfo> FanInfoList { set; get; }
        public ThFanWEXHConfigInfo()
        {
            FanMarkHeigthType = 1;
            AirPortMarkHeigthType = 1;
            IsInsertHole = true;
            IsInsertValve = true;
            IsInsertAirPort = true;
            FanMarkHeight = 2.5;
            AirPortLength = 320;
            AirPortHeight = 150;
            AirPortDeepth = 200;
            AirPortMarkHeight = 2.5;
            AirPortWindSpeed = 0.0;
            FanInfoList = new List<ThFanConfigInfo>();
            FanConfigInfo = new ThFanConfigInfo();
        }
    }
    public class ThFanLayoutConfigInfo
    {
        public int FanType { set; get; }//风机类型
        public bool IsInsertHole { set; get; }//插入墙洞
        public string MapScale { set; get; }//出图比例
        public ThFanWAFConfigInfo WAFConfigInfo { get; }//壁式轴流风机
        public ThFanWEXHConfigInfo WEXHConfigInfo { get; }//壁式排气扇
        public ThFanCEXHConfigInfo CEXHConfigInfo { get; }//吊顶式排气扇信息
        public ThFanLayoutConfigInfo()
        {
            FanType = 0;
            MapScale = "1:100";
            IsInsertHole = true;
            WAFConfigInfo = new ThFanWAFConfigInfo();
            WEXHConfigInfo = new ThFanWEXHConfigInfo();
            CEXHConfigInfo = new ThFanCEXHConfigInfo();
        }
    }
}
