using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPElectrical.FireAlarmSmokeHeat.Model
{
    public class ThFaAreaLayoutParameter
    {
        public int FloorHightIdx { get; set; } = -1;
        public int RootThetaIdx { get; set; } = -1;
        public double Scale { get; set; } = 100;
        public double AisleAreaThreshold { get; set; } = 0.025;

        public string BlkNameHeat = "";
        //public string BlkNameSmoke = ""; //heat块用作计算避让框，smoke暂时没必要

        public List<Point3d> stairPartResult { get; set; } = new List<Point3d>();
        public Dictionary<Polyline, ThFaSmokeCommon.layoutType> RoomType { get; set; } = new Dictionary<Polyline, ThFaSmokeCommon.layoutType>();
    }
}
