using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Command;

namespace ThMEPHVAC.Command
{
    class SmokeProofSystemCmd : ThMEPBaseCommand, IDisposable
    {
        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public override void SubExecute()
        {
            //SmokeCalculateUI
        }
    }
}
