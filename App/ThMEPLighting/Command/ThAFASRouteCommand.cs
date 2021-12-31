using System;
using AcHelper;
using System.Linq;
using AcHelper.Commands;
using Autodesk.AutoCAD.EditorInput;
using ThMEPEngineCore.ConnectWiring;
using ThMEPEngineCore.ConnectWiring.Service;
using ThMEPEngineCore.Command;
using System.Collections.Generic;
using ThMEPEngineCore.ConnectWiring.Model;

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
            List<WiringLoopModel> configFilter = new List<WiringLoopModel>();
            foreach (var config in configInfo)
            {
                WiringLoopModel model = new WiringLoopModel();
                foreach (var info in config.loopInfoModels)
                {
                    var parameter = ThMEPLightingService.Instance.AFASParameter.Where(x => x.loopType == info.LineContent).FirstOrDefault();
                    if(parameter.IsNull())
                    {
                        continue;
                    }
                    info.LineType = parameter.layerType;
                    info.PointNum = int.TryParse(parameter.pointNum, out int num) ? num : int.MaxValue;
                    model.loopInfoModels.Add(info);
                }
                if(model.loopInfoModels.Count > 0)
                {
                    configFilter.Add(model);
                }
            }
            ConnectWiringService connectWiringService = new ConnectWiringService();
            connectWiringService.Routing(configFilter, false, ThMEPLightingService.Instance.AvoidColumnChecked);
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }
    }
}
