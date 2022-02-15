using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using Linq2Acad;

using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.UI
{
    public class ThElectricalPDSUIApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }

        [CommandMethod("TIANHUACAD", "THPDSTest", CommandFlags.Modal)]
        public void THPDSTest()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var engine1 = new ThCableSegmentRecognitionEngine();
                engine1.RecognizeMS(acadDatabase.Database, new Point3dCollection());
                engine1.Results.OfType<Curve>().ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o);
                    o.LayerId = ThPDSLayerService.CreateAITestCableLayer(acadDatabase.Database);
                });

                var engine = new ThCabletraySegmentRecognitionEngine();
                engine.RecognizeMS(acadDatabase.Database, new Point3dCollection());
                engine.Results.OfType<Curve>().ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o);
                    o.LayerId = ThPDSLayerService.CreateAITestCabletrayLayer(acadDatabase.Database);
                });
            }
        }
    }
}
