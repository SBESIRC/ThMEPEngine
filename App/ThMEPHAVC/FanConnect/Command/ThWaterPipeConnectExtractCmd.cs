using System;
using ThMEPEngineCore.Command;
using ThMEPHVAC.FanConnect.Service;
using ThMEPHVAC.FanConnect.ViewModel;

namespace ThMEPHVAC.FanConnect.Command
{
    public class ThWaterPipeConnectExtractCmd : ThMEPBaseCommand, IDisposable
    {
        public ThWaterPipeConfigInfo ConfigInfo { set; get; }//界面输入信息
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        public override void SubExecute()
        {
            //获取范围
            var area = ThFanConnectUtils.SelectArea();
            //获取水管数据
            var waterPipes = ThPipeExtractServiece.GetPipeTreeModel(area);
            //扩展管路
            ThWaterPipeExtendServiece pipeExtendServiece = new ThWaterPipeExtendServiece();
            pipeExtendServiece.ConfigInfo = ConfigInfo;
            pipeExtendServiece.PipeExtend(waterPipes);

        }
    }
}
