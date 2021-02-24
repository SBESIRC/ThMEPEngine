using DotNetARX;
using Linq2Acad;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Plumbing;
using ThMEPWSS.Pipe.Tools;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Service;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWRoofFloorRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWRoofFloorRoom> Rooms { get; set; }
        public List<ThIfcGravityWaterBucket> gravityWaterBuckets { get; set; }
        public List<ThIfcSideEntryWaterBucket> sideEntryWaterBuckets { get; set; }
        public List<ThWRoofRainPipe> roofRainPipes { get; set; }
        public ThWRoofFloorRecognitionEngine()
        {
            Rooms = new List<ThWRoofFloorRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            Rooms = new List<ThWRoofFloorRoom>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var blockCollection = new List<BlockReference>();
                blockCollection = BlockTools.GetAllDynBlockReferences(database, "楼层框定");
                var RoofSpaces = new List<ThIfcSpace>();
                if (blockCollection.Count > 0)
                {
                    RoofSpaces = GetRoofSpaces(blockCollection);
                }
                var baseCircles = GetBaseCircles(blockCollection);
                Rooms = ThRoofFloorRoomService.Build(RoofSpaces, gravityWaterBuckets, sideEntryWaterBuckets, roofRainPipes, baseCircles);
            }
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
        private static List<ThIfcSpace> GetBaseCircles(List<BlockReference> blocks)
        {
            var FloorSpaces = new List<ThIfcSpace>();
            foreach (BlockReference block in blocks)
            {
                if (BlockTools.GetDynBlockValue(block.Id, "楼层类型").Contains("大屋面"))
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
    }
}
