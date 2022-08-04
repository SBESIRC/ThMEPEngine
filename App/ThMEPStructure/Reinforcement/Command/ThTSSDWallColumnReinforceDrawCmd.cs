using System;
using AcHelper.Commands;
using ThMEPEngineCore.Command;

namespace ThMEPStructure.Reinforcement.Command
{
    /// <summary>
    /// 绘制标准墙柱边缘构件
    /// </summary>
    public class ThTSSDWallColumnReinforceDrawCmd : ThMEPBaseCommand, IDisposable
    {
        public ThTSSDWallColumnReinforceDrawCmd()
        {
            ActionName = "边缘构件绘制";
            CommandName = "THQZPJ1";            
        }

        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            // 启动探索者 
            CommandHandlerBase.ExecuteFromCommandLine(false, "AABYGJHZ");
        }
    }
}
