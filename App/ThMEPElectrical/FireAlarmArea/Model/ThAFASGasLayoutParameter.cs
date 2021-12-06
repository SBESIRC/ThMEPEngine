using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.FireAlarmArea.Model
{
    class ThAFASGasLayoutParameter
    {
        public double Scale { get; set; } = 100;
        public double ProtectRadius { get; set; } = 8000;
        public double AisleAreaThreshold { get; set; } = 0.025;
        public string BlkNameGas = "";
        public string BlkNameGasPrf = "";

        public Dictionary<Polyline, ThFaSmokeCommon.layoutType> RoomType { get; set; } = new Dictionary<Polyline, ThFaSmokeCommon.layoutType>();
    }
}
