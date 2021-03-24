using System;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Engine
{
    public abstract class ThWRoomRecognitionEngine
    {
        public List<ThIfcRoom> Spaces { get; set; } = new List<ThIfcRoom>();
        public abstract void Recognize(Database database, Point3dCollection pts);   
    }
}
