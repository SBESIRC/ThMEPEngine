using System;
using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacPortModifyCmd : IAcadCommand, IDisposable
    {
        public void Dispose() { }

        public void Execute()
        {
            var ids = Get_start_node("选择起始节点");
            if (ids == null)
                return;
        }
        private ObjectId[] Get_start_node(string prompt)
        {
            var options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = prompt,
                RejectObjectsOnLockedLayers = true,
                AllowSubSelections = false,
            };
            var result = Active.Editor.GetSelection(options);
            return (result.Status == PromptStatus.OK) ? result.Value.GetObjectIds() : null;
        }
    }
}
