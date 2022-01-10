using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.IndoorFanModels
{
    public class IndoorFanPlaceModel
    {
        public IndoorFanPlaceModel()
        {
            HisVentCount = 1;
        }
        public int HisVentCount { get; set; }
        public IndoorFanBase TargetFanInfo { get; set; }
        public IndoorFanLayoutModel LayoutModel { get; set; }
    }
}
