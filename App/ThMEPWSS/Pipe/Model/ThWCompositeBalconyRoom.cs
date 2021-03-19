using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Model
{
   public class ThWCompositeBalconyRoom : ThIfcRoom
    {
        public ThWBalconyRoom Balcony { get; set; }
        public List<ThWDevicePlatformRoom> DevicePlatforms { get; set; }
        public ThWCompositeBalconyRoom(ThWBalconyRoom balcony, List<ThWDevicePlatformRoom> devicePlatforms)
        {
            Balcony = balcony;        
            DevicePlatforms = devicePlatforms;
        }
    }
}
