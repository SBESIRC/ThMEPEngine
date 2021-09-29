using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    
    public class ThFanSideConfigInfo
    {
        public int MarkHeigthType { set; get; }//风机标高类型
        public double FanMarkHeight { set; get; }//标高高度
        public ThFanConfigInfo FanConfigInfo { get; set; }//选择的风机属性
        public ObservableCollection<ThFanConfigInfo> FanInfoList { set; get; }//风机属性列表
        public ThFanSideConfigInfo()
        {
            MarkHeigthType = 1;
            FanMarkHeight = 2.5;
            FanInfoList = new ObservableCollection<ThFanConfigInfo>();
            FanInfoList.Add(new ThFanConfigInfo());
        }
    }
    public class ThAirPortSideConfigInfo
    {
        public int MarkHeigthType { set; get; }//底边标高类型
        public bool IsInsertValve { set; get; }//是否插入蝶阀
        public bool IsInsertAirPort { set; get; }//是否插入风口
        public double AirPortLength { set; get; }//风口长度
        public double AirPortHeight { set; get; }//风口高度
        public double AirPortDeepth { set; get; }//风口深度(默认)
        public double AirPortMarkHeight { set; get; }//风口标记高度
        public double AirPortWindSpeed { set; get; }//风速
        public ThAirPortSideConfigInfo()
        {
            MarkHeigthType = 1;
            IsInsertValve = true;
            IsInsertAirPort = true;
            AirPortLength = 320;
            AirPortHeight = 150;
            AirPortDeepth = 200;
            AirPortMarkHeight = 2.5;
            AirPortWindSpeed = 0.0;
        }
    }
    public class ThAirPipeConfigInfo
    {
        public int AirPortMarkType { set; get; }//风口底边标高类型
        public bool IsInsertPipe { set; get; }//是否插入风管
        public double AirPipeLength { set; get; }//风管长度
        public double AirPipeHeight { set; get; }//风管高度
        public double AirPipeWindSpeed { set; get; }//风管风速
        public double AirPipeMarkHeight { set; get; }//风管标记高度
        public double AirPortLength { set; get; }//风口长度
        public double AirPortHeight { set; get; }//风口高度
        public double AirPortDeepth { set; get; }//风口深度(默认)
        public double AirPortMarkHeight { set; get; }//风口标记高度
        public double AirPortWindSpeed { set; get; }//风速
        public ThAirPipeConfigInfo()
        {
            AirPortMarkType = 1;
            IsInsertPipe = true;
            AirPipeLength = 160.0;
            AirPipeHeight = 120.0;
            AirPipeWindSpeed = 0.0;
            AirPipeMarkHeight = 2.5;
            AirPortLength = 300;
            AirPortHeight = 150;
            AirPortDeepth = 200;
            AirPortMarkHeight = 2.5;
            AirPortWindSpeed = 0.0;
        }
    }
    public class ThFanWAFConfigInfo
    {
        public ThFanSideConfigInfo FanSideConfigInfo { set; get; }
        public ThAirPortSideConfigInfo AirPortSideConfigInfo { set; get; }
        public ThFanWAFConfigInfo()
        {
            FanSideConfigInfo = new ThFanSideConfigInfo();
            AirPortSideConfigInfo = new ThAirPortSideConfigInfo();
        }
    }
    public class ThFanWEXHConfigInfo
    {
        public ThFanSideConfigInfo FanSideConfigInfo { set; get; }
        public ThAirPortSideConfigInfo AirPortSideConfigInfo { set; get; }
        public ThFanWEXHConfigInfo()
        {
            FanSideConfigInfo = new ThFanSideConfigInfo();
            AirPortSideConfigInfo = new ThAirPortSideConfigInfo();
        }
    }
    public class ThFanCEXHConfigInfo
    {
        public ThFanSideConfigInfo FanSideConfigInfo { set; get; }//风机侧信息
        public ThAirPipeConfigInfo AirPipeConfigInfo { set; get; }//风管信息
        public ThAirPortSideConfigInfo AirPortSideConfigInfo { set; get; }//补风侧信息
        public ThFanCEXHConfigInfo()
        {
            FanSideConfigInfo = new ThFanSideConfigInfo();
            AirPipeConfigInfo = new ThAirPipeConfigInfo();
            AirPortSideConfigInfo = new ThAirPortSideConfigInfo();
        }
    }
    public class ThFanLayoutConfigInfo
    {
        public int FanType { set; get; }//风机类型
        public bool IsInsertHole { set; get; }//插入墙洞
        public string MapScale { set; get; }//出图比例
        public ThFanWAFConfigInfo WAFConfigInfo { set; get; }//壁式轴流风机
        public ThFanWEXHConfigInfo WEXHConfigInfo { set; get; }//壁式排气扇
        public ThFanCEXHConfigInfo CEXHConfigInfo { set; get; }//吊顶式排气扇信息
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
