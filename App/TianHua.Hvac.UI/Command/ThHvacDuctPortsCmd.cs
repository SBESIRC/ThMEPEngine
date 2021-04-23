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
            DBObjectCollection center_lines = get_lines_from_prompt("请选择中心线", false);
            if (center_lines.Count == 0)
                return;
            DBObjectCollection start_line = get_lines_from_prompt("请选择起始线", true);
            if (start_line.Count == 0)
                return;
            center_lines.Remove(start_line[0]);
            //只有一根线不做处理
            if (center_lines.Count == 0)
                return;

        }

        private DBObjectCollection get_lines_from_prompt(string prompt, bool only_able)
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
                var objIds = result.Value.GetObjectIds().ToObjectIdCollection();
                if (objIds.Count == 0)
                    return new DBObjectCollection();
                var tmp = new DBObjectCollection();
                var lines = new DBObjectCollection();
                ThLaneLineSimplifier.RemoveDangles(tmp, 100.0).ForEach(l => lines.Add(l));
                return lines;
            }
            else
            {
                return new DBObjectCollection();
            }
        }

    }
}
