using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.IndoorFanLayout.Models
{
    class AreaRegionType
    {
        public string RegionType { get; set; }//区域名称（大区域、中区域、小区域）
        public double MinSideLength { get; set; }//该区域短边最小长度
        public double MaxSideLength { get; set; }//该区域长边最大长度
    }
}
