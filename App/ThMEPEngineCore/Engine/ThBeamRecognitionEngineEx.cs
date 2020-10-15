using System;
using AcHelper;
using Linq2Acad;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamRecognitionEngineEx : ThBuildingElementRecognitionEngine, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var beamTextDbExtension = new ThStructureBeamAnnotationDbExtension(Active.Database))
            {
                beamTextDbExtension.BuildElementTexts();
                beamTextDbExtension.Annotations.ForEach(o => Elements.Add(ThIfcLineBeam.Create(o)));
            }
        }
    }
}
