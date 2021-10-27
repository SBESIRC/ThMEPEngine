using System;
using AcHelper;
using Linq2Acad;
using ThMEPEngineCore.Command;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using ThMEPHVAC.LoadCalculation.Model;
using ThMEPHVAC.LoadCalculation.Service;

namespace ThMEPHVAC.LoadCalculation.Command
{
    public class ThInsertRoomFunctionCmd : ThMEPBaseCommand, IDisposable
    {
        public ThInsertRoomFunctionCmd()
        {
            this.CommandName = "THNTFJ";
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
                //选择插入点
                var ppr = Active.Editor.GetPoint("\n请选择暖通房间图块插入点位");
                if (ppr.Status != PromptStatus.OK)
                {
                    return;
                }
                string roomFunctionName = ThLoadCalculationUIService.Instance.Parameter.RoomFunctionName;
                ImportRoomFunctionBlock(roomFunctionName, ppr.Value);
            }
        }

        private void ImportRoomFunctionBlock(string roomFunctionName, Point3d value)
        {
            InsertBlockService.InsertSpecifyBlock(roomFunctionName, value);
        }
    }
}
