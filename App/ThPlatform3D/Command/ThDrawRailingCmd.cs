using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Command;

namespace ThPlatform3D.Command
{
    public class ThDrawRailingCmd : ThMEPBaseCommand
    {
        public ThDrawRailingCmd()
        {
            CommandName = "THDrawRailing";
            ActionName = "绘制栏杆线";
        }

        public override void SubExecute()
        {
            throw new NotImplementedException();
        }
    }
}
