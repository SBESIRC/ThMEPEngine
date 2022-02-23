using System;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Tools;
using ThMEPWSS.Pipe.Service;

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

        public override void ExtractFromMS(Database database)
        {
            throw new NotSupportedException();
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotSupportedException();
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

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }
    }
    public class ThWCompositeFloorRecognitionEngine : ThWRoomRecognitionEngine, IDisposable
    {
        public List<ThWRoofTopFloorRoom> RoofTopFloors { get; set; }
        public List<ThWRoofFloorRoom> RoofFloors { get; set; }
        public List<ThWTopFloorRoom> TopFloors { get; set; }
        public List<ThWTopFloorRoom> NormalFloors { get; set; }
        public List<Curve> TagNameFrames { get; set; }
        public List<Curve> StairFrames { get; set; }
        public List<Curve> Columns { get; set; }
        public List<Curve> Walls { get; set; }
        public List<Curve> Doors { get; set; }
        public List<Curve> Devices { get; set; }
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
        public Dictionary<Point3d, string> NonStandardBaseCircles { get; set; }
        public Dictionary<Point3d, string> StandardBaseCircles { get; set; }
        public ThWCompositeFloorRecognitionEngine()
        {
            TagNameFrames = new List<Curve>();
            StairFrames = new List<Curve>();
            Columns = new List<Curve>();
            Walls = new List<Curve>();
            Doors = new List<Curve>();
            Devices = new List<Curve>();
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
            RoofTopFloors = new List<ThWRoofTopFloorRoom>();
            RoofFloors = new List<ThWRoofFloorRoom>();
            TopFloors = new List<ThWTopFloorRoom>();
            NormalFloors= new List<ThWTopFloorRoom>();
            NonStandardBaseCircles = new Dictionary<Point3d, string>();
            StandardBaseCircles = new Dictionary<Point3d, string>();
        }
        public void Dispose()
        {
            //
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var engine = new ThStoreysRecognitionEngine();
                engine.Recognize(acadDatabase.Database, pts);
                if (engine.Elements.Count == 0)
                {
                    return;
                }
                var blockCollection = new List<BlockReference>();
                engine.Elements.Cast<ThWStoreys>().ForEach(o =>
                {
                    blockCollection.Add(acadDatabase.Element<BlockReference>(o.ObjectId));
                });
                var deviceSpaces = GetDeviceSpaces(blockCollection);
                var roofSpaces = GetRoofSpaces(blockCollection);
                var standardSpaces = GetStandardSpaces(blockCollection);
                var nonStandardSpaces = GetNonStandardSpaces(blockCollection);
                var base_Circles = GetBaseCircles(blockCollection);
                var frameSpaces=new List<ThIfcSpace>();
                if (deviceSpaces.Count > 0)
                {
                    frameSpaces.Add(deviceSpaces[0]);
                }
                if (roofSpaces.Count > 0)
                {
                    frameSpaces.Add(roofSpaces[0]);
                }

                var boundaryEngine = new ThMEPEngineCore.Engine.ThDB3RoomOutlineRecognitionEngine();
                boundaryEngine.RecognizeMS(acadDatabase.Database, pts);
                var rooms = boundaryEngine.Elements.Cast<ThIfcRoom>().ToList();
                var markEngine = new ThAIRoomMarkRecognitionEngine();
                markEngine.RecognizeMS(acadDatabase.Database, pts);
                var marks = markEngine.Elements.Cast<ThIfcTextNote>().ToList();
                var builder = new ThRoomBuilderEngine();
                builder.Build(rooms, marks);
                //this.Spaces = rooms.Select(o => new ThIfcSpace()
                //{
                //    Tags = o.Tags,
                //    Boundary = o.Boundary,
                //}).ToList();

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
                var RoofDeviceEngine = new ThWRoofTopFloorRecognitionEngine()
                {
                    Spaces = this.Spaces,
                    blockCollection = blockCollection,
                    DeviceSpaces = deviceSpaces,
                    RoofRainPipes = roofRainPipes,
                    GravityWaterBuckets = GravityWaterBuckets,
                    SideEntryWaterBuckets = SideEntryWaterBuckets,
                };               
                RoofDeviceEngine.Recognize(database, pts);             
                RoofTopFloors = RoofDeviceEngine.Rooms;
                RoofDeviceEngine.TagNameFrames.ForEach(o => TagNameFrames.Add(o));
                RoofDeviceEngine.StairFrames.ForEach(o => StairFrames.Add(o));
                RoofDeviceEngine.Columns.ForEach(o => Columns.Add(o));
                RoofDeviceEngine.Walls.ForEach(o => Walls.Add(o));
                RoofDeviceEngine.Doors.ForEach(o => Doors.Add(o));
                RoofDeviceEngine.Devices.ForEach(o => Devices.Add(o));
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
                    blockCollection = blockCollection,
                    RoofSpaces = roofSpaces,
                    gravityWaterBuckets = GravityWaterBuckets,                 
                   sideEntryWaterBuckets= SideEntryWaterBuckets,
                   roofRainPipes= roofRainPipes
                };            
                RoofEngine.Recognize(database, pts);
                RoofFloors = RoofEngine.Rooms;
                var FirstEngine = new ThWTopFloorRecognitionEngine()
                {
                    Spaces= this.Spaces,
                    blockCollection = blockCollection,
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
                NonStandardBaseCircles = FirstEngine.NonStandardBaseCircles;
                StandardBaseCircles= FirstEngine.StandardBaseCircles;
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
            var Floor_Spaces = new List<ThIfcSpace>();
            var tags = "";
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
                    if (tags == "")
                    {
                        string[] patterns = ThStructureUtils.OriginalFromXref(blockString[0]).ToUpper().Split('-').Reverse().ToArray();
                        tags = patterns[0];
                        FloorSpaces.Add(new ThIfcSpace { Boundary = GetBoundaryCurves(blockBounds)[0], Tags = blockString });
                    }
                    else
                    {
                        string[] patterns = ThStructureUtils.OriginalFromXref(blockString[0]).ToUpper().Split('-').Reverse().ToArray();
                        if(int.Parse(patterns[0])> int.Parse(tags))
                        {
                            FloorSpaces.Add(new ThIfcSpace { Boundary = GetBoundaryCurves(blockBounds)[0], Tags = blockString });
                        }
                    }
                }
            }
            if (FloorSpaces.Count > 1)
            {
                Floor_Spaces.Add(FloorSpaces[FloorSpaces.Count-1]);
            }
            else
            {
                Floor_Spaces.Add(FloorSpaces[0]);
            }
            return Floor_Spaces;
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
