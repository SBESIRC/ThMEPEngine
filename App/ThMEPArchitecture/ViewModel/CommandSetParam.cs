using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Command;

namespace ThMEPArchitecture.ViewModel
{
    public class CommandSetParamCmd : ThMEPBaseCommand, IDisposable
    {
        public CommandSetParamCmd()//debug 读取基因直排
        {
            CommandName = "-THCWBZTC";
        }
        public override void SubExecute()
        {
            try
            {
                var msg = Active.Editor.GetInteger("\n 请选择要开启的log：0-都开启，1-仅主进程，2-都关闭:");
                if (msg.Status != PromptStatus.OK) return;
                if (msg.Value == 0)
                {
                    ParameterStock.LogMainProcess = true;
                    ParameterStock.LogSubProcess = true;
                }
                else if (msg.Value == 1)
                {
                    ParameterStock.LogMainProcess = true;
                    ParameterStock.LogSubProcess = false;
                }
                else if (msg.Value == 2)
                {
                    ParameterStock.LogMainProcess = false;
                    ParameterStock.LogSubProcess = false;
                }
                else
                {
                    throw new Exception("无该选项!");
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }

        public override void AfterExecute()
        {
            Active.Editor.WriteMessage($"seconds: {_stopwatch.Elapsed.TotalSeconds} \n");
            base.AfterExecute();
        }
        public void Dispose()
        {
        }
    }
}
