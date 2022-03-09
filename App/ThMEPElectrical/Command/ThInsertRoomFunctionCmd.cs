using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.ElectricalLoadCalculation;
using ThMEPEngineCore.Command;

namespace ThMEPElectrical.Command
{
    public class ThInsertRoomFunctionCmd : ThMEPBaseCommand, IDisposable
    {
        public ThInsertRoomFunctionCmd()
        {
            this.CommandName = "THCRFJGNBZ";
            this.ActionName = "插入房间功能标注";
        }
        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            string roomFunctionName = ElectricalLoadCalculationConfig.RoomFunctionName;
            while (true)
            {
                //选择插入点
                var ppr = Active.Editor.GetPoint("\n请选择电气房间图块插入点位");
                if (ppr.Status != PromptStatus.OK)
                {
                    return;
                }
                using (var database = AcadDatabase.Active())
                {
                    //初始化图纸(导入图层/图块/图层三板斧等)
                    ElectricalLoadCalculationService.initialization();

                    ElectricalLoadCalculationService.InsertRoomFunctionBlock(ElectricalLoadCalculationConfig.DefaultRoomNumber, roomFunctionName, ppr.Value.TransformBy(Active.Editor.UCS2WCS()));
                }
            }
        }
    }
}
