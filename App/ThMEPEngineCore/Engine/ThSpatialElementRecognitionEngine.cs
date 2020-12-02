using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThSpatialElementRecognitionEngine : IDisposable
    {
        public List<ThIfcSpace> Spaces { get; set; }

        public ThSpatialElementRecognitionEngine()
        {
            Spaces = new List<ThIfcSpace>();
        }

        public void Dispose()
        {
        }

        public abstract void Recognize(Database database, Point3dCollection polygon);
    }
}
