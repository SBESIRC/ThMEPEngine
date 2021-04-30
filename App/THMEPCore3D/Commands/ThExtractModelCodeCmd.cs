using System;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using AcHelper.Commands;
using THMEPCore3D.Engine;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace THMEPCore3D.Commands
{
    public class ThExtractModelCodeCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            using (AcadDatabase acadDb=AcadDatabase.Active())
            using (var codeEngine = new ThModelCodeExtractionEngine())
            using (var roomQueryEngine = new ThRoomQueryEngine())
            {
                var per = Active.Editor.GetEntity("\n选择一个框线");
                var pts = new Point3dCollection();
                if (per.Status == PromptStatus.OK)
                {
                    var frame = acadDb.Element<Polyline>(per.ObjectId);
                    pts = frame.VerticesEx(100.0);
                }
                codeEngine.Extract(acadDb.Database, pts);
                roomQueryEngine.Query(codeEngine.Results);
            }
        }
    }
}
