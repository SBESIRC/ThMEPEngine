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
            PlaceModel = new IndoorFanPlaceModel();
            PlaceModel.HisVentCount = 1;
        }
        public static IndoorFanParameter Instance = new IndoorFanParameter();
        public IndoorFanLayoutModel LayoutModel { get; set; }
        public IndoorFanLayoutModel ChangeLayoutModel { get; set; }
        public IndoorFanPlaceModel PlaceModel { get; set; }
    }
}
