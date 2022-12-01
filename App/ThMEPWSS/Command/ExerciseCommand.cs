using AcHelper;
using HandyControl.Controls;
using Linq2Acad;
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
        public ExerciseViewmodel Viewmodel { get; set; }
        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                Active.Editor.WriteLine(Viewmodel.ReadText);
            }

        }
    }
}
