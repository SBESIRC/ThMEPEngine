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

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWRoofFloorRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWRoofFloorRoom> Rooms { get; set; }
        public ThWRoofFloorRecognitionEngine()
        {
            Rooms = new List<ThWRoofFloorRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            Rooms = new List<ThWRoofFloorRoom>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                if (this.Spaces.Count == 0)
                {
                    this.Spaces = GetSpaces(database, pts);
                }
                var baseCircles= new List<ThIfcSpace>();
                var gravityWaterBuckets = GetgravityWaterBuckets(database, pts);
                var sideEntryWaterBuckets = GetsideEntryWaterBuckets(database, pts);
                var roofRainPipes = GetroofRainPipes(database, pts);
                Rooms = ThRoofFloorRoomService.Build(this.Spaces, gravityWaterBuckets, sideEntryWaterBuckets, roofRainPipes, baseCircles);
            }
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
