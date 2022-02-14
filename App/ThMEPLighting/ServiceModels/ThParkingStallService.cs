using System.Collections.Generic;
using ThMEPLighting.Common;
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
            this.ParkingBlockNames = new List<string>();
            this.ParkingSource = EnumParkingSource.OnlyLayerName;
        }
        public static ThParkingStallService Instance = new ThParkingStallService();
        public List<string> ParkingLayerNames { get; }
        public List<string> ParkingBlockNames { get; }
        public Light_Place_Type LightDirection { get; private set; }
        public EnumParkingSource ParkingSource { get; set; }
        public int GroupMaxLightCount { get; set; }
        public double BlockScale { get; set; }
        public Parkingillumination ParkingStallIllumination { get; set; }
        public void SetLightDir(bool isLongSide) 
        {
            //这里灯方向是沿着长边或短边
            this.LightDirection = isLongSide ? Light_Place_Type.LONG_EDGE : Light_Place_Type.SHORT_EDGE;
        }
    }
}
