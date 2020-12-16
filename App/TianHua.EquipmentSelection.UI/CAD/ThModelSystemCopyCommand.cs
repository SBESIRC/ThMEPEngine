using System;
using AcHelper;
using Linq2Acad;
using AcHelper.Commands;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThModelSystemCopyCommand : ThModelCommand, IDisposable
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

                // 复制风机系统
                foreach (var item in ThFanSelectionService.Instance.ModelMapping)
                {
                    ThFanSelectionEngine.CloneModels(item.Key, item.Value);
                }
            }
        }
    }
}
