using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using AcHelper;
using Dreambuild.AutoCAD;

using ThMEPEngineCore.Algorithm;

using ThPlatform3D.WallConstruction.Service;
using ThPlatform3D.WallConstruction.Cmd;

namespace ThPlatform3D
{
    internal class ThWallConstructionCmdEntrence
    {
        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThWallConstructionData", CommandFlags.Modal)]
        public void ThWallConstructionData()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //画框，提数据，转数据
                var selectFrames = ThSelectFrameUtil.GetFrame();
                if (selectFrames.Count == 0)
                {
                    return;
                }

                var transformer = new ThMEPOriginTransformer(selectFrames[0]);
                transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));
             

                var dataQuery = ThWallConstructionUtilServices.GetData(acadDatabase, selectFrames, transformer);
             
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThWallConstruction", CommandFlags.Modal)]
        public void ThWallConstruction()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                using (var cmd = new ThWallConstructionCmd())
                {
                    cmd.SubExecute();
                }
            }
        }

    }
}
