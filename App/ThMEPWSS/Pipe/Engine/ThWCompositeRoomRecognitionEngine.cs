using System;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Geom;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Service;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWCompositeRoomRecognitionEngine : ThWRoomRecognitionEngine, IDisposable
    {
        public List<ThWCompositeRoom> Rooms { get; set; }
        public List<ThWCompositeBalconyRoom> FloorDrainRooms { get; set; }
        public List<ThWRainPipe> rainPipes { get; set; }
        public List<ThWRoofRainPipe> roofRainPipes { get; set; }
        public List<ThWCondensePipe> condensePipes { get; set; }
        public List<ThWWashingMachine> washmachines { get; set; }
        public List<ThWBasin> basinTools { get; set; }
        public List<ThWFloorDrain> floorDrains { get; set; }
        public List<ThWClosestool> closets { get; set; }

        public ThWCompositeRoomRecognitionEngine()
        {
            Rooms = new List<ThWCompositeRoom>();
            FloorDrainRooms= new List<ThWCompositeBalconyRoom>();
        }

        public void Dispose()
        {
            //
        }

        public override void Recognize(Database database, Point3dCollection pts)
        {
            var kitchenRooms = ThKitchenRoomService.Build(Spaces, basinTools, 
                rainPipes, roofRainPipes, condensePipes, floorDrains);

            var toiletRooms = ThToiletRoomService.Build(Spaces, closets, 
                floorDrains, condensePipes, roofRainPipes);

            GeneratePairInfo(kitchenRooms, toiletRooms);

            var balconyRooms = ThBalconyRoomService.Build(Spaces, washmachines, 
                floorDrains, rainPipes, basinTools);

            var devicePlatforms = ThDevicePlatformRoomService.Build(Spaces, 
                floorDrains, rainPipes, condensePipes, roofRainPipes);

            GenerateBalconyPairInfo(balconyRooms, devicePlatforms);
        } 
        /// <summary>
        /// 根据厨房间， 卫生间 生成复合房间信息
        /// </summary>
        /// <param name="kitechenRooms"></param>
        /// <param name="toiletRooms"></param>
        private void GeneratePairInfo(List<ThWKitchenRoom> kitchenRooms, List<ThWToiletRoom> toiletRooms)
        {
            var toilets = new List<ThWToiletRoom>();
            foreach (var kitchen in kitchenRooms)
            {
                if (toiletRooms.Count > 0)
                {
                    int s = 0;
                    foreach (var toilet in toiletRooms)
                    {
                        if (IsPair(kitchen, toilet))
                        {
                            Rooms.Add(new ThWCompositeRoom(kitchen, toilet));
                            s = 1;
                            toilets.Add(toilet);
                            break;
                        }
                       
                    }
                    if (s == 0)
                    {
                        foreach (var toilet in toiletRooms)
                        {
                            if (IsDistancedPair(kitchen, toilet))
                            {
                                Rooms.Add(new ThWCompositeRoom(kitchen, toilet));
                                toilets.Add(toilet);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    Rooms.Add(new ThWCompositeRoom(kitchen, new ThWToiletRoom()));
                }
            }
            foreach (var toilet in toiletRooms)
            {
                int s = 0;
                
                foreach (var kitchen in toilets)
                {
                    if (IsReversePair(kitchen, toilet))
                    {
                        s = 1;
                        break;
                    }               
                }
                if(s==0)
                {                        
                        Rooms.Add(new ThWCompositeRoom(new ThWKitchenRoom(), toilet));                                   
                }
            }
        }
        private bool IsPair(ThWKitchenRoom kitchen, ThWToiletRoom toilet)
        {
            var toiletboundary = toilet.Space.Boundary as Polyline;
            var kitchenboundary = kitchen.Space.Boundary as Polyline;
            double distance = kitchenboundary.GetCenter().DistanceTo(toiletboundary.GetCenter());
            return distance < ThWPipeCommon.MAX_TOILET_TO_KITCHEN_DISTANCE;
        }
        private bool IsReversePair(ThWToiletRoom kitchen, ThWToiletRoom toilet)
        {
            var toiletboundary = toilet.Space.Boundary as Polyline;
            var kitchenboundary = kitchen.Space.Boundary as Polyline;
            double distance = kitchenboundary.GetCenter().DistanceTo(toiletboundary.GetCenter());
            return distance < 1;
        }
        private bool IsDistancedPair(ThWKitchenRoom kitchen, ThWToiletRoom toilet)
        {
            var toiletboundary = toilet.Space.Boundary as Polyline;
            var kitchenboundary = kitchen.Space.Boundary as Polyline;
            double distance = kitchenboundary.GetCenter().DistanceTo(toiletboundary.GetCenter());
            return distance < ThWPipeCommon.MAX_TOILET_TO_KITCHEN_DISTANCE1;
        }
        /// <summary>
        /// 根据生活阳台， 设备平台 生成包含阳台地漏房间信息
        /// </summary>
        /// <param name="kitechenRooms"></param>
        /// <param name="toiletRooms"></param>
        private void GenerateBalconyPairInfo(List<ThWBalconyRoom> balconyRooms, List<ThWDevicePlatformRoom> devicePlatformRooms)
        {
            var balconyroom1 = new List<ThWBalconyRoom>();
            for (int i=0;i<balconyRooms.Count;i++)
            {   
                if (balconyRooms[i].FloorDrains.Count > 1 && balconyRooms[i].Washmachines.Count>0)
                {
                    var devicePlatformIncludingRoom = new List<ThWDevicePlatformRoom>();
                    if (devicePlatformRooms.Count > 0)
                    {
                        foreach (var devicePlatformRoom in devicePlatformRooms)
                        {

                            if (devicePlatformRoom!=null&&IsBalconyPair(balconyRooms[i], devicePlatformRoom))
                            {
                                devicePlatformIncludingRoom.Add(devicePlatformRoom);
                            }
                        }
                        FloorDrainRooms.Add(new ThWCompositeBalconyRoom(balconyRooms[i], devicePlatformIncludingRoom));
                    }
                    else
                    {
                        FloorDrainRooms.Add(new ThWCompositeBalconyRoom(balconyRooms[i], devicePlatformIncludingRoom));
                    }

                } 
                else if(balconyRooms[i].FloorDrains.Count==1)
                {
                    var devicePlatformIncludingRoom = new List<ThWDevicePlatformRoom>();
                    if (devicePlatformRooms.Count > 0)
                    {
                        foreach (var devicePlatformRoom in devicePlatformRooms)
                        {

                            if (devicePlatformRoom != null && IsBalconyPair(balconyRooms[i], devicePlatformRoom))
                            {
                                devicePlatformIncludingRoom.Add(devicePlatformRoom);
                            }
                        }
                        FloorDrainRooms.Add(new ThWCompositeBalconyRoom(balconyRooms[i], devicePlatformIncludingRoom));
                    }
                    else
                    {
                        FloorDrainRooms.Add(new ThWCompositeBalconyRoom(balconyRooms[i], devicePlatformIncludingRoom));
                    }
                }

                else
                {

                    balconyroom1.Add(balconyRooms[i]);
                }
            }
            for (int i = 0; i < balconyroom1.Count; i++)
            {
                int num = 0;
                for (int j = i + 1; j < balconyroom1.Count; j++)
                {
                    
                    var s = balconyroom1[i].Space.Boundary as Polyline;
                    var s1 = balconyroom1[j].Space.Boundary as Polyline;
                    if (s.GetCenter().DistanceTo(s1.GetCenter()) < ThWPipeCommon.MAX_BALCONY_TO_BALCONY_DISTANCE)
                    {
                        num = 1;
                        var newBalcony = CreateNewBalcony(balconyroom1[i], balconyroom1[j]);
                        var devicePlatformIncludingRoom = new List<ThWDevicePlatformRoom>();
                        foreach (var devicePlatformRoom in devicePlatformRooms)
                        {

                            if (IsBalconyPair(newBalcony, devicePlatformRoom))
                            {
                                devicePlatformIncludingRoom.Add(devicePlatformRoom);
                            }
                        }
                        FloorDrainRooms.Add(new ThWCompositeBalconyRoom(newBalcony, devicePlatformIncludingRoom));
                    }
                }
                if(num==0)
                {
                    var devicePlatformIncludingRoom = new List<ThWDevicePlatformRoom>();
                    foreach (var devicePlatformRoom in devicePlatformRooms)
                    {

                        if (IsBalconyPair(balconyroom1[i], devicePlatformRoom))
                        {
                            devicePlatformIncludingRoom.Add(devicePlatformRoom);
                        }
                    }
                    FloorDrainRooms.Add(new ThWCompositeBalconyRoom(balconyroom1[i], GetDeviceRoom(devicePlatformIncludingRoom, balconyroom1[i])));
                }
            }
        }
        private bool IsBalconyPair(ThWBalconyRoom balconyRoom, ThWDevicePlatformRoom devicePlatformRoom)
        {
            var balconyRoomboundary = balconyRoom.Space.Boundary as Polyline;
            var devicePlatformRoomboundary = devicePlatformRoom.Space.Boundary as Polyline;
            var distance = (devicePlatformRoomboundary.GetCenter().DistanceTo(balconyRoomboundary.GetCenter()));
            if (distance<ThWPipeCommon.MAX_BALCONY_TO_DEVICEPLATFORM_DISTANCE&&(Math.Abs(devicePlatformRoomboundary.GetCenter().Y- balconyRoomboundary.GetCenter().Y)<2000))
            {
                return true;
            }      
            return false;
        }
        private static ThWBalconyRoom CreateNewBalcony(ThWBalconyRoom balcony1, ThWBalconyRoom balcony2)
        {
            var balcony = new ThWBalconyRoom();
             foreach (var floordrain in balcony1.FloorDrains)
            {
                balcony.FloorDrains.Add(floordrain);
            }
            foreach (var floordrain in balcony2.FloorDrains)
            {
                balcony.FloorDrains.Add(floordrain);
            }
            balcony.RainPipes = balcony1.RainPipes.Count>0? balcony1.RainPipes:balcony2.RainPipes;
            balcony.Washmachines = balcony1.Washmachines.Count > 0 ? balcony1.Washmachines : balcony2.Washmachines;
            balcony.BasinTools = balcony1.BasinTools.Count > 0 ? balcony1.BasinTools : balcony2.BasinTools;
            balcony.Space = new ThMEPEngineCore.Model.ThIfcSpace();
            balcony.Space.Boundary = CreateNewBoundary(balcony1, balcony2);
            return balcony;
        }
        private static Polyline CreateNewBoundary(ThWBalconyRoom balcony1, ThWBalconyRoom balcony2)
        {
            var boundary1 = balcony1.Space.Boundary as Polyline;
            var boundary2 = balcony2.Space.Boundary as Polyline;
            var vertice1 = boundary1.Vertices();
            var vertice2 = boundary2.Vertices();
            var vertices= new List<Point3d>();
            for (int i = 0; i < vertice1.Count; i++)
            {
                vertices.Add(vertice1[i]);
            }

            for (int i = 0; i < vertice2.Count; i++)
            {
                vertices.Add(vertice2[i]);
            }

            return GeomUtils.CalculateProfile(vertices);        
        }
        private static List<ThWDevicePlatformRoom> GetDeviceRoom(List<ThWDevicePlatformRoom> deviceRoom,ThWBalconyRoom balconyRoom)
        {
            var devicerooms = new List<ThWDevicePlatformRoom>();
            var deviceLeftRooms = new List<ThWDevicePlatformRoom>();
            var deviceRightRooms = new List<ThWDevicePlatformRoom>();
            if (deviceRoom.Count==1)
            {
                devicerooms.Add(deviceRoom[0]);
            }
            else if(deviceRoom.Count>1)
            {
                foreach(var room in deviceRoom)
                {
                    var boundary = room.Space.Boundary as Polyline;
                    var boundary1 = balconyRoom.Space.Boundary as Polyline;
                    if (boundary.GetCenter().X< boundary1.GetCenter().X)
                    {
                        deviceLeftRooms.Add(room);
                    }
                    else
                    {
                        deviceRightRooms.Add(room);
                    }
                }
            }
            devicerooms.Add(GetTrueRoom(deviceLeftRooms, balconyRoom));
            devicerooms.Add(GetTrueRoom(deviceRightRooms, balconyRoom));
            return devicerooms;
        }
        private static ThWDevicePlatformRoom GetTrueRoom(List<ThWDevicePlatformRoom> deviceRoom, ThWBalconyRoom balconyRoom)
        {
            double dis = double.MaxValue;
            ThWDevicePlatformRoom trueRoom = null;
            for(int i=0;i< deviceRoom.Count;i++)
            {
                var boundary = deviceRoom[i].Space.Boundary as Polyline;
                var boundary1 = balconyRoom.Space.Boundary as Polyline;
                if (boundary.GetCenter().DistanceTo(boundary1.GetCenter())< dis)
                {
                    dis = boundary.GetCenter().DistanceTo(boundary1.GetCenter());
                    trueRoom = deviceRoom[i];
                }
            }
            return trueRoom;
        }
    }
}
