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

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWRoofDeviceFloorRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWRoofDeviceFloorRoom> Rooms { get; set; }
        public List<Polyline> TagNameFrames { get; set; }
        public ThWRoofDeviceFloorRecognitionEngine()
        {
            Rooms = new List<ThWRoofDeviceFloorRoom>();
            TagNameFrames = new List<Polyline>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            Rooms = new List<ThWRoofDeviceFloorRoom>();
            TagNameFrames = new List<Polyline>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                if (this.Spaces.Count == 0)
                {
                    this.Spaces = GetSpaces(database, pts);
                }
                TagNameFrames = GetTagNameFrames(this.Spaces);
                var baseCircles = new List<ThIfcSpace>();
                var gravityWaterBuckets = GetgravityWaterBuckets(database, pts);
                var sideEntryWaterBuckets = GetsideEntryWaterBuckets(database, pts);
                var roofRainPipes = GetroofRainPipes(database, pts);         
                Rooms = ThRoofDeviceFloorRoomService.Build(this.Spaces, gravityWaterBuckets, sideEntryWaterBuckets, roofRainPipes, baseCircles);
            }
        }
        private static List<Polyline> GetTagNameFrames(List<ThIfcSpace> spaces)
        {
            var frame = new List<Polyline>();
            foreach (ThIfcSpace space in spaces)
            {
                if(space.Tags.Count>0)
                {
                    var length=space.Tags[0].Length;
                    Point3d pt = ThGeometryTool.GetMidPt(
                         space.Boundary.GeometricExtents.MinPoint,
                         space.Boundary.GeometricExtents.MaxPoint);
                    frame.Add(ThWPipeOutputFunction.GetBoundary(length, pt));
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
