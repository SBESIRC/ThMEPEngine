using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Geom;
using ThMEPWSS.Pipe.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThCADCore.NTS;
using NFox.Cad;
using ThMEPEngineCore.Model;
using System;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWCompositeExtractionEngine : ThDistributionElementExtractionEngine
    {
        public ThWRainPipeExtractionVisitor rainPipes_visitor { get; set; }
        public ThWRoofRainPipeExtractionVisitor roofRainPipes_visitor { get; set; }
        public ThWFloorDrainExtractionVisitor floorDrains_visitor { get; set; }
        public ThWCondensePipeExtractionVisitor condensePipes_visitor { get; set; }
        public ThWWashMachineExtractionVisitor washmachines_visitor { get; set; }
        public ThWBasinExtractionVisitor basinTools_visitor { get; set; }
        public ThWBlockReferenceVisitor common_visitor { get; set; }
        public override void Extract(Database database)
        {
            common_visitor = new ThWBlockReferenceVisitor();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(common_visitor);
            extractor.Extract(database);
        }
    }

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
                var rainPipesEngine = new ThWCompositeRecognitionEngine();
                rainPipesEngine.Recognize(database, pts);
                var rainPipes = rainPipesEngine.Elements.Where(o => o is ThWRainPipe).Cast<ThWRainPipe>().ToList();
                var roofRainPipes = rainPipesEngine.Elements.Where(o => o is ThWRoofRainPipe).Cast<ThWRoofRainPipe>().ToList();
                var condensePipes = rainPipesEngine.Elements.Where(o => o is ThWCondensePipe).Cast<ThWCondensePipe>().ToList();
                var washmachines = rainPipesEngine.Elements.Where(o => o is ThWWashingMachine).Cast<ThWWashingMachine>().ToList();
                var basinTools = rainPipesEngine.Elements.Where(o => o is ThWBasin).Cast<ThWBasin>().ToList();
                var floorDrains= rainPipesEngine.Elements.Where(o => o is ThWFloorDrain).Cast<ThWFloorDrain>().ToList();
                var closets = rainPipesEngine.Elements.Where(o => o is ThWClosestool).Cast<ThWClosestool>().ToList();
                var kichenEngine = new ThWKitchenRoomRecognitionEngine()
                {
                    Spaces = Spaces,
                    RainPipes = rainPipes,
                    RoofRainPipes= roofRainPipes,
                    BasinTools = basinTools
                };
                kichenEngine.Recognize(database, pts);
                //var floorDrains = GetFloorDrains(database, pts);
                var toiletEngine = new ThWToiletRoomRecognitionEngine()
                {                 
                    Spaces = kichenEngine.Spaces,
                    FloorDrains= floorDrains,
                    CondensePipes= condensePipes,
                    RoofRainPipes = roofRainPipes,
                    closestools=closets
                };
                toiletEngine.Recognize(database, pts);
                GeneratePairInfo(kichenEngine.Rooms, toiletEngine.Rooms);
                var balconyEngine = new ThWBalconyRoomRecognitionEngine()
                {
                    Spaces = toiletEngine.Spaces,
                    FloorDrains = floorDrains,
                    Washmachines = washmachines,
                    RainPipes = rainPipes,
                    BasinTools = basinTools
                };
                balconyEngine.Recognize(database, pts);
                var devicePlatformEngine = new ThWDevicePlatformRoomRecognitionEngine()
                {
                    Spaces = balconyEngine.Spaces,
                    FloorDrains = floorDrains,
                    RainPipes = rainPipes,
                    CondensePipes = condensePipes,
                    RoofRainPipes = roofRainPipes
                };
                devicePlatformEngine.Recognize(database, pts);
                GenerateBalconyPairInfo(balconyEngine.Rooms, devicePlatformEngine.Rooms);
            }
        }
        public class ThWCompositeRecognitionEngine : ThDistributionElementRecognitionEngine
        {
            public override void Recognize(Database database, Point3dCollection polygon)
            {
                using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
                {
                    var engine = new ThWCompositeExtractionEngine();
                    engine.Extract(database);

                    var dbObjs = engine.common_visitor.Results.Select(o => o.Geometry).ToCollection();
                    if (polygon.Count > 0)
                    {
                        ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                        dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                    }
                    var results = engine.common_visitor.Results.Where(o => dbObjs.Contains(o.Geometry));
                    results.Where(o => ThRainPipeLayerManager.IsRainPipeBlockName(o.Data as string))
                        .ForEach(o => Elements.Add(ThWRainPipe.Create(o.Geometry.GeometricExtents.ToRectangle())));
                    results.Where(o => ThRoofRainPipeLayerManager.IsRoofPipeBlockName(o.Data as string))
                        .ForEach(o => Elements.Add(ThWRoofRainPipe.Create(o.Geometry.GeometricExtents.ToRectangle())));
                    results.Where(o => ThCondensePipeLayerManager.IsCondensePipeBlockName(o.Data as string))
                        .ForEach(o => Elements.Add(ThWCondensePipe.Create(o.Geometry.GeometricExtents.ToRectangle())));
                    results.Where(o => ThWashMachineLayerManager.IsWashmachineBlockName(o.Data as string))
                        .ForEach(o => Elements.Add(ThWWashingMachine.Create(o.Geometry)));
                    results.Where(o => ThBasintoolLayerManager.IsBasintoolBlockName(o.Data as string))
                        .ForEach(o => Elements.Add(ThWBasin.Create(o.Geometry)));
                    results.Where(o => ThFloorDrainLayerManager.IsToiletFloorDrainBlockName(o.Data as string))
                        .ForEach(o => Elements.Add(ThWFloorDrain.Create(o.Geometry)));
                    results.Where(o => ThFloorDrainLayerManager.IsBalconyFloorDrainBlockName(o.Data as string))
                       .ForEach(o => Elements.Add(ThWFloorDrain.Create(o.Geometry)));
                    results.Where(o => ThClosestoolLayerManager.IsClosetoolBlockName(o.Data as string))
                     .ForEach(o => Elements.Add(ThWClosestool.Create(o.Geometry.GeometricExtents.ToRectangle())));
                }                               
            }
        }
        protected List<ThWRainPipe> GetRainPipes(Database database, Point3dCollection pts)
        {
            using (var rainPipesEngine = new ThWCompositeRecognitionEngine())
            {
                rainPipesEngine.Recognize(database, pts);
                return rainPipesEngine.Elements.Cast<ThWRainPipe>().ToList();
            }
        }
        protected List<ThWRoofRainPipe> GetRoofRainPipes(Database database, Point3dCollection pts)
        {
            using (ThWRoofRainPipeRecognitionEngine roofRainPipesEngine = new ThWRoofRainPipeRecognitionEngine())
            {
                roofRainPipesEngine.Recognize(database, pts);
                return roofRainPipesEngine.Elements.Cast<ThWRoofRainPipe>().ToList();
            }
        }
        protected List<ThWFloorDrain> GetFloorDrains(Database database, Point3dCollection pts)
        {
            using (ThWFloorDrainRecognitionEngine floorDrainEngine = new ThWFloorDrainRecognitionEngine())
            {
                floorDrainEngine.Recognize(database, pts);
                return floorDrainEngine.Elements.Cast<ThWFloorDrain>().ToList();
            }
        }
        protected List<ThWCondensePipe> GetCondensePipes(Database database, Point3dCollection pts)
        {
            using (ThWCondensePipeRecognitionEngine condensePipesEngine = new ThWCondensePipeRecognitionEngine())
            {
                condensePipesEngine.Recognize(database, pts);
                return condensePipesEngine.Elements.Cast<ThWCondensePipe>().ToList();
            }
        }
        private List<ThWWashingMachine> GetWashmachines(Database database, Point3dCollection pts)
        {
            using (ThWWashMachineRecognitionEngine washmachinesEngine = new ThWWashMachineRecognitionEngine())
            {
                washmachinesEngine.Recognize(database, pts);
                return washmachinesEngine.Elements.Cast<ThWWashingMachine>().ToList();
            }
        }
        private List<ThWBasin> GetBasinTools(Database database, Point3dCollection pts)
        {
            using (ThWBasinRecognitionEngine basinToolsEngine = new ThWBasinRecognitionEngine())
            {
                basinToolsEngine.Recognize(database, pts);
                return basinToolsEngine.Elements.Cast<ThWBasin>().ToList();
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
                if (toiletRooms.Count > 0)
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
                else
                {
                    Rooms.Add(new ThWCompositeRoom(kitchen, new ThWToiletRoom()));
                }
            }
            foreach (var toilet in toiletRooms)
            {
                int s = 0;
                foreach (var kitchen in kitchenRooms)
                {
                    if (IsPair(kitchen, toilet))
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
                    foreach (var devicePlatformRoom in devicePlatformRooms)
                    {

                        if (IsBalconyPair(balconyRooms[i], devicePlatformRoom))
                        {
                            devicePlatformIncludingRoom.Add(devicePlatformRoom);
                        }
                    }
                    FloorDrainRooms.Add(new ThWCompositeBalconyRoom(balconyRooms[i], devicePlatformIncludingRoom));
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
