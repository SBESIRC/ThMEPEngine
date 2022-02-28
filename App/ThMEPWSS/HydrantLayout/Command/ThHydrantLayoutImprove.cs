using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Command;

namespace ThMEPWSS.HydrantLayout.Command
{
    public class ThHydrantLayoutImproveCmd:ThMEPBaseCommand
    {
        public Dictionary<string, List<string>> BlockNameDict { get; set; } = new Dictionary<string, List<string>>();
        public ThHydrantLayoutImproveCmd()
        {
            ActionName = "优化布置";
            CommandName = "THXHSYH";
        }

        public override void SubExecute()
        {
            HydrantLayoutImproveExecute();
        }

        public void HydrantLayoutImproveExecute()
        {

        }
    }
}
