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
    public class ThWGravityWaterBucketRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var gravityWaterBucketDbExtension = new ThGravityWaterBucketDbExtension(database))
            {
                gravityWaterBucketDbExtension.BuildElementCurves();
                List<Entity> ents = new List<Entity>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    gravityWaterBucketDbExtension.GravityWaterBuckets.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex gravityWaterBucketSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in gravityWaterBucketSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        ents.Add(filterObj as Entity);
                    }
                }
                else
                {
                    ents = gravityWaterBucketDbExtension.GravityWaterBuckets;
                }
                ents.ForEach(o =>
                {
                    Elements.Add(ThWGravityWaterBucket.Create(o));
                });
            }
        }
    }
}
