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
    public class ThAFASRouteCommand : ThMEPBaseCommand, IDisposable
    {
        public ThAFASRouteCommand()
        {
            CommandName = "THHZLX";
            ActionName = "火灾报警连线";
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
#if (ACAD2016 || ACAD2018)
            BlockConfigSrervice configSrervice = new BlockConfigSrervice();
            var configInfo = configSrervice.GetLoopInfo("火灾报警");

            foreach (var config in configInfo)
            {
                foreach (var info in config.loopInfoModels)
                {
                    var parameter = ThMEPLightingService.Instance.AFASParameter.Where(x => x.loopType == info.LineContent).FirstOrDefault();
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
