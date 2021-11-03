using System;
using AcHelper;
using Linq2Acad;
using ThMEPEngineCore.Command;
using Autodesk.AutoCAD.EditorInput;
using ThMEPHVAC.LoadCalculation.Service;
using ThMEPHVAC.LoadCalculation.Model;

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
            using (var database = AcadDatabase.Active())
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
                    InsertBlockService.InsertRoomFunctionBlock(LoadCalculationParameterFromConfig.DefaultRoomNumber, roomFunctionName, ppr.Value);
                }
            }
        }
    }
}
