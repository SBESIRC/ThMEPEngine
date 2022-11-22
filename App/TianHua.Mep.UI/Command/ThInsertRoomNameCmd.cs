using System;
using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using ThMEPEngineCore.Command;
using TianHua.Mep.UI.UI;
using TianHua.Mep.UI.ViewModel;
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Mep.UI.Command
{
    public class ThInsertRoomNameCmd : ThMEPBaseCommand, IDisposable
    {
        private static InsertRoomNameUI _uiInsertRoom;
        public ThInsertRoomNameCmd()
        {
            ActionName = "提取或插入房间名称";
            CommandName = "THFJMCTQ";
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            var options = new PromptKeywordOptions("\n选择处理方式");
            options.Keywords.Add("提取", "E", "提取(E)");
            options.Keywords.Add("手动插入", "S", "手动插入(S)");
            options.Keywords.Default = "提取";
            var result = Active.Editor.GetKeywords(options);
            if (result.Status != PromptStatus.OK)
            {
                return;
            }
            if(result.StringResult== "提取")
            {
                var vm = new ThInsertRoomNameVM();
                vm.Extract();
            }
            else
            {
                ShowUI(new ThInsertRoomNameVM());
            }
        }

        private void ShowUI(ThInsertRoomNameVM vm)
        {
            if (vm == null)
            {
                return;
            }
            if (_uiInsertRoom != null && _uiInsertRoom.IsLoaded)
            {
                _uiInsertRoom.UpdateDataContext(vm);
            }
            else
            {
                _uiInsertRoom = new InsertRoomNameUI(vm);
                acadApp.ShowModelessWindow(acadApp.MainWindow.Handle, _uiInsertRoom,false);
            }
        }

        private void CloseUI()
        {
            if (_uiInsertRoom != null && _uiInsertRoom.IsLoaded)
                _uiInsertRoom.Close();
        }
    }
}
