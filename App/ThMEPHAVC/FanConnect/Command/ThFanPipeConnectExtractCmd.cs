using AcHelper;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPHVAC.FanConnect.Service;

namespace ThMEPHVAC.FanConnect.Command
{
    class ThFanPipeConnectExtractCmd : ThMEPBaseCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }
        public void ImportBlockFile()
        {
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))//引用模块的位置
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                if (blockDb.Layers.Contains("AI-风管路由"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("AI-风管路由"), true);
                }
            }
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                acadDb.Database.UnFrozenLayer("AI-风管路由");
                acadDb.Database.UnLockLayer("AI-风管路由");
                acadDb.Database.UnOffLayer("AI-风管路由");
            }
        }
        public override void SubExecute()
        {
            try
            {
                using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (var database = AcadDatabase.Active())
                {
                    ImportBlockFile();
                    var startPt = ThFanConnectUtils.SelectPoint();
                    if (startPt.IsEqualTo(new Point3d()))
                    {
                        return;
                    }
                    //提取风管路由
                    var pipes = ThEquipElementExtractService.GetFanPipes(startPt);
                    ////水管干路和支干路
                    if (pipes.Count == 0)
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }

        }
    }
}
