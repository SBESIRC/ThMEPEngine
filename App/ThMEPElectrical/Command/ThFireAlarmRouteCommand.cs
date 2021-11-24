﻿using System;
using AcHelper;
using Linq2Acad;
using System.Linq;
using AcHelper.Commands;
using ThMEPElectrical.Service;
using ThMEPEngineCore.ConnectWiring;
using ThMEPEngineCore.ConnectWiring.Service;

namespace ThMEPElectrical.Command
{
    public class ThFireAlarmRouteCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
        }

        public void Execute()
        {
#if (ACAD2016 || ACAD2018)
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
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }
    }
}
