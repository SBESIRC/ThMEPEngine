﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FanConnect.Model
{
    public class ThFanCUModel
    {
        public bool IsConnected { set; get; }
        public string FanType { set; get; }//类型
        public double CoolCapa { set; get; }//制冷量
        public double CoolFlow { set; get; }//制冷流量
        public double HotFlow { set; get; }//制热流量
        public Point3d FanPoint { set; get; }//连接点
        public Polyline FanObb { set; get; }//设备外包框
        public BlockReference FanData { set; get; }//设备数据
        public ThFanCUModel()
        {
            IsConnected = false;
        }
    }
}
