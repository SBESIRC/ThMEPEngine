using System;
using System.Windows.Forms;
using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Hvac;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.Model;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacDuctModifyCmd : IAcadCommand, IDisposable
    {
        public void Dispose() { }

        public void Execute()
        {
            var objIds = GetModifyDuctId("选择要修改的管段");
            if (objIds == null || objIds.Length == 0)
                return;
            var type = ThDuctPortsInterpreter.GetEntityType(objIds);
            if (type == "Duct" || type == "Vertical_bypass")
            {
                if (objIds == null || objIds.Length == 0)
                    return;
                GetDuctParam(objIds, out DuctModifyParam curDuctParam);
                var dlg = new fmDuctModify(curDuctParam.airVolume, curDuctParam.ductSize);
                if (AcadApp.ShowModalDialog(dlg) != DialogResult.OK)
                    return;
                if (type == "Duct")
                {
                    new ThDuctPortsModifyDuct(dlg.ductSize, objIds, curDuctParam);
                }
                else
                    new ThFanModifyVBypass(dlg.ductSize, objIds, curDuctParam);
            }
            else
                ThMEPHVACService.PromptMsg("请选择管段");
        }
        private ObjectId[] GetModifyDuctId(string prompt)
        {
            var options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = prompt,
                RejectObjectsOnLockedLayers = true,
                AllowSubSelections = false,
                SingleOnly = true
            };
            var result = Active.Editor.GetSelection(options);
            if (result.Status == PromptStatus.OK)
            {
                return result.Value.GetObjectIds();
            }
            return null;
        }
        private void GetDuctParam(ObjectId[] objIds, out DuctModifyParam param)
        {
            var list = ThDuctPortsInterpreter.GetValueList(objIds);
            if (list == null)
            {
                ThMEPHVACService.PromptMsg("请使用最新管道生成工具生成XData");
            }
            var groupId = ThDuctPortsReadComponent.GetGroupIdsBySubEntityId(objIds[0]);
            param = ThHvacAnalysisComponent.AnayDuctparam(list, groupId);
            if (param.type == "")
            {
                ThMEPHVACService.PromptMsg("该管段未包含XData");
            }
        }
    }
}
