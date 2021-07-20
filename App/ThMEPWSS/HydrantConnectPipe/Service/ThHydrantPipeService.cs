using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.HydrantConnectPipe.Model;
using ThMEPWSS.HydrantConnectPipe.Engine;

namespace ThMEPWSS.HydrantConnectPipe.Service
{
    public class ThHydrantPipeService
    {
        public List<ThHydrantPipe> GetFireHydrantPipeFromLayer(Point3dCollection selectArea)//从层里面提取立管
        {
            using (var database = AcadDatabase.Active())
            using (var acadDb = AcadDatabase.Use(database.Database))
            {
                var hydrantPipe = acadDb.ModelSpace.OfType<Circle>().Where(o => o.Layer == "W-FRPT-HYDT-EQPM" || o.Layer == "W-FRPT-HYDT" || o.Layer == "W-FRPT-EXTG").ToList();
                var map = new Dictionary<Polyline, Circle>();
                hydrantPipe.ForEach(o => map.Add(o.ToRectangle(),o));

                var spatialIndex = new ThCADCoreNTSSpatialIndex(map.Keys.ToCollection());
                var dbObjects = spatialIndex.SelectCrossingPolygon(selectArea);

                var rst = new List<ThHydrantPipe>();
                dbObjects.Cast<Polyline>().ForEach(p=> rst.Add(ThHydrantPipe.Create(map[p])));
                return rst;
            }
        }
        public List<ThHydrantPipe> GetFireHydrantPipeFromBR(Point3dCollection selectArea)//从块里面提取立管
        {
            List<ThHydrantPipe> fireHydrantPipes = new List<ThHydrantPipe>();
            using (var database = AcadDatabase.Active())
            using (var hydrantPipeEngine = new ThHydrantPipeRecognitionEngine())
            {
                hydrantPipeEngine.Recognize(database.Database, selectArea);//从块里面提取块
                hydrantPipeEngine.RecognizeMS(database.Database, selectArea);//从本图提取块
                foreach (var element in hydrantPipeEngine.Datas)
                {
                    ThHydrantPipe fireHydrantPipe = ThHydrantPipe.Create(element);
                    fireHydrantPipes.Add(fireHydrantPipe);
                }
            }
            return fireHydrantPipes;
        }

        public List<ThHydrantPipe> GetFireHydrantPipe(Point3dCollection selectArea)
        {
            List<ThHydrantPipe> fireHydrantPipes = new List<ThHydrantPipe>();
            fireHydrantPipes.AddRange(GetFireHydrantPipeFromLayer(selectArea));
            fireHydrantPipes.AddRange(GetFireHydrantPipeFromBR(selectArea));
            return fireHydrantPipes;
        }
    }
}
