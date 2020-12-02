using Linq2Acad;
using ThMEPWSS.Pipe.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;


namespace ThMEPWSS.Pipe.Engine
{
    public class ThWCompositeRoomRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWCompositeRoom> Rooms { get; set; }

        public List<ThWCompositeBalconyRoom> FloorDrainRooms { get; set; }
        public ThWCompositeRoomRecognitionEngine()
        {
            Rooms = new List<ThWCompositeRoom>();
            FloorDrainRooms= new List<ThWCompositeBalconyRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var kichenEngine = new ThWKitchenRoomRecognitionEngine();
                kichenEngine.Recognize(database, pts);
                var toiletEngine = new ThWToiletRoomRecognitionEngine()
                {
                    Spaces = kichenEngine.Spaces
                };
                toiletEngine.Recognize(database, pts);
                GeneratePairInfo(kichenEngine.Rooms, toiletEngine.Rooms);
                var balconyEngine = new ThWBalconyRoomRecognitionEngine()
                {
                    Spaces = toiletEngine.Spaces
                };
                balconyEngine.Recognize(database, pts);
                var devicePlatformEngine = new ThWDevicePlatformRoomRecognitionEngine()
                {
                    Spaces = balconyEngine.Spaces
                };
                devicePlatformEngine.Recognize(database, pts);
                GenerateBalconyPairInfo(balconyEngine.Rooms, devicePlatformEngine.Rooms);
            }
        }

        /// <summary>
        /// 根据厨房间， 卫生间 生成复合房间信息
        /// </summary>
        /// <param name="kitechenRooms"></param>
        /// <param name="toiletRooms"></param>
        private void GeneratePairInfo(List<ThWKitchenRoom> kitchenRooms, List<ThWToiletRoom> toiletRooms)
        {
            foreach (var kitchen in kitchenRooms)
            {
                foreach (var toilet in toiletRooms)
                {
                    if (IsPair(kitchen, toilet))
                    {
                        Rooms.Add(new ThWCompositeRoom(kitchen, toilet));
                        break;
                    }
                }
            }
        }

        private bool IsPair(ThWKitchenRoom kitchen, ThWToiletRoom toilet)
        {
            var toiletboundary = toilet.Toilet.Boundary as Polyline;
            var kitchenboundary = kitchen.Kitchen.Boundary as Polyline;
            double distance = kitchenboundary.GetCenter().DistanceTo(toiletboundary.GetCenter());
            return distance < ThWPipeCommon.MAX_TOILET_TO_KITCHEN_DISTANCE;
        }
        /// <summary>
        /// 根据生活阳台， 设备平台 生成包含阳台地漏房间信息
        /// </summary>
        /// <param name="kitechenRooms"></param>
        /// <param name="toiletRooms"></param>
        private void GenerateBalconyPairInfo(List<ThWBalconyRoom> balconyRooms, List<ThWDevicePlatformRoom> devicePlatformRooms)
        {
         
            foreach (var balconyRoom in balconyRooms)
            {
                var devicePlatformIncludingRoom = new List<ThWDevicePlatformRoom>();
                foreach (var devicePlatformRoom in devicePlatformRooms)
                {

                        if (IsBalconyPair(balconyRoom, devicePlatformRoom))
                        {
                        devicePlatformIncludingRoom.Add(devicePlatformRoom);
                        }
                }
                FloorDrainRooms.Add(new ThWCompositeBalconyRoom(balconyRoom, devicePlatformIncludingRoom));

            }
        }

        private bool IsBalconyPair(ThWBalconyRoom balconyRoom, ThWDevicePlatformRoom devicePlatformRoom)
        {
            var balconyRoomboundary = balconyRoom.Balcony.Boundary as Polyline;       
            foreach (var deviceplatform in devicePlatformRoom.DevicePlatform)
            {
                var devicePlatformRoomboundary = deviceplatform.Boundary as Polyline;

                var distance=(devicePlatformRoomboundary.GetCenter().DistanceTo(balconyRoomboundary.GetCenter()));
                if(distance>=ThWPipeCommon.MAX_BALCONY_TO_DEVICEPLATFORM_DISTANCE)
                {
                    return false;
                }
            }        
            return true;
        }
    }
}
