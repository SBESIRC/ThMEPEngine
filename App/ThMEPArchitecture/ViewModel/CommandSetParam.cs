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
                ParameterStock.ReadHiddenParameter = true;
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
