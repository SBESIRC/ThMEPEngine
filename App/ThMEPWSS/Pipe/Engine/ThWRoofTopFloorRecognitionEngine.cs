using System;
using Linq2Acad;
using DotNetARX;
using ThCADExtension;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Tools;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Service;

namespace ThMEPWSS.Pipe.Engine
{
    /// <summary>
    /// 小屋面
    /// </summary>
    public class ThWRoofTopFloorRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWRoofTopFloorRoom> Rooms { get; set; }
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
        public List<ThWGravityWaterBucket> GravityWaterBuckets { get; set; }
        public List<ThWSideEntryWaterBucket> SideEntryWaterBuckets { get; set; }
        public List<ThWRoofRainPipe> RoofRainPipes { get; set; }
        public List<ThIfcSpace> DeviceSpaces { get; set; }
        public List<ThIfcSpace> RoofSpaces { get; set; }
        public List<ThIfcSpace> StandardSpaces { get; set; }
        public List<ThIfcSpace> NonStandardSpaces { get; set; }
        public List<BlockReference> blockCollection { get; set; }
        public ThWRoofTopFloorRecognitionEngine()
        {
            Rooms = new List<ThWRoofTopFloorRoom>();
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
            AllObstacles = new List<Curve>();
            Layers = new List<string>();
            Spaces = new List<ThIfcSpace>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            Rooms = new List<ThWRoofTopFloorRoom>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {             
                if (this.Spaces.Count == 0)
                {
                    this.Spaces = GetSpaces(database, pts);
                }
                //获取外参元素
                GetBuildingElements(database, pts);
                GetLayers(database, pts).ForEach(o => Layers.Add(o));
                GetTagNameFrames(database, pts);
                GetStarFrames(this.Spaces);
                //TODO: 获取块的GeometricExtents会抛出异常
                //GetInnerDoors(database, pts);
                //GetDevices(database, pts);
                GetElevationFrames(database, pts);
                GetAxialCircleTag(database, pts);
                GetAxialAxisTags(database, pts);               
                GetExternalTags(database, pts);              
                //获取本图元素
                GetWells(database, pts);
                GetAllTags(database, pts);              
                GetRainPipes(database, pts);             
                GetPositionTags(database, pts);
                //指定阻碍物
                GetAllObstacles();
                var baseCircles = GetBaseCircles(blockCollection);
                Rooms = ThRoofDeviceFloorRoomService.Build(DeviceSpaces, GravityWaterBuckets, SideEntryWaterBuckets, RoofRainPipes, baseCircles);
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
        private static List<string> GetLayers(Database database, Point3dCollection pts)
        {
            var strings = new List<string>();
            using (var LayerNamesDbExtension = new ThLayerNamesDbExtension(database))
            {
                strings = LayerNamesDbExtension.LayerFilter;
            }
            return strings;
        }
        private void GetPositionTags(Database database, Point3dCollection pts)
        {           
            var circles = new List<Curve>();
            var texts = new List<DBText>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var positionTagsDbExtension = new ThPositionTagsDbExtension(database))
            {
                positionTagsDbExtension.BuildElementCurves();
                circles = positionTagsDbExtension.Polylines;
                positionTagsDbExtension.BuildElementTexts();
                texts = positionTagsDbExtension.texts;
            }
            circles.ForEach(o => PositionTags.Add(ThWPipeOutputFunction.GetPolylineBoundary(o)));
            texts.ForEach(o => PositionTags.Add(ThWPipeOutputFunction.GetTextBoundary(o.WidthFactor * o.Height, o.Height, o.Position)));       
        }
        private void GetRainPipes(Database database, Point3dCollection pts)
        {
            var circles = new List<Curve>();         
            var innerDoorEngine = new ThWRainPipeRecognitionEngine();
            innerDoorEngine.Recognize(database, pts);
            innerDoorEngine.Elements.ForEach(o =>
            {
                Curve curve = o.Outline as Curve;
                circles.Add(curve.WashClone());
            });
            circles.ForEach(o => RainPipes.Add(ThWPipeOutputFunction.GetPolylineBoundary(o)));       
        }
        private void GetAllTags(Database database, Point3dCollection pts)
        {
            var circles = new List<Curve>();         
            var texts = new List<DBText>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var dimensionTagsDbExtension = new ThDimensionTagsDbExtension(database))
            {
                dimensionTagsDbExtension.BuildElementCurves();
                circles = dimensionTagsDbExtension.Polylines;
                dimensionTagsDbExtension.BuildElementTexts();
                texts = dimensionTagsDbExtension.texts;
            }
            circles.ForEach(o => DimensionTags.Add(ThWPipeOutputFunction.GetPolylineBoundary(o)));
            texts.ForEach(o => DimensionTags.Add(ThWPipeOutputFunction.GetTextBoundary(o.WidthFactor * o.Height, o.Height, o.Position)));         
        }
        private void GetWells(Database database, Point3dCollection pts)
        {
            var wellsEngine = new ThWWellRecognitionEngine();
            wellsEngine.Recognize(database, pts);
            wellsEngine.Elements.ForEach(o =>
            {
                Polyline curve = o.Outline as Polyline;
                Wells.Add(curve.WashClone());
            });
        }
        private void GetExternalTags(Database database, Point3dCollection pts)
        {  
            var externalTagsEngine = new ThWExternalTagRecognitionEngine();
            externalTagsEngine.Recognize(database, pts);
            externalTagsEngine.Elements.ForEach(o =>
            {
                Polyline curve = o.Outline as Polyline;
                ExternalTags.Add(curve.WashClone());
            });         
        }

        private void GetAxialAxisTags(Database database, Point3dCollection pts)
        {
            var polylines = new List<Curve>();           
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var axialAxisTagDbExtension = new ThAxialAxisTagDbExtension(database))
            {
                axialAxisTagDbExtension.BuildElementCurves();
                polylines = axialAxisTagDbExtension.Polylines;
            }
            polylines.ForEach(o => AxialAxisTags.Add(ThWPipeOutputFunction.GetPolylineBoundary(o)));           
        }

        private void GetAxialCircleTag(Database database, Point3dCollection pts)
        {
            var circles = new List<Circle>();        
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var axialCircleTagDbExtension = new ThAxialCircleTagDbExtension(database))
            {
                axialCircleTagDbExtension.BuildElementCurves();
                circles = axialCircleTagDbExtension.Circles;
            }
            circles.ForEach(o => AxialCircleTags.Add(ThWPipeOutputFunction.GetCircleBoundary(o)));          
        }
        private void GetInnerDoors(Database database, Point3dCollection pts)
        {
            var engine = new ThDoorRecognitionEngine();
            engine.Recognize(database, pts);
            InnerDoors=engine.Elements.Where(o => o.Outline is Curve).Select(o => o.Outline as Curve).ToList();      
        }
        private void GetDevices(Database database, Point3dCollection pts)
        {
            var engine = new ThWDeviceRecognitionEngine();
            engine.Recognize(database, pts);
            Devices=engine.Elements.Where(o => o.Outline is Curve).Select(o => o.Outline as Curve).ToList();
        }
        private void  GetTagNameFrames(Database database, Point3dCollection pts)
        {
            var architectureElevationEngine = new ThWArchitectureElevationRecognitionEngine();
            architectureElevationEngine.Recognize(database, pts);
            architectureElevationEngine.DbTexts.ForEach(o =>
            {
                if (!o.Layer.Contains("LEVL"))
                {
                    TagNameFrames.Add(ThWPipeOutputFunction.GetTextBoundary(o.WidthFactor * o.Height, o.Height, o.Position));
                }
            });
        }
        private void GetElevationFrames(Database database, Point3dCollection pts)
        {     
            var architectureElevationEngine = new ThWArchitectureElevationRecognitionEngine();
            architectureElevationEngine.Recognize(database, pts);
            architectureElevationEngine.DbTexts.ForEach(o =>
            {
                if (o.Layer.Contains("LEVL"))
                {
                    ElevationFrames.Add(ThWPipeOutputFunction.GetTextBoundary(o.WidthFactor * o.Height, o.Height, o.Position));
                }
            });            
        }
        private void GetStarFrames(List<ThIfcSpace> spaces)
        {         
            foreach (ThIfcSpace space in spaces)
            {
                Point3d minPoint = space.Boundary.GeometricExtents.MinPoint;
                Point3d maxPoint = space.Boundary.GeometricExtents.MaxPoint;
                if (Math.Abs(minPoint.DistanceTo(maxPoint) - 5500) < 100)
                {
                    StairFrames.Add(space.Boundary as Polyline);
                }
            }         
        }

        private void GetBuildingElements(Database database, Point3dCollection pts)
        {
            var extractor = new ThBuildingElementExtractor();
            var columnVisitor = new ThColumnExtractionVisitor()
            {
                LayerFilter = ThStructureColumnLayerManager.HatchXrefLayers(database),
            };
            var shearWallVisitor = new ThShearWallExtractionVisitor()
            {
                LayerFilter = ThStructureShearWallLayerManager.HatchXrefLayers(database),
            };
            var windowVisitor = new ThWindowExtractionVisitor()
            {
                LayerFilter = ThWindowLayerManager.CurveXrefLayers(database),
            };
            var archWallVisitor = new ThArchitectureWallExtractionVisitor()
            {
                LayerFilter = ThArchitectureWallLayerManager.CurveXrefLayers(database),
            };
            extractor.Accept(columnVisitor);
            extractor.Accept(shearWallVisitor);
            extractor.Accept(windowVisitor);
            extractor.Accept(archWallVisitor);
            extractor.Extract(database);

            var columnEngine = new ThColumnRecognitionEngine();
            columnEngine.Recognize(columnVisitor.Results, pts);
            var columns = columnEngine.Elements.Where(o => o.Outline is Curve).Select(o => o.Outline as Curve);
            var shearWallEngine = new ThShearWallRecognitionEngine();
            shearWallEngine.Recognize(shearWallVisitor.Results, pts);
            var shearWalls = shearWallEngine.Elements.Where(o => o.Outline is Curve).Select(o => o.Outline as Curve);
            var windowEngine = new ThWindowRecognitionEngine();
            windowEngine.Recognize(windowVisitor.Results, pts);
            var windows = windowEngine.Elements.Where(o => o.Outline is Curve).Select(o => o.Outline as Curve);
            var archWallEngine = new ThArchitectureWallRecognitionEngine();
            archWallEngine.Recognize(archWallVisitor.Results, pts);
            var archWalls = archWallEngine.Elements.Where(o => o.Outline is Curve).Select(o => o.Outline as Curve);
            Windows.AddRange(windows);
            Columns.AddRange(columns);
            ShearWalls.AddRange(shearWalls);
            ArchitectureWalls.AddRange(archWalls);
        }

        private void GetAllObstacles()
        {
            AllObstacles.AddRange(Windows);
            AllObstacles.AddRange(Columns);
            AllObstacles.AddRange(ShearWalls);
            AllObstacles.AddRange(ArchitectureWalls);
            AllObstacles.AddRange(Wells);
            AllObstacles.AddRange(TagNameFrames);
            AllObstacles.AddRange(StairFrames);
            AllObstacles.AddRange(InnerDoors); 
            AllObstacles.AddRange(Devices); 
            AllObstacles.AddRange(ElevationFrames); 
            AllObstacles.AddRange(AxialCircleTags); 
            AllObstacles.AddRange(AxialAxisTags); 
            AllObstacles.AddRange(ExternalTags); 
            AllObstacles.AddRange(DimensionTags); 
            AllObstacles.AddRange(RainPipes); 
            AllObstacles.AddRange(PositionTags);
        }
    }
}
