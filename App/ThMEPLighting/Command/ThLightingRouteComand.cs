using System;
using AcHelper;
using System.Linq;
using AcHelper.Commands;
using Autodesk.AutoCAD.EditorInput;
using ThMEPEngineCore.ConnectWiring;
using ThMEPEngineCore.ConnectWiring.Service;
using ThMEPEngineCore.Command;

namespace ThMEPLighting.Command
{
    public class ThLightingRouteComand : ThMEPBaseCommand, IDisposable
    {
        public ThLightingRouteComand()
        {
            CommandName = "THZMLX";
            ActionName = "照明连线";
        }


        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
#if (ACAD2016 || ACAD2018)
            BlockConfigSrervice configSrervice = new BlockConfigSrervice();
            var configInfo = configSrervice.GetLoopInfo("照明");

            foreach (var config in configInfo)
            {
                foreach (var info in config.loopInfoModels)
                {
                    var parameter = ThMEPLightingService.Instance.Parameter.Where(x => x.loopType == info.LineContent).FirstOrDefault();
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
