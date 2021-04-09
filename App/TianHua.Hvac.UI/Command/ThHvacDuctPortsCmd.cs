using System;
using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.LaneLine;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacDuctPortsCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            DBObjectCollection lineobjs = get_centerline();
            if (lineobjs.Count == 0)
                return;

        }

        private ObjectIdCollection get_from_prompt(string prompt, bool only_able)
        {
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = prompt,
                RejectObjectsOnLockedLayers = true,
                SingleOnly = only_able
            };
            var result = Active.Editor.GetSelection(options);

            if (result.Status == PromptStatus.OK)
            {
                return result.Value.GetObjectIds().ToObjectIdCollection();
            }
            else
            {
                return new ObjectIdCollection();
            }
        }

        private DBObjectCollection get_centerline()
        {
            var objIds = get_from_prompt("请选择中心线", false);
            if (objIds.Count == 0)
                return new DBObjectCollection();
            var tmp = new DBObjectCollection();
            var center_lines = new DBObjectCollection();

            ThLaneLineSimplifier.RemoveDangles(tmp, 100.0).ForEach(l => center_lines.Add(l));
            return center_lines;
        }
    }
}
