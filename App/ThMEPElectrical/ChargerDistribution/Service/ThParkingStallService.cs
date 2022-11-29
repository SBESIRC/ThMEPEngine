using System.Collections.Generic;

namespace ThMEPElectrical.ChargerDistribution.Service
{
    public class ThParkingStallService
    {
        public ThParkingStallService()
        {
            this.ParkingLayerNames = new List<string>();
            this.ParkingBlockNames = new List<string>();
        }
        public static ThParkingStallService Instance = new ThParkingStallService();
        public List<string> ParkingLayerNames { get; }
        public List<string> ParkingBlockNames { get; }
    }
}
