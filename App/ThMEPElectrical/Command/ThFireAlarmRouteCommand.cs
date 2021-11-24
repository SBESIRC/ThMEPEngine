﻿using AcHelper.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Service;
using ThMEPEngineCore.ConnectWiring;
using ThMEPEngineCore.ConnectWiring.Service;

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
            BlockConfigSrervice configSrervice = new BlockConfigSrervice();
            var configInfo = configSrervice.GetLoopInfo("火灾报警");

            foreach (var config in configInfo)
            {
                foreach (var info in config.loopInfoModels)
                {
                    var parameter = ThElectricalUIService.Instance.fireAlarmParameter.Where(x => x.loopType == info.LineContent).FirstOrDefault();
                    info.LineType = parameter.layerType;
                    info.PointNum = int.TryParse(parameter.pointNum, out int num) ? num : int.MaxValue;
                }
            }

            ConnectWiringService connectWiringService = new ConnectWiringService();
            connectWiringService.Routing(configInfo);
        }
    }
}
