using System;
using ThMEPEngineCore.Command;

namespace TianHua.Structure.WPF.UI.Command
{
    public class WallColumnReinforceSetCmd : ThMEPBaseCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public WallColumnReinforceSetCmd()
        {
            this.ActionName = "墙柱配筋设置";
            this.CommandName = "THQZCSSZ";
        }

        public override void SubExecute()
        {
        }
    }
}
