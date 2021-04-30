using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPElectrical.SystemDiagram.Engine;

namespace ThMEPElectrical.Command
{
    public class ThAutoFireAlarmSystemCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var dataEngine = new ThAutoFireAlarmSystemRecognitionEngine())
            {
                var per = Active.Editor.GetEntity("\n选择一个框:");
                var pts = new Point3dCollection();
                if(per.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    var frame = acadDatabase.Element<Polyline>(per.ObjectId);
                    pts = frame.Vertices();

                }
                dataEngine.Recognize(acadDatabase.Database, pts);
            }
        }
    }
}
