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
    public class ThWSideEntryWaterBucketRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var sideEntryWaterBucketDbExtension = new ThSideEntryWaterBucketDbExtension(database))
            {
                sideEntryWaterBucketDbExtension.BuildElementCurves();
                List<Entity> ents = new List<Entity>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    sideEntryWaterBucketDbExtension.SideEntryWaterBuckets.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex sideEntryWaterBucketSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in sideEntryWaterBucketSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        ents.Add(filterObj as Entity);
                    }
                }
                else
                {
                    ents = sideEntryWaterBucketDbExtension.SideEntryWaterBuckets;
                }
                ents.ForEach(o =>
                {
                    Elements.Add(ThWSideEntryWaterBucket.Create(o));
                });
            }
        }
    }
}
