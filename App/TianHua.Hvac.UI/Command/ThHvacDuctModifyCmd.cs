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
            var objIds = Get_modify_duct_id("选择要修改的管段");
            if (objIds == null || objIds.Length == 0)
                return;
            var type = ThDuctPortsInterpreter.Get_entity_type(objIds);
            if (type == "Duct" || type == "Vertical_bypass")
            {
                var ids = Get_center_line(objIds, out DuctModifyParam param);
                if (ids == null || ids.Length == 0)
                    return;
                var dlg = new fmDuctModify(param.air_volume, param.duct_size);
                if (AcadApp.ShowModalDialog(dlg) != DialogResult.OK)
                    return;
                if (type == "Duct")
                {
                    _ = new ThDuctPortsModifyDuct(ids, dlg.duct_size, param);
                }
                else
                {
                    new ThFanModifyVBypass(ids, dlg.duct_size, param);
                }
            }
            else
                ThMEPHVACService.Prompt_msg("请选择管段");
        }
        private ObjectId[] Get_modify_duct_id(string prompt)
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
        private ObjectId[] Get_center_line(ObjectId[] objIds, out DuctModifyParam param)
        {
            param = new DuctModifyParam();
            var list = ThDuctPortsInterpreter.Get_value_list(objIds);
            if (list == null)
            {
                ThMEPHVACService.Prompt_msg("请使用最新管道生成工具生成XData");
                return null;
            }
            var groupId = ThDuctPortsReadComponent.GetGroupIdsBySubEntityId(objIds[0]);
            param = ThHvacAnalysisComponent.AnayDuctparam(list, groupId.Handle, groupId.Database);
            if (param.type == "")
            {
                ThMEPHVACService.Prompt_msg("该管段未包含XData");
                return null;
            }
            return objIds;
        }
    }
}
