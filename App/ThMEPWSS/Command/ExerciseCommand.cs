using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using HandyControl.Controls;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.ExerciseProj;

namespace ThMEPWSS.Command
{
    public class ExerciseCommand : ThMEPBaseCommand, IDisposable
    {
        public ExerciseCommand(ExerciseViewmodel viewModel = null)
        {
            ActionName = "生成";
            CommandName = "THOOOOOOOO";
            Viewmodel = viewModel;
        }
        public ExerciseViewmodel Viewmodel { get; set; }
        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            //读取数据
            ExerciseService exerciseService=new ExerciseService();
            exerciseService.Process();

        }



    }

}
