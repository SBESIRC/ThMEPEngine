using System;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;

namespace ThMEPWSS.Pipe.Engine
{
    public abstract class ThContainerRecognitionEngine : IDisposable
    {
        public void Dispose()
        {            
        }
        public abstract void Recognize(Database database, Point3dCollection pts);
        protected List<ThIfcSpace> GetSpaces(Database database, Point3dCollection pts)
        {
            using (var fixedPrecision=new ThCADCoreNTSFixedPrecision())
            using (var spaceEngine = new ThSpaceRecognitionEngine())
            {
                spaceEngine.Recognize(database, pts);
                return spaceEngine.Spaces;
            }
        }
    }
}
