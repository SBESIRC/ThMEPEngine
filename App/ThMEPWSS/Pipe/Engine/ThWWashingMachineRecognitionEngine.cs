using System.Collections.Generic;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Model.Plumbing;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWashMachineRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var washmachineDbExtension = new ThWashMachineDbExtension(database))
            {
                washmachineDbExtension.BuildElementCurves();
                List<Entity> ents = new List<Entity>();

                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    washmachineDbExtension.Washmachine.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex washmachineSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in washmachineSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        ents.Add(filterObj as Entity);
                    }
                }
                else
                {
                    ents = washmachineDbExtension.Washmachine;
                }
                ents.ForEach(o =>
                {
                    Elements.Add(ThWWashingMachine.Create(o));
                });
            }
        }
    }
}
