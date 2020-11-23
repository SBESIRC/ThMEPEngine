using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPEngineCore.Engine
{
    public class ThFloorDrainRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            Elements.AddRange(RecognizeToiletFloorDrain(database, polygon));
            Elements.AddRange(RecognizeBalconyFloorDrain(database, polygon));
        }
        private List<ThIfcFloorDrain> RecognizeToiletFloorDrain(Database database, Point3dCollection polygon)
        {
            List<ThIfcFloorDrain> floorDrains = new List<ThIfcFloorDrain>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var floorDrainDbExtension = new ThFloorDrainDbExtension(database))
            {
                floorDrainDbExtension.BuildElementCurves();
                List<Entity> ents = new List<Entity>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    floorDrainDbExtension.FloorDrains.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex floorDrainSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in floorDrainSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        ents.Add(filterObj as Entity);
                    }
                }
                else
                {
                    ents = floorDrainDbExtension.FloorDrains;
                }
                ents.ForEach(o =>
                {
                    floorDrains.Add(ThIfcFloorDrain.Create(o));
                });
                floorDrains.ForEach(o => o.Use = UseKind.Toilet);
            }
            return floorDrains;
        }
        private List<ThIfcFloorDrain> RecognizeBalconyFloorDrain(Database database, Point3dCollection polygon)
        {
            List<ThIfcFloorDrain> floorDrains = new List<ThIfcFloorDrain>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var floorDrainDbExtension = new ThBalconyFloorDrainDbExtension(database))
            {
                floorDrainDbExtension.BuildElementCurves();
                List<Entity> ents = new List<Entity>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    floorDrainDbExtension.FloorDrains.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex floorDrainSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in floorDrainSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        ents.Add(filterObj as Entity);
                    }
                }
                else
                {
                    ents = floorDrainDbExtension.FloorDrains;
                }
                ents.ForEach(o =>
                {
                    floorDrains.Add(ThIfcFloorDrain.Create(o));
                });
            }
            floorDrains.ForEach(o => o.Use = UseKind.Balcony);
            return floorDrains;
        }
    }
}
