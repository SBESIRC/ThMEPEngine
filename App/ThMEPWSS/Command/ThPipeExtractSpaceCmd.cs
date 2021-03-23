using System;
using Linq2Acad;
using AcHelper.Commands;
using ThMEPWSS.Pipe.Service;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.Command
{
    public class ThPipeExtractSpaceCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var service = new ThExtractDbSpaceService();
                service.Extract(acadDatabase.Database, new Point3dCollection());
            }
        }
    }
}
