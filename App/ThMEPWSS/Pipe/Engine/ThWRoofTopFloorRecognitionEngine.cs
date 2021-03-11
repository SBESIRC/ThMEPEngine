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
        public List<ThWRoofDeviceFloorRoom> Rooms { get; set; }
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
            Rooms = new List<ThWRoofDeviceFloorRoom>();
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
            Rooms = new List<ThWRoofDeviceFloorRoom>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {             
                if (this.Spaces.Count == 0)
                {
                    this.Spaces = GetSpaces(database, pts);
                }
                GetLayers(database, pts).ForEach(o => Layers.Add(o));
                GetTagNameFrames(database, pts).ForEach(o => TagNameFrames.Add(o));
                GetTagNameFrames(database, pts).ForEach(o => AllObstacles.Add(o));
                GetStarFrames(this.Spaces).ForEach(o => StairFrames.Add(o));
                GetStarFrames(this.Spaces).ForEach(o => AllObstacles.Add(o));
                GetColumns(database, pts).ForEach(o => Columns.Add(o));
                GetColumns(database, pts).ForEach(o => AllObstacles.Add(o));
                GetShearWalls(database, pts).ForEach(o => ShearWalls.Add(o));
                GetShearWalls(database, pts).ForEach(o => AllObstacles.Add(o));
                GetInnerDoors(database, pts).ForEach(o => InnerDoors.Add(o));
                GetInnerDoors(database, pts).ForEach(o => AllObstacles.Add(o));
                GetDevices(database, pts).ForEach(o => Devices.Add(o));
                GetDevices(database, pts).ForEach(o => AllObstacles.Add(o));
                GetArchitectureWalls(database, pts).ForEach(o => ArchitectureWalls.Add(o));
                GetArchitectureWalls(database, pts).ForEach(o => AllObstacles.Add(o));
                GetWindows(database, pts).ForEach(o => Windows.Add(o));
                GetWindows(database, pts).ForEach(o => AllObstacles.Add(o));
                GetElevationFrames(database, pts).ForEach(o => ElevationFrames.Add(o));
                GetElevationFrames(database, pts).ForEach(o => AllObstacles.Add(o));
                GetAxialCircleTag(database, pts).ForEach(o => AxialCircleTags.Add(o));
                GetAxialCircleTag(database, pts).ForEach(o => AllObstacles.Add(o));
                GetAxialAxisTags(database, pts).ForEach(o => AxialAxisTags.Add(o));
                GetAxialAxisTags(database, pts).ForEach(o => AllObstacles.Add(o));
                GetExternalTags(database, pts).ForEach(o => ExternalTags.Add(o));
                GetExternalTags(database, pts).ForEach(o => AllObstacles.Add(o));
                //本图元素
                GetWells(database, pts).ForEach(o => Wells.Add(o));
                GetWells(database, pts).ForEach(o => AllObstacles.Add(o));
                GetAllTags(database, pts).ForEach(o => DimensionTags.Add(o));
                GetAllTags(database, pts).ForEach(o => AllObstacles.Add(o));
                GetRainPipes(database, pts).ForEach(o => RainPipes.Add(o));
                GetRainPipes(database, pts).ForEach(o => AllObstacles.Add(o));
                GetPositionTags(database, pts).ForEach(o => PositionTags.Add(o));
                GetPositionTags(database, pts).ForEach(o => AllObstacles.Add(o));
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
        private static List<Curve> GetPositionTags(Database database, Point3dCollection pts)
        {
            var Columns = new List<Curve>();
            var circles = new List<Curve>();
            var texts = new List<DBText>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var positionTagsDbExtension = new ThPositionTagsDbExtension(database))
            {
                positionTagsDbExtension.BuildElementCurves();
                Columns = positionTagsDbExtension.Polylines;
                positionTagsDbExtension.BuildElementTexts();
                texts = positionTagsDbExtension.texts;
            }
            Columns.ForEach(o => circles.Add(ThWPipeOutputFunction.GetPolylineBoundary(o)));
            texts.ForEach(o => circles.Add(ThWPipeOutputFunction.GetTextBoundary(o.WidthFactor * o.Height, o.Height, o.Position)));
            return circles;
        }
        private static List<Curve> GetRainPipes(Database database, Point3dCollection pts)
        {
            var Columns = new List<Curve>();
            var circles = new List<Curve>();         
            var innerDoorEngine = new ThWRainPipeRecognitionEngine();
            innerDoorEngine.Recognize(database, pts);
            innerDoorEngine.Elements.ForEach(o =>
            {
                Curve curve = o.Outline as Curve;
                Columns.Add(curve.WashClone());
            });         
            Columns.ForEach(o => circles.Add(ThWPipeOutputFunction.GetPolylineBoundary(o)));
            return circles;

        }
        private static List<Curve> GetAllTags(Database database, Point3dCollection pts)
        {
            var Columns = new List<Curve>();
            var circles = new List<Curve>();
            var texts = new List<DBText>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var dimensionTagsDbExtension = new ThDimensionTagsDbExtension(database))
            {
                dimensionTagsDbExtension.BuildElementCurves();
                Columns = dimensionTagsDbExtension.Polylines;
                dimensionTagsDbExtension.BuildElementTexts();
                texts = dimensionTagsDbExtension.texts;
            }
            Columns.ForEach(o => circles.Add(ThWPipeOutputFunction.GetPolylineBoundary(o)));
            texts.ForEach(o => circles.Add(ThWPipeOutputFunction.GetTextBoundary(o.WidthFactor * o.Height, o.Height, o.Position)));
            return circles;
        }
        private static List<Curve> GetWells(Database database, Point3dCollection pts)
        {
            var Columns = new List<Curve>();
            var WellsEngine = new ThWWellRecognitionEngine();
            WellsEngine.Recognize(database, pts);
            WellsEngine.Elements.ForEach(o =>
            {
                Polyline curve = o.Outline as Polyline;
                Columns.Add(curve.WashClone());
            });
            return Columns;
        }
        private static List<Curve> GetExternalTags(Database database, Point3dCollection pts)
        {
            var Columns = new List<Curve>();
            var ExternalTagsEngine = new ThWExternalTagRecognitionEngine();
            ExternalTagsEngine.Recognize(database, pts);
            ExternalTagsEngine.Elements.ForEach(o =>
            {
                Polyline curve = o.Outline as Polyline;
                Columns.Add(curve.WashClone());
            });
            return Columns;
        }

        private static List<Curve> GetAxialAxisTags(Database database, Point3dCollection pts)
        {
            var Columns = new List<Curve>();
            var circles = new List<Curve>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var axialAxisTagDbExtension = new ThAxialAxisTagDbExtension(database))
            {
                axialAxisTagDbExtension.BuildElementCurves();
                Columns = axialAxisTagDbExtension.Polylines;
            }
            Columns.ForEach(o => circles.Add(ThWPipeOutputFunction.GetPolylineBoundary(o)));
            return circles;
        }

        private static List<Curve> GetAxialCircleTag(Database database, Point3dCollection pts)
        {
            var Columns = new List<Circle>();
            var circles = new List<Curve>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var axialCircleTagDbExtension = new ThAxialCircleTagDbExtension(database))
            {
                axialCircleTagDbExtension.BuildElementCurves();
                Columns = axialCircleTagDbExtension.Circles;
            }
            Columns.ForEach(o => circles.Add(ThWPipeOutputFunction.GetCircleBoundary(o)));
            return circles;
        }
        private static List<Curve> GetColumns(Database database, Point3dCollection pts)
        {
            var engine = new ThColumnRecognitionEngine();
            engine.Recognize(database, pts);
            return engine.Elements.Where(o => o.Outline is Curve).Select(o => o.Outline as Curve).ToList();
        }
        private static List<Curve> GetShearWalls(Database database, Point3dCollection pts)
        {
            var engine = new ThShearWallRecognitionEngine();
            engine.Recognize(database, pts);
            return engine.Elements.Where(o => o.Outline is Curve).Select(o => o.Outline as Curve).ToList();
        }
        private static List<Curve> GetInnerDoors(Database database, Point3dCollection pts)
        {
            var engine = new ThDoorRecognitionEngine();
            engine.Recognize(database, pts);
            return engine.Elements.Where(o => o.Outline is Curve).Select(o => o.Outline as Curve).ToList();
        }
        private static List<Curve> GetWindows(Database database, Point3dCollection pts)
        {
            var engine = new ThWindowRecognitionEngine();
            engine.Recognize(database, pts);
            return engine.Elements.Where(o => o.Outline is Curve).Select(o => o.Outline as Curve).ToList();
        }
        private static List<Curve> GetArchitectureWalls(Database database, Point3dCollection pts)
        {
            var engine = new ThArchitectureWallRecognitionEngine();
            engine.Recognize(database, pts);
            return engine.Elements.Where(o => o.Outline is Curve).Select(o => o.Outline as Curve).ToList();
        }
        private static List<Curve> GetDevices(Database database, Point3dCollection pts)
        {
            var engine = new ThWDeviceRecognitionEngine();
            engine.Recognize(database, pts);
            return engine.Elements.Where(o => o.Outline is Curve).Select(o => o.Outline as Curve).ToList();
        }
        private static List<Curve> GetTagNameFrames(Database database, Point3dCollection pts)
        {
            var Columns = new List<Curve>();
            var architectureElevationEngine = new ThWArchitectureElevationRecognitionEngine();
            architectureElevationEngine.Recognize(database, pts);
            architectureElevationEngine.DbTexts.ForEach(o =>
            {

                if (!o.Layer.Contains("LEVL"))
                {
                    Columns.Add(ThWPipeOutputFunction.GetTextBoundary(o.WidthFactor * o.Height, o.Height, o.Position));
                }
            });
            return Columns;
        }
        private static List<Curve> GetElevationFrames(Database database, Point3dCollection pts)
        {
            var Columns = new List<Curve>();
            var architectureElevationEngine = new ThWArchitectureElevationRecognitionEngine();
            architectureElevationEngine.Recognize(database, pts);

            architectureElevationEngine.DbTexts.ForEach(o =>
            {

                if (o.Layer.Contains("LEVL"))
                {
                    Columns.Add(ThWPipeOutputFunction.GetTextBoundary(o.WidthFactor * o.Height, o.Height, o.Position));
                }
            });
            return Columns;
        }
        private static List<Polyline> GetStarFrames(List<ThIfcSpace> spaces)
        {
            var frame = new List<Polyline>();
            foreach (ThIfcSpace space in spaces)
            {
                Point3d minPoint = space.Boundary.GeometricExtents.MinPoint;
                Point3d maxPoint = space.Boundary.GeometricExtents.MaxPoint;
                if (Math.Abs(minPoint.DistanceTo(maxPoint) - 5500) < 100)
                {
                    frame.Add(space.Boundary as Polyline);
                }
            }
            return frame;
        }
    }
}
