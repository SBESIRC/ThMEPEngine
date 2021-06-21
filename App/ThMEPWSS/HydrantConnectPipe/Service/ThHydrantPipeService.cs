using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.HydrantConnectPipe.Engine;
using ThMEPWSS.HydrantConnectPipe.Model;

namespace ThMEPWSS.HydrantConnectPipe.Service
{
    public class ThHydrantPipeService
    {
        public List<ThHydrantPipe> GetFireHydrantPipeFromLayer(Point3dCollection selectArea)//从层里面提取立管
        {
            using (var database = AcadDatabase.Active())
            using (var acadDb = AcadDatabase.Use(database.Database))
            {
                var hydrantPipe = acadDb.ModelSpace.OfType<Entity>().Where(o => o.Layer == "W-FRPT-HYDT-EQPM").ToList();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(hydrantPipe.ToCollection());
                var dbObjects = spatialIndex.SelectCrossingPolygon(selectArea);

                var rst = new List<ThHydrantPipe>();
                foreach (var obj in dbObjects)
                {
                    if (obj is Circle)
                    {
                        var ent = obj as Entity;
                        var blk = obj as Circle;
                        rst.Add(ThHydrantPipe.Create(ent));
                    }
                }
                return rst;
            }
        }
        public List<ThHydrantPipe> GetFireHydrantPipeFromBR(Point3dCollection selectArea)//从块里面提取立管
        {
            List<ThHydrantPipe> fireHydrantPipes = new List<ThHydrantPipe>();
            using (var database = AcadDatabase.Active())
            using (var hydrantPipeEngine = new ThHydrantPipeRecognitionEngine())
            {
                hydrantPipeEngine.RecognizeMS(database.Database, selectArea);//从本图上取数据
                foreach (var element in hydrantPipeEngine.Datas)
                {
                    ThHydrantPipe fireHydrantPipe = ThHydrantPipe.Create(element.Geometry);
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
