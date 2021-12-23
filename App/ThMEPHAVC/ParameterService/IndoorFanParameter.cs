using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.IndoorFanModels;

namespace ThMEPHVAC.ParameterService
{
    public class IndoorFanParameter
    {
        IndoorFanParameter()
        {
        }
        public static IndoorFanParameter Instance = new IndoorFanParameter();
        public IndoorFanLayoutModel LayoutModel { get; set; }
        public IndoorFanPlaceModel PlaceModel { get; set; }
    }
}
