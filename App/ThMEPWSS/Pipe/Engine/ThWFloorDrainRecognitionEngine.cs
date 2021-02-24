using Linq2Acad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWFloorDrainRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            Elements.AddRange(RecognizeToiletFloorDrain(database, polygon));
            Elements.AddRange(RecognizeBalconyFloorDrain(database, polygon));
        }
        private List<ThWFloorDrain> RecognizeToiletFloorDrain(Database database, Point3dCollection polygon)
        {
            List<ThWFloorDrain> floorDrains = new List<ThWFloorDrain>();
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
                    floorDrains.Add(ThWFloorDrain.Create(o));
                });
                floorDrains.ForEach(o => o.Use = UseKind.Toilet);
            }
            return floorDrains;
        }
        private List<ThWFloorDrain> RecognizeBalconyFloorDrain(Database database, Point3dCollection polygon)
        {
            List<ThWFloorDrain> floorDrains = new List<ThWFloorDrain>();
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
                    floorDrains.Add(ThWFloorDrain.Create(o));
                });
            }
            floorDrains.ForEach(o => o.Use = UseKind.Balcony);
            return floorDrains;
        }
    }
}
