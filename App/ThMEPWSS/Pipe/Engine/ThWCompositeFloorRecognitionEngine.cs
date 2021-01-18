using Linq2Acad;
using ThMEPWSS.Pipe.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThCADExtension;
using System;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWCompositeFloorRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWRoofDeviceFloorRoom> RoofDeviceFloors { get; set; }

        public List<ThWRoofFloorRoom> RoofFloors { get; set; }
        public List<ThWTopFloorRoom> TopFloors { get; set; }

        public List<ThWTopFloorRoom> NormalFloors { get; set; }
        public List<Curve> Columns { get; set; }
        public ThWCompositeFloorRecognitionEngine()
        {
            Columns =new List<Curve>();
            RoofDeviceFloors = new List<ThWRoofDeviceFloorRoom>();
            RoofFloors = new List<ThWRoofFloorRoom>();
            TopFloors = new List<ThWTopFloorRoom>();
            NormalFloors= new List<ThWTopFloorRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
               
                var ColumnRecognitionEngine=new ThColumnRecognitionEngine();
                ColumnRecognitionEngine.Recognize(database, pts);
                ColumnRecognitionEngine.Elements.ForEach(o =>
                {
                    var curve = o.Outline as Curve;
                    Columns.Add(curve.WashClone());
                });
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
                var RoofDeviceEngine = new ThWRoofDeviceFloorRecognitionEngine();               
                RoofDeviceEngine.Recognize(database, pts);
                RoofDeviceFloors = RoofDeviceEngine.Rooms;
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
