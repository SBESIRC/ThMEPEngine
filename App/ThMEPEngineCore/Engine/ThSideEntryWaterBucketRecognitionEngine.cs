using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Plumbing;
namespace ThMEPEngineCore.Engine
{
    public class ThSideEntryWaterBucketRecognitionEngine : ThDistributionElementRecognitionEngine
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
                    Elements.Add(ThIfcSideEntryWaterBucket.Create(o));
                });
            }
        }
    }
}
