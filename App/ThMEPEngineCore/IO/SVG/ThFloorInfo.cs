using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.IO.SVG
{
    public class ThFloorInfo
    {
        public string FloorName { get; set; } = "";
        public string FloorNo { get; set; } = "";
        public string StdFlrNo { get; set; } = "";
        public string Bottom_elevation { get; set; } = "";
        public string Height { get; set; } = "";
        public string Description { get; set; } = "";
        public double BottomElevation
        {
            get
            {
                return Get_Bottom_elevation(Bottom_elevation);
            }
        }
        private double Get_Bottom_elevation(string bottom_elevation)
        {
            double bottomElevation = 0.0;
            double.TryParse(bottom_elevation, out bottomElevation);
            return bottomElevation;
        }
    }
}
