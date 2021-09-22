using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ServiceModels
{
    public class ThParkingStallService
    {
        ThParkingStallService() 
        {
            this.BlockScale = 100;
            this.GroupMaxLightCount = 25;
            this.LightDirection = Light_Place_Type.LONG_EDGE;
            this.ParkingLayerNames = new List<string>();
        }
        public static ThParkingStallService Instance = new ThParkingStallService();
        public List<string> ParkingLayerNames { get; }
        public Light_Place_Type LightDirection { get; private set; }
        public int GroupMaxLightCount { get; set; }
        public double BlockScale { get; set; }
        public void SetLightDir(bool isLongSide) 
        {
            this.LightDirection = isLongSide ? Light_Place_Type.LONG_EDGE : Light_Place_Type.SHORT_EDGE;
        }
    }
}
