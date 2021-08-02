using System;
using System.Windows.Forms;
using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using ThMEPHVAC.Model;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacDuctModifyCmd : IAcadCommand, IDisposable
    {
        public void Dispose() { }

        public void Execute()
        {
            var ids = Get_center_line("选择要修改的管段", out Duct_modify_param param);
            if (ids == null)
                return;
            var dlg = new fmDuctModify(param.air_volume, param.duct_size);
            if (AcadApp.ShowModalDialog(dlg) != DialogResult.OK)
                return;
            string duct_size = dlg.duct_size;
            _ = new ThDuctPortsModifyDuct(ids, duct_size, param);
        }
        private ObjectId[] Get_center_line(string prompt, out Duct_modify_param param)
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
            param = new Duct_modify_param();
            if (result.Status == PromptStatus.OK)
            {
                var objIds = result.Value.GetObjectIds();
                var list = ThDuctPortsInterpreter.Get_value_list(objIds);
                if (list == null)
                {
                    ThDuctPortsService.Prompt_msg("请使用最新管道生成工具生成XData");
                    return null;
                }
                var groupId = ThDuctPortsReadComponent.GetGroupIdsBySubEntityId(objIds[0]);
                param = ThDuctPortsInterpreter.Get_duct_param(list, groupId.Handle);
                return objIds;
            }
            else
                return null;
        }
    }
}
