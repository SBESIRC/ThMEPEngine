﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FanConnect.ViewModel
{
    public class ThWaterSystemConfigInfo
    {
        public int SystemType { set; get; }//系统选择
        public int HorizontalType { set; get; }//水平同异程
        public int PipeSystemType { set; get; }//水系统管制
        public bool IsCodeAndHotPipe { set; get; }//空调冷热水管
        public bool IsCWPipe { set; get; }//冷凝水管
        public bool IsCoolPipe { set; get; }//空调冷却水管
        public bool IsGenerValve { set; get; }//是否穿框线处生成阀门
        public string FrictionCoeff { set; get; }//空调水比摩阻
        public double MarkHeigth { set; get; }//中心标高
        public ThWaterSystemConfigInfo()
        {
            SystemType = 0;
            HorizontalType = 0;
            PipeSystemType = 0;
            IsCodeAndHotPipe = true;
            IsCWPipe = true;
            IsCoolPipe = true;
            IsGenerValve = true;
            FrictionCoeff = "150";
            MarkHeigth = 3.0;
        }
    }
    public class ThWaterValveConfigInfo
    {
        public string FeedPipeValve { set; get; }//给水管阀门
        public string ReturnPipeValeve { set; get; }//回水管阀门
        public string MapScale { set; get; }//出图比例
        public ThWaterValveConfigInfo()
        {
            FeedPipeValve = "截止阀";
            ReturnPipeValeve = "截止阀";
            MapScale = "1:100";
        }
    }

    public class ThWaterPipeConfigInfo
    {
        public ThWaterSystemConfigInfo WaterSystemConfigInfo { set; get; }//系统参数信息
        public ThWaterValveConfigInfo WaterValveConfigInfo { set; get; }//阀门配置信息
        public ThWaterPipeConfigInfo()
        {
            WaterSystemConfigInfo = new ThWaterSystemConfigInfo();
            WaterValveConfigInfo = new ThWaterValveConfigInfo();
        }
    }

}
