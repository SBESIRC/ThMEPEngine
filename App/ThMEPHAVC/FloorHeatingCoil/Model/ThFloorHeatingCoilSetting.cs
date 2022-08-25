using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.FloorHeatingCoil.Model
{
    public class ThFloorHeatingCoilSetting
    {
        public Dictionary<string, List<string>> BlockNameDict { get; set; } = new Dictionary<string, List<string>>();
        public bool WithUI = false;
        //public List<Polyline> SelectFrame { get; set; } = new List<Polyline>();



        public static ThFloorHeatingCoilSetting Instance = new ThFloorHeatingCoilSetting();
        public ThFloorHeatingCoilSetting()
        {
         
        }
    }
}
