using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Command;
using ThMEPWSS.Diagram.ViewModel;

namespace ThMEPWSS.Command
{
    public class ExerciseCommand : ThMEPBaseCommand, IDisposable
    {
        public ExerciseCommand(ExerciseViewmodel viewModel = null)
        {
            ActionName = "生成";
            CommandName = "THDXPSXTT";
            Viewmodel = viewModel;
        }
        public static ExerciseViewmodel Viewmodel { get; set; }
        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            MessageBox.Show(Viewmodel.ReadText);
        }
    }
}
