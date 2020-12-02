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
    public class ThGravityWaterBucketRecognitionEngine : ThDistributionElementRecognitionEngine
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
                    Elements.Add(ThIfcGravityWaterBucket.Create(o));
                });
            }
        }
    }
}
