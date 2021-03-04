using ThMEPEngineCore.Model;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Tools;
using ThMEPEngineCore.Engine;
using NFox.Cad;
using ThCADCore.NTS;
using ThMEPWSS.Pipe.Service;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThMEPEngineCore.Service;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWCompositeExtractionEngine : ThDistributionElementExtractionEngine
    {
        public ThWBlockReferenceVisitor Visitor { get; set; }
        public override void Extract(Database database)
        {
            Visitor = new ThWBlockReferenceVisitor();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(Visitor);
            extractor.Extract(database);
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
                Point3dCollection pts = new Point3dCollection();
                Point3dCollection pts1 = new Point3dCollection();
                for (int i=0;i< polygon.Count-1;i++)
                {
                    if(i<4)
                    {
                        pts1.Add(polygon[i]);
                    }
                    else
                    {
                        pts.Add(polygon[i]);
                    }
                }

                var dbObjs = engine.Visitor.Results.Select(o => o.Geometry).ToCollection();
                if (pts.Count > 0)
                {
                    ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    dbObjs = spatialIndex.SelectCrossingPolygon(pts);
                }
                var dbObjs1 = engine.Visitor.Results.Select(o => o.Geometry).ToCollection();
                if (pts1.Count > 0)
                {
                    ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs1);
                    dbObjs1 = spatialIndex.SelectCrossingPolygon(pts1);
                }

                var results = engine.Visitor.Results.Where(o => dbObjs.Contains(o.Geometry));
                results.Where(o => ThRainPipeLayerManager.IsRainPipeBlockName(o.Data as string))
                    .ForEach(o => Elements.Add(ThWRainPipe.Create(o.Geometry.GeometricExtents.ToRectangle())));
                results.Where(o => ThRoofRainPipeLayerManager.IsRoofRainPipeBlockName(o.Data as string))
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
                var results1 = engine.Visitor.Results.Where(o => dbObjs1.Contains(o.Geometry));
                results1.Where(o => ThRoofRainPipeLayerManager.IsRoofRainPipeBlockName(o.Data as string))
                  .ForEach(o => Elements.Add(ThWRoofRainPipe.Create(o.Geometry.GeometricExtents.ToRectangle())));
                results1.Where(o => ThGravityWaterBucketLayerManager.IsGravityWaterBucketBlockName(o.Data as string))
                 .ForEach(o => Elements.Add(ThWGravityWaterBucket.Create(o.Geometry))); 
                    results1.Where(o => ThSideEntryWaterBucketLayerManager.IsSideEntryWaterBucketBlockName(o.Data as string))
                 .ForEach(o => Elements.Add(ThWSideEntryWaterBucket.Create(o.Geometry)));
            }
        }
    }
    public class ThWCompositeFloorRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWRoofDeviceFloorRoom> RoofDeviceFloors { get; set; }
        public List<ThWRoofFloorRoom> RoofFloors { get; set; }
        public List<ThWTopFloorRoom> TopFloors { get; set; }
        public List<ThWTopFloorRoom> NormalFloors { get; set; }
        public List<Curve> TagNameFrames { get; set; }
        public List<Curve> StairFrames { get; set; }
        public List<Curve> Columns { get; set; }
        public List<Curve> ShearWalls { get; set; }
        public List<Curve> InnerDoors { get; set; }
        public List<Curve> Devices { get; set; }
        public List<Curve> ArchitectureWalls { get; set; }
        public List<Curve> Windows { get; set; }
        public List<Curve> ElevationFrames { get; set; }
        public List<Curve> AxialCircleTags { get; set; }
        public List<Curve> AxialAxisTags { get; set; }
        public List<Curve> ExternalTags { get; set; }
        public List<Curve> Wells { get; set; }
        public List<Curve> DimensionTags { get; set; }
        public List<Curve> RainPipes { get; set; }
        public List<Curve> PositionTags { get; set; }
        public List<Curve> AllObstacles { get; set; }
        public List<string> Layers { get; set; }    
        public ThWCompositeFloorRecognitionEngine()
        {
            TagNameFrames = new List<Curve>();
            StairFrames = new List<Curve>();
            Columns = new List<Curve>();
            ShearWalls = new List<Curve>();
            InnerDoors = new List<Curve>();
            Devices = new List<Curve>();
            ArchitectureWalls = new List<Curve>();
            Windows = new List<Curve>();
            ElevationFrames = new List<Curve>();
            AxialCircleTags = new List<Curve>();
            AxialAxisTags = new List<Curve>();
            ExternalTags = new List<Curve>();
            Wells = new List<Curve>();
            DimensionTags = new List<Curve>();
            RainPipes = new List<Curve>();
            PositionTags = new List<Curve>();
            AllObstacles= new List<Curve>();
            Layers = new List<string>();
            RoofDeviceFloors = new List<ThWRoofDeviceFloorRoom>();
            RoofFloors = new List<ThWRoofFloorRoom>();
            TopFloors = new List<ThWTopFloorRoom>();
            NormalFloors= new List<ThWTopFloorRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var BlockCollection = new List<BlockReference>();
                BlockCollection = BlockTools.GetAllDynBlockReferences(database, "楼层框定");
                var deviceSpaces = new List<ThIfcSpace>();
                var roofSpaces = new List<ThIfcSpace>();
                var standardSpaces = new List<ThIfcSpace>();
                var nonStandardSpaces = new List<ThIfcSpace>();
                var base_Circles = new List<ThIfcSpace>();
                if (BlockCollection.Count > 0)
                {
                    deviceSpaces = GetDeviceSpaces(BlockCollection);
                    roofSpaces = GetRoofSpaces(BlockCollection);
                    standardSpaces = GetStandardSpaces(BlockCollection);
                    nonStandardSpaces = GetNonStandardSpaces(BlockCollection);
                    base_Circles = GetBaseCircles(BlockCollection);
                }
                var frameSpaces=new List<ThIfcSpace>();
                if (deviceSpaces.Count > 0)
                {
                    frameSpaces.Add(deviceSpaces[0]);
                }
                if (roofSpaces.Count > 0)
                {
                    frameSpaces.Add(roofSpaces[0]);
                }      
                var rainPipesEngine = new ThWCompositeRecognitionEngine();
                rainPipesEngine.Recognize(database, GetBoundaryVertices(frameSpaces, standardSpaces));
                var rainPipes = rainPipesEngine.Elements.Where(o => o is ThWRainPipe).Cast<ThWRainPipe>().ToList();
                var roofRainPipes = rainPipesEngine.Elements.Where(o => o is ThWRoofRainPipe).Cast<ThWRoofRainPipe>().ToList();
                var condensePipes = rainPipesEngine.Elements.Where(o => o is ThWCondensePipe).Cast<ThWCondensePipe>().ToList();
                var washmachines = rainPipesEngine.Elements.Where(o => o is ThWWashingMachine).Cast<ThWWashingMachine>().ToList();
                var basinTools = rainPipesEngine.Elements.Where(o => o is ThWBasin).Cast<ThWBasin>().ToList();
                var floorDrains = rainPipesEngine.Elements.Where(o => o is ThWFloorDrain).Cast<ThWFloorDrain>().ToList();
                var closets = rainPipesEngine.Elements.Where(o => o is ThWClosestool).Cast<ThWClosestool>().ToList();
                var GravityWaterBuckets = rainPipesEngine.Elements.Where(o => o is ThWGravityWaterBucket).Cast<ThWGravityWaterBucket>().ToList();  
                var SideEntryWaterBuckets = rainPipesEngine.Elements.Where(o => o is ThWSideEntryWaterBucket).Cast<ThWSideEntryWaterBucket>().ToList();
                var RoofDeviceEngine = new ThWRoofDeviceFloorRecognitionEngine()
                {
                    blockCollection = BlockCollection,
                    DeviceSpaces = deviceSpaces,
                    RoofRainPipes = roofRainPipes,
                    GravityWaterBuckets = GravityWaterBuckets,
                    SideEntryWaterBuckets = SideEntryWaterBuckets,
                };               
                RoofDeviceEngine.Recognize(database, pts);             
                RoofDeviceFloors = RoofDeviceEngine.Rooms;
                RoofDeviceEngine.TagNameFrames.ForEach(o => TagNameFrames.Add(o));
                RoofDeviceEngine.StairFrames.ForEach(o => StairFrames.Add(o));
                RoofDeviceEngine.Columns.ForEach(o => Columns.Add(o));
                RoofDeviceEngine.ShearWalls.ForEach(o => ShearWalls.Add(o));
                RoofDeviceEngine.InnerDoors.ForEach(o => InnerDoors.Add(o));
                RoofDeviceEngine.Devices.ForEach(o => Devices.Add(o));
                RoofDeviceEngine.ArchitectureWalls.ForEach(o => ArchitectureWalls.Add(o));
                RoofDeviceEngine.Windows.ForEach(o => Windows.Add(o));
                RoofDeviceEngine.ElevationFrames.ForEach(o => ElevationFrames.Add(o));
                RoofDeviceEngine.AxialCircleTags.ForEach(o => AxialCircleTags.Add(o));
                RoofDeviceEngine.AxialAxisTags.ForEach(o => AxialAxisTags.Add(o));
                RoofDeviceEngine.ExternalTags.ForEach(o => ExternalTags.Add(o));
                RoofDeviceEngine.Wells.ForEach(o => Wells.Add(o));
                RoofDeviceEngine.DimensionTags.ForEach(o => DimensionTags.Add(o));
                RoofDeviceEngine.RainPipes.ForEach(o => RainPipes.Add(o));
                RoofDeviceEngine.PositionTags.ForEach(o => PositionTags.Add(o));
                RoofDeviceEngine.AllObstacles.ForEach(o => AllObstacles.Add(o));
                RoofDeviceEngine.Layers.ForEach(o => Layers.Add(o));
                var RoofEngine = new ThWRoofFloorRecognitionEngine()
                {
                    blockCollection = BlockCollection,
                    RoofSpaces = roofSpaces,
                    gravityWaterBuckets = GravityWaterBuckets,                 
                   sideEntryWaterBuckets= SideEntryWaterBuckets,
                   roofRainPipes= roofRainPipes
                };            
                RoofEngine.Recognize(database, pts);
                RoofFloors = RoofEngine.Rooms;
                if(!(RoofDeviceEngine.Spaces.Count>0))
                {
                    this.Spaces = GetSpaces(database, pts);
                }
                else
                {
                    this.Spaces = RoofDeviceEngine.Spaces;
                }                          
                var FirstEngine = new ThWTopFloorRecognitionEngine()
                {
                    Spaces= this.Spaces,
                    blockCollection = BlockCollection,
                    StandardSpaces= standardSpaces,
                    NonStandardSpaces= nonStandardSpaces,
                    roofRainPipes = roofRainPipes,
                    condensePipes= condensePipes,
                    washmachines= washmachines,
                    basinTools= basinTools,
                    floorDrains= floorDrains,
                    closets= closets,
                    rainPipes= rainPipes
                };      
                FirstEngine.Recognize(database, pts);
                TopFloors = FirstEngine.Rooms;
                if (TopFloors.Count > 0)
                {
                    for (int i = 1;i< TopFloors.Count; i++)
                    {
                        NormalFloors.Add(TopFloors[i]);
                    }
                }
            }
        }   
        public static List<ThIfcSpace> GetBaseCircles(List<BlockReference> blocks)
        {
            var FloorSpaces = new List<ThIfcSpace>();
            foreach (BlockReference block in blocks)
            {
                if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Contains("小屋面"))
                {
                    var s = new DBObjectCollection();
                    block.Explode(s);
                    List<Circle> circle = new List<Circle>();
                    foreach (var s1 in s)
                    {
                        if (s1.GetType().Name.Contains("Circle"))
                        {
                            Circle baseCircle = s1 as Circle;
                            FloorSpaces.Add(new ThIfcSpace { Boundary = baseCircle });
                        }
                    }
                }
            }
            return FloorSpaces;
        }
        public static List<ThIfcSpace> GetDeviceSpaces(List<BlockReference> blocks)
        {
            var FloorSpaces = new List<ThIfcSpace>();
            var blockBounds = new List<BlockReference>();
            foreach (BlockReference block in blocks)
            {
                if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Contains("小屋面"))
                {
                    blockBounds.Add(block);
                }
            }
            GetBoundaryCurves(blockBounds).ForEach(o => FloorSpaces.Add(new ThIfcSpace { Boundary = o }));
            return FloorSpaces;
        }
        public static List<ThIfcSpace> GetRoofSpaces(List<BlockReference> blocks)
        {
            var FloorSpaces = new List<ThIfcSpace>();
            var blockBounds = new List<BlockReference>();
            foreach (BlockReference block in blocks)
            {
                if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Contains("大屋面"))
                {
                    blockBounds.Add(block);
                }
            }
            GetBoundaryCurves(blockBounds).ForEach(o => FloorSpaces.Add(new ThIfcSpace { Boundary = o }));
            return FloorSpaces;
        }
        public static List<ThIfcSpace> GetStandardSpaces(List<BlockReference> blocks)
        {
            var FloorSpaces = new List<ThIfcSpace>();

            foreach (BlockReference block in blocks)
            {
                var blockBounds = new List<BlockReference>();
                var blockString = new List<string>();
                if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Contains("标准层"))
                {
                    blockBounds.Add(block);
                }
                blockString.Add(BlockTools.GetAttributeInBlockReference(block.Id, "楼层编号"));
                if (blockBounds.Count > 0)
                {
                    FloorSpaces.Add(new ThIfcSpace { Boundary = GetBoundaryCurves(blockBounds)[0], Tags = blockString });
                }
            }

            return FloorSpaces;
        }
        public static List<ThIfcSpace> GetNonStandardSpaces(List<BlockReference> blocks)
        {
            var FloorSpaces = new List<ThIfcSpace>();
            var blockBounds = new List<BlockReference>();
            foreach (BlockReference block in blocks)
            {
                if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Contains("非标层"))
                {
                    blockBounds.Add(block);
                }
            }
            GetBoundaryCurves(blockBounds).ForEach(o => FloorSpaces.Add(new ThIfcSpace { Boundary = o }));
            return FloorSpaces;
        }
        public static List<Curve> GetBoundaryCurves(List<BlockReference> blockCollection)
        {
            var blockCurves = new List<Curve>();
            foreach (BlockReference block in blockCollection)
            {
                blockCurves.Add(ThWPipeOutputFunction.GetBlockBoundary(block));
            }
            return blockCurves;
        }
        private static Point3dCollection GetBoundaryVertices(List<ThIfcSpace> roofSpaces, List<ThIfcSpace> StandardSpaces)
        {
            var Vertices = new Point3dCollection();
            double minpt_x = double.MinValue;
            double minpt_y = double.MinValue;
            double maxpt_x = double.MaxValue;
            double maxpt_y = double.MaxValue;
            for (int i = 0; i < roofSpaces.Count; i++)
            {
                Polyline bound = roofSpaces[i].Boundary as Polyline;
                var minpoint = bound.GeometricExtents.MinPoint;
                var maxpoint = bound.GeometricExtents.MaxPoint;
                if (maxpoint.X > minpt_x)
                {
                    minpt_x = maxpoint.X;
                }
                if (maxpoint.Y > minpt_y)
                {
                    minpt_y = maxpoint.Y;
                }
                if (minpoint.X < maxpt_x)
                {
                    maxpt_x = minpoint.X;
                }
                if (minpoint.Y < maxpt_y)
                {
                    maxpt_y = minpoint.Y;
                }
            }
            Vertices.Add(new Point3d(maxpt_x, maxpt_y, 0));
            Vertices.Add(new Point3d(minpt_x, maxpt_y, 0));
            Vertices.Add(new Point3d(minpt_x, minpt_y, 0));
            Vertices.Add(new Point3d(maxpt_x, minpt_y, 0));
            if (StandardSpaces.Count > 0)
            {
                Polyline bound1 = StandardSpaces[0].Boundary as Polyline;
                foreach (Point3d pt in bound1.Vertices())
                {
                    Vertices.Add(pt);
                }
            }
            return Vertices;
        }
    }
}
