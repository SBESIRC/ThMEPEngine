using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Command;
using ThMEPWSS.DrainageGeneralPlan.Service;
using ThMEPWSS.DrainageGeneralPlan.Utils;
using ThMEPEngineCore.Diagnostics;

namespace ThMEPWSS.DrainageGeneralPlan
{
    public class ThDrainageGeneralPlanCmd : ThMEPBaseCommand, IDisposable
    {
        public ThDrainageGeneralPlanCmd()
        {
            InitialCmdInfo();
        }
        private void InitialCmdInfo()
        {
            ActionName = "排水总图";
            CommandName = "THPSZT"; //排水总图
        }

        public override void SubExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blkList =ThDrainageGeneralPlanCommon.BlkName;
                var layerList = ThDrainageGeneralPlanCommon.LayerList ;

                ThDrainageGeneralPlanPreService.LoadBlockLayerToDocument(acadDatabase.Database, blkList, layerList);
                
                ThDrainageGeneralPlanCommon.Drai_Main  = ThDrainageGeneralPlanPreService.ExtractMainPiPe(acadDatabase.Database, ThDrainageGeneralPlanCommon.LayerName_Drai_Main);
                DrawUtils.ShowGeometry(ThDrainageGeneralPlanCommon.Drai_Main, ThDrainageGeneralPlanCommon.LayerName_Drai_Main);

                ThDrainageGeneralPlanCommon.Drai_Out = ThDrainageGeneralPlanPreService.ExtractOutPiPe(acadDatabase.Database, ThDrainageGeneralPlanCommon.LayerName_Drai_Out);
                DrawUtils.ShowGeometry(ThDrainageGeneralPlanCommon.Drai_Out, ThDrainageGeneralPlanCommon.LayerName_Drai_Out);

                ThDrainageGeneralPlanCommon.Rain_Main = ThDrainageGeneralPlanPreService.ExtractMainPiPe(acadDatabase.Database, ThDrainageGeneralPlanCommon.LayerName_Rain_Main);
                DrawUtils.ShowGeometry(ThDrainageGeneralPlanCommon.Rain_Main, ThDrainageGeneralPlanCommon.LayerName_Rain_Main);

                ThDrainageGeneralPlanCommon.Rain_Out = ThDrainageGeneralPlanPreService.ExtractOutPiPe(acadDatabase.Database, ThDrainageGeneralPlanCommon.LayerName_Rain_Out);
                DrawUtils.ShowGeometry(ThDrainageGeneralPlanCommon.Rain_Out, ThDrainageGeneralPlanCommon.LayerName_Rain_Out);

                //ThDrainageGeneralPlanDataService.Group(ThDrainageGeneralPlanCommon.Drai_Main, ThDrainageGeneralPlanCommon.Drai_Out);
            }
        }

        
        //析构 释放对象 无操作
        public void Dispose()
        {
        }
    }
}
