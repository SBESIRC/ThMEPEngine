using System;
using System.Linq;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Service;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Plumbing;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.CAD;
using ThMEPWSS.Pipe.Tools;
using ThCADExtension;
using ThMEPEngineCore.Service;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWRoofDeviceFloorRecognitionEngine : ThWRoomRecognitionEngine
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
        public ThWRoofDeviceFloorRecognitionEngine()
        {
            Rooms = new List<ThWRoofDeviceFloorRoom>();
            TagNameFrames = new List<Curve>();
            StairFrames= new List<Curve>();
            Columns= new List<Curve>();
            ShearWalls= new List<Curve>();
            InnerDoors= new List<Curve>();
            Devices = new List<Curve>();
            ArchitectureWalls = new List<Curve>();
            Windows= new List<Curve>();
            ElevationFrames = new List<Curve>();
            AxialCircleTags = new List<Curve>();
            AxialAxisTags = new List<Curve>();
            ExternalTags= new List<Curve>();
            Wells = new List<Curve>();
            DimensionTags= new List<Curve>();
            RainPipes= new List<Curve>();
            PositionTags = new List<Curve>();
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
                GetTagNameFrames(database, pts).ForEach(o=> TagNameFrames.Add(o));
                GetStarFrames(this.Spaces).ForEach(o => StairFrames.Add(o));
                GetColumns(database, pts).ForEach(o => Columns.Add(o));
                GetShearWalls(database, pts).ForEach(o => ShearWalls.Add(o));
                GetInnerDoors(database, pts).ForEach(o => InnerDoors.Add(o));
                GetDevices(database, pts).ForEach(o => Devices.Add(o));
                GetArchitectureWalls(database, pts).ForEach(o => ArchitectureWalls.Add(o));
                GetWindows(database, pts).ForEach(o => Windows.Add(o));
                GetElevationFrames(database, pts).ForEach(o => ElevationFrames.Add(o));
                GetAxialCircleTag(database, pts).ForEach(o => AxialCircleTags.Add(o));
                GetAxialAxisTags(database, pts).ForEach(o => AxialAxisTags.Add(o));
                GetExternalTags(database, pts).ForEach(o => ExternalTags.Add(o));
                //本图元素
                GetWells(database, pts).ForEach(o => Wells.Add(o));
                GetAllTags(database, pts).ForEach(o => DimensionTags.Add(o));
                GetRainPipes(database, pts).ForEach(o => RainPipes.Add(o));
                GetPositionTags(database, pts).ForEach(o => PositionTags.Add(o));
                var baseCircles = new List<ThIfcSpace>();
                var gravityWaterBuckets = GetgravityWaterBuckets(database, pts);
                var sideEntryWaterBuckets = GetsideEntryWaterBuckets(database, pts);
                var roofRainPipes = GetroofRainPipes(database, pts);         
                Rooms = ThRoofDeviceFloorRoomService.Build(this.Spaces, gravityWaterBuckets, sideEntryWaterBuckets, roofRainPipes, baseCircles);
            }
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
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var rainPipesDbExtension = new ThRainPipesDbExtension(database))
            {
                rainPipesDbExtension.BuildElementCurves();
                Columns = rainPipesDbExtension.Polylines;              
            }
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
            var WellsEngine = new ThWWellsRecognitionEngine();
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
            var ExternalTagsEngine = new ThWExternalTagsRecognitionEngine();
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
        private static List<Curve> GetColumns(Database database, Point3dCollection pts )
        {
            var Columns = new List<Curve>();
            var ColumnRecognitionEngine = new ThColumnRecognitionEngine();
            ColumnRecognitionEngine.Recognize(database, pts);
            ColumnRecognitionEngine.Elements.ForEach(o =>
            {
                var curve = o.Outline as Curve;
                Columns.Add(curve.WashClone());
            });
            return Columns;
        }
        private static List<Curve> GetShearWalls(Database database, Point3dCollection pts)
        {
            var Columns = new List<Curve>();
            var shearWallEngine = new ThShearWallRecognitionEngine();
            shearWallEngine.Recognize(database, pts);
            shearWallEngine.Elements.ForEach(o =>
            {
                if (o.Outline is Curve curve)
                {
                    Columns.Add(curve.WashClone());
                }
                else if (o.Outline is MPolygon mPolygon)
                {
                    throw new NotSupportedException();
                }
            });
            return Columns;
        }
        private static List<Curve> GetInnerDoors(Database database, Point3dCollection pts)
        {
            var Columns = new List<Curve>();
            var innerDoorEngine = new ThWInnerDoorRecognitionEngine();
            innerDoorEngine.Recognize(database, pts);
            innerDoorEngine.Elements.ForEach(o =>
            {
                Polyline curve = o.Outline as Polyline;
                Columns.Add(curve.WashClone());
            });
            return Columns;
        }
        private static List<Curve> GetDevices(Database database, Point3dCollection pts)
        {
            var Columns = new List<Curve>();
            var deviceEngineEngine = new ThWDeviceRecognitionEngine();
            deviceEngineEngine.Recognize(database, pts);
            deviceEngineEngine.Elements.ForEach(o =>
            {
                Polyline curve = o.Outline as Polyline;
                Columns.Add(curve.WashClone());
            });
            return Columns;
        }
        private static List<Curve> GetArchitectureWalls(Database database, Point3dCollection pts)
        {
            var Columns = new List<Curve>();
            var architectureWallRecognitionEngine = new ThArchitectureWallRecognitionEngine();
            architectureWallRecognitionEngine.Recognize(database, pts);
            architectureWallRecognitionEngine.Elements.ForEach(o =>
            {
                if (o.Outline is Curve curve)
                {
                    Columns.Add(curve.WashClone());
                }
            });
            return Columns;
        }
        private static List<Curve> GetWindows(Database database, Point3dCollection pts)
        {
            var Columns = new List<Curve>();
            var windowRecognition = new ThWindowRecognitionEngine();
            windowRecognition.Recognize(database, pts);
            windowRecognition.Elements.ForEach(o =>
            {
                Polyline curve = o.Outline as Polyline;
                Columns.Add(curve.WashClone());
            });
            return Columns;
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
                if (Math.Abs(minPoint.DistanceTo(maxPoint)-5500) < 100)
                {                                
                    frame.Add(space.Boundary as Polyline);
                }
            }
            return frame;
        }
        private List<ThIfcGravityWaterBucket> GetgravityWaterBuckets(Database database, Point3dCollection pts)
        {
            using (ThGravityWaterBucketRecognitionEngine gravityWaterBucket = new ThGravityWaterBucketRecognitionEngine())
            {
                gravityWaterBucket.Recognize(database, pts);
                return gravityWaterBucket.Elements.Cast<ThIfcGravityWaterBucket>().ToList();
            }
        }
        private List<ThIfcSideEntryWaterBucket> GetsideEntryWaterBuckets(Database database, Point3dCollection pts)
        {
            using (ThSideEntryWaterBucketRecognitionEngine sideEntryWaterBucketEngine = new ThSideEntryWaterBucketRecognitionEngine())
            {
                sideEntryWaterBucketEngine.Recognize(database, pts);
                return sideEntryWaterBucketEngine.Elements.Cast<ThIfcSideEntryWaterBucket>().ToList();
            }
        }
        private List<ThIfcRoofRainPipe> GetroofRainPipes(Database database, Point3dCollection pts)
        {
            using (ThRoofRainPipeRecognitionEngine roofRainPipesEngine = new ThRoofRainPipeRecognitionEngine())
            {
                roofRainPipesEngine.Recognize(database, pts);
                return roofRainPipesEngine.Elements.Cast<ThIfcRoofRainPipe>().ToList();
            }
        }

    }
}
