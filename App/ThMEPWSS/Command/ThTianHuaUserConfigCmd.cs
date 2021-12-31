using System;
using AcHelper;
using Linq2Acad;
using Autodesk.AutoCAD.ApplicationServices;
using ThMEPWSS.ViewModel;
using ThMEPEngineCore.Command;

namespace ThMEPWSS.Command
{
    public class ThTianHuaUserConfigCmd : ThMEPBaseCommand, IDisposable
    {
        private ThTianHuaUserConfigVM VM { get; set; }
        public ThTianHuaUserConfigCmd(ThTianHuaUserConfigVM vm)
        {
            CommandName = "THUC";
            ActionName = "参数配置";
            this.VM = vm;
        }
        public void Dispose()
        {
        }
       
        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var acadDb =  AcadDatabase.Active())
            {
                if(VM.BeamSourceSwitch== "协同")
                {
                    Application.SetSystemVariable("USERR1", 0);
                }
                else
                {
                    Application.SetSystemVariable("USERR1", 1);
                }
            }
        }
    }
}
