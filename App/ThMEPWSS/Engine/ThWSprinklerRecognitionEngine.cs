using Linq2Acad;
using System.Linq;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPWSS.Uitl;
using ThMEPWSS.Model;

namespace ThMEPWSS.Engine
{
    public class ThWSprinklerRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.ModelSpace
                    .OfType<Entity>()
                    .Where(e => e.Layer == ThWSSCommon.SprayLayerName)
                    .Where(e => IsSprinkler(e))
                    .ForEach(e => Elements.Add(ThWSprinkler.Create(e)));
            }
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }

        private bool IsSprinkler(Entity e)
        {
            // 同时支持天华喷淋图块和天正喷淋自定义实体
            return ThWStandardService.Instance.IsSprinkler(e) || ThWTangentService.Instance.IsSprinkler(e);
        }
    }
}
