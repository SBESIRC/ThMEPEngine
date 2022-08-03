using AcHelper.Commands;
using DotNetARX;
using System;
using ThMEPEngineCore.Command;
using ThMEPStructure.Reinforcement.Service;

namespace ThMEPStructure.Reinforcement.Command
{
    /// <summary>
    /// 绘制标准墙柱边缘构件
    /// </summary>
    public class ThTSSDWallColumnReinforceDrawCmd : ThMEPBaseCommand, IDisposable
    {
        public ThTSSDWallColumnReinforceDrawCmd()
        {
            ActionName = "生成柱表";
            CommandName = "THQZPJ1";            
        }

        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            // Load TSDP Profile
            AcHelper.Active.Document.SendCommand("TSPL ");

            // 启动探索者 
            CommandHandlerBase.ExecuteFromCommandLine(false, "AABYGJHZ");
        }
    }
}
