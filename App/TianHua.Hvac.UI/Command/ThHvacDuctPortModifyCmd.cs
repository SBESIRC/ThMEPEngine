using System;
using System.Linq;
using System.Windows.Forms;
using AcHelper;
using AcHelper.Commands;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using ThMEPHVAC.Model;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacDuctPortModifyCmd : IAcadCommand, IDisposable
    {
        public void Dispose() { }

        public void Execute()
        {
            using (var db = AcadDatabase.Active())
            {
                //var ids = Get_center_line("选择要修改的管段", out Duct_modify_param param);
                //var dlg = new fmDuctModify(param.air_volume, param.duct_size);
                //if (AcadApp.ShowModalDialog(dlg) != DialogResult.OK)
                    //return;

                //var modify = new ThDuctPortsReDraw(ids, dlg.duct_size, dlg.air_volume, );

            }
        }
        //private ObjectId[] Get_center_line(string prompt, out Duct_modify_param param)
        //{
        //    PromptSelectionOptions options = new PromptSelectionOptions()
        //    {
        //        AllowDuplicates = false,
        //        MessageForAdding = prompt,
        //        RejectObjectsOnLockedLayers = true,
        //        AllowSubSelections = false,
        //    };
        //    var result = Active.Editor.GetSelection(options);

        //    if (result.Status == PromptStatus.OK)
        //    {
        //        var objIds = result.Value.GetObjectIds();
        //        var list = Get_value_list(objIds);
        //        param = Get_param(list);
        //        return objIds;
        //    }
        //    else
        //    {
        //        param = new Duct_modify_param();
        //        return null;
        //    }
        //}
        private Duct_modify_param Get_param(TypedValueList list, out ObjectId start_id)
        {
            start_id = ObjectId.Null;
            var param = new Duct_modify_param();
            var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
            if (!values.Any())
                return param;
            //start_id = values.ElementAt(0).Value;
            param.air_volume = Double.Parse((string)values.ElementAt(2).Value);
            param.duct_size = (string)values.ElementAt(3).Value;
            return param;
        }
        private TypedValueList Get_value_list(ObjectId[] obj_ids)
        {
            TypedValueList list = new TypedValueList();
            foreach (var id in obj_ids)
            {
                var g_ids = id.GetGroups();
                foreach (var g_id in g_ids)
                {
                    list = g_id.GetXData("Duct");
                    if (list == null)
                        continue;
                    break;
                }
                if (list.Count != 0)
                    break;
            }
            return list;
        }
    }
}
