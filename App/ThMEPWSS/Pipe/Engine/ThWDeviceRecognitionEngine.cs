using System.Collections.Generic;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Plumbing;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.Pipe.Engine
{
  public  class ThWDeviceRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var deviceDbExtension = new ThDeviceDbExtension(database))
            {
                deviceDbExtension.BuildElementCurves();
                List<Entity> ents = new List<Entity>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    deviceDbExtension.Devices.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex basintoolSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in basintoolSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        ents.Add(filterObj as Entity);
                    }
                }
                else
                {
                    ents = deviceDbExtension.Devices;
                }
                ents.ForEach(o =>
                {
                    Elements.Add(ThIfcDevice.Create(o));
                });
            }
        }
    }
}
