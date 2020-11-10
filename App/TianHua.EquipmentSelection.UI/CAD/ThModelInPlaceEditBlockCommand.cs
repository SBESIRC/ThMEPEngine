using System;
using AcHelper;
using Linq2Acad;
using AcHelper.Commands;
using TianHua.Publics.BaseCode;
using Autodesk.AutoCAD.EditorInput;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThModelInPlaceEditBlockCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            using (Active.Document.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                // set focus to AutoCAD
                //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
                    Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
                Active.Document.Window.Focus();
#endif

                // 获取风机参数
                var _FanDataModel = ThFanSelectionService.Instance.Model;
                if (_FanDataModel == null)
                {
                    return;
                }

                // 在位编辑风机
                ThFanSelectionEngine.EditModelsInplace(_FanDataModel);

                // 清除风机参数
                ThFanSelectionService.Instance.Model = null;
            }
        }
    }
}
