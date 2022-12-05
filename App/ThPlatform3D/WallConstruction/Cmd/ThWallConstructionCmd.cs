using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using Linq2Acad;
using ThCADCore.NTS;
using AcHelper;
using DotNetARX;
using NFox.Cad;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.GeojsonExtractor.Service;

using ThPlatform3D.WallConstruction.Data;
using ThPlatform3D.WallConstruction.Service;
using ThPlatform3D.WallConstruction.Model;

namespace ThPlatform3D.WallConstruction.Cmd
{
    public class ThWallConstructionCmd : ThMEPBaseCommand, IDisposable
    {
        private ThWallConstructionViewModel VM;
        private void InitialCmdInfo()
        {
            ActionName = "生成";
            CommandName = "THQMZF";//墙面做法 
        }
        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            ThWallConostructionExecute();
        }

        private void ThWallConostructionExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var selectFrames = ThSelectFrameUtil.GetFrame();
                if (selectFrames.Count == 0)
                {
                    return;
                }

                var transformer = new ThMEPOriginTransformer(selectFrames[0]);
                transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));


                var dataQuery = ThWallConstructionUtilServices.GetData(acadDatabase, selectFrames, transformer);

                //可用dataQuery data
                
            }
        }
    }
}
