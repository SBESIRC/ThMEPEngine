using System.Collections.Generic;
namespace ThMEPWSS.Pipe.Model
{
   public class ThWCompositeBalconyRoom : ThWRoom
    {
        public ThWBalconyRoom Balcony { get; set; }
        public ThWDevicePlatformRoom DevicePlatform { get; set; }
        public List<ThWDevicePlatformRoom> DevicePlatforms { get; set; }

        public ThWCompositeBalconyRoom(ThWBalconyRoom balconyRoom, List<ThWDevicePlatformRoom> devicePlatformRooms)
        {
            Balcony = balconyRoom;        
            DevicePlatforms = devicePlatformRooms;
        }
    }
}
