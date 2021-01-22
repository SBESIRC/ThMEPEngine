using Linq2Acad;
using ThMEPWSS.Pipe.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Engine
{
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
            RoofDeviceFloors = new List<ThWRoofDeviceFloorRoom>();
            RoofFloors = new List<ThWRoofFloorRoom>();
            TopFloors = new List<ThWTopFloorRoom>();
            NormalFloors= new List<ThWTopFloorRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {                           
                var RoofDeviceEngine = new ThWRoofDeviceFloorRecognitionEngine();               
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
                var RoofEngine = new ThWRoofFloorRecognitionEngine()
                {
                    Spaces = RoofDeviceEngine.Spaces
                };
                RoofEngine.Recognize(database, pts);
                RoofFloors = RoofEngine.Rooms;
                var FirstEngine = new ThWTopFloorRecognitionEngine()
                {
                    Spaces = RoofEngine.Spaces
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
    }
}
