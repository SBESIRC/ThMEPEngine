using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ParkingStall.Model
{
    public class LightPlaceOneSide
    {
        public List<LightPlaceInfo> LightPlaceInfos;
        public ParkingSpace_Type ParkType;

        public LightPlaceOneSide(List<LightPlaceInfo> lightPlaceInfos, ParkingSpace_Type parkingSpace_Type)
        {
            LightPlaceInfos = lightPlaceInfos;
            ParkType = parkingSpace_Type;
        }
    }
}
