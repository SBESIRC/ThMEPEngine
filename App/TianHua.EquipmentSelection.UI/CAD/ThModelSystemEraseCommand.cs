using System;
using AcHelper;
using Linq2Acad;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThModelSystemEraseCommand : ThModelCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public override void Execute()
        {
            using (Active.Document.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 切换焦点到CAD
                SetFocusToDwgView();

                // 删除风机系统
                ThFanSelectionService.Instance.ErasedModels.ForEach(o => ThFanSelectionEngine.RemoveModels(o, true));
                ThFanSelectionService.Instance.UnerasedModels.ForEach(o => ThFanSelectionEngine.RemoveModels(o, false));
            }
        }
    }
}
