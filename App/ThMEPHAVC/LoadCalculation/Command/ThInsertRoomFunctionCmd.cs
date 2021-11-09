using System;
using AcHelper;
using Linq2Acad;
using ThMEPEngineCore.Command;
using Autodesk.AutoCAD.EditorInput;
using ThMEPHVAC.LoadCalculation.Service;
using ThMEPHVAC.LoadCalculation.Model;
using GeometryExtensions;

namespace ThMEPHVAC.LoadCalculation.Command
{
    public class ThInsertRoomFunctionCmd : ThMEPBaseCommand, IDisposable
    {
        public ThInsertRoomFunctionCmd()
        {
            this.CommandName = "THFJBHCR";
            this.ActionName = "暖通房间块布置";
        }
        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            string roomFunctionName = ThLoadCalculationUIService.Instance.Parameter.RoomFunctionName;
            while (true)
            {
                //选择插入点
                var ppr = Active.Editor.GetPoint("\n请选择暖通房间图块插入点位");
                if (ppr.Status != PromptStatus.OK)
                {
                    return;
                }
                using (var database = AcadDatabase.Active())
                {
                    //初始化图纸(导入图层/图块/图层三板斧等)
                    InsertBlockService.initialization();

                    InsertBlockService.InsertRoomFunctionBlock(LoadCalculationParameterFromConfig.DefaultRoomNumber, roomFunctionName, ppr.Value.TransformBy(Active.Editor.UCS2WCS()));
                }                
            }
        }
    }
}
