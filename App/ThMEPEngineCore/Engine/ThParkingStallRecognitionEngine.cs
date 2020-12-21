using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Engine
{
    public class ThParkingStallRecognitionEngine : ThSpatialElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThDbObjectRecognitionEngine();
            engine.Recognize(new ThParkingStallDbExtension(database), polygon);
            foreach(Curve curve in engine.DbObjects)
            {
                Spaces.Add(ThIfcParkingStall.Create(curve));
            }
        }
    }
}
