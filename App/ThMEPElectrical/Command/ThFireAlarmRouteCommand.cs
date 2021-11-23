using AcHelper.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.ConnectWiring;

namespace ThMEPElectrical.Command
{
    public class ThFireAlarmRouteCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public void Execute()
        {
            ConnectWiringService connectWiringService = new ConnectWiringService();
            connectWiringService.Routing(20, "火灾报警");
        }
    }
}
