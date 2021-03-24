using DotNetARX;
using Linq2Acad;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Service;

namespace ThMEPWSS.Pipe.Engine
{
    /// <summary>
    /// 大屋面
    /// </summary>
    public class ThWRoofFloorRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWRoofFloorRoom> Rooms { get; set; }
        public List<ThWGravityWaterBucket> gravityWaterBuckets { get; set; }
        public List<ThWSideEntryWaterBucket> sideEntryWaterBuckets { get; set; }
        public List<ThWRoofRainPipe> roofRainPipes { get; set; }
        public List<ThIfcRoom> RoofSpaces { get; set; }
        public List<BlockReference> blockCollection { get; set; }
        public ThWRoofFloorRecognitionEngine()
        {
            Rooms = new List<ThWRoofFloorRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            Rooms = new List<ThWRoofFloorRoom>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {             
                var baseCircles = GetBaseCircles(blockCollection);
                Rooms = ThRoofFloorRoomService.Build(RoofSpaces, gravityWaterBuckets, sideEntryWaterBuckets, roofRainPipes, baseCircles);
            }
        }    
        private static List<ThIfcRoom> GetBaseCircles(List<BlockReference> blocks)
        {
            var FloorSpaces = new List<ThIfcRoom>();
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
                            FloorSpaces.Add(new ThIfcRoom { Boundary = baseCircle });
                        }
                    }
                }
            }
            return FloorSpaces;
        }  
    }
}
