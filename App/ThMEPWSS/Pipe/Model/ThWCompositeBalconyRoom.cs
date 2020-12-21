using System.Collections.Generic;

namespace ThMEPWSS.Pipe.Model
{
   public class ThWCompositeBalconyRoom : ThWRoom
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
