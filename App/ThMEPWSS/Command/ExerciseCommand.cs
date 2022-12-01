using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
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
                var bound = Viewmodel.SelectedBound;
                if (bound == null)
                    return;
                //var UnderFloor=bound.BlockName.Where(e => e.Equals("")
                var ents = acadDatabase.ModelSpace.OfType<Entity>().ToList();//取出cad数据库的所有元素的列表A
                var ents_in_block = ents.Where(e =>
                {
                    var cond_matchlayer = e.Layer.Equals("W-FRPT-DRAI-PIPE");
                    cond_matchlayer = cond_matchlayer || (e.Layer.Contains("W-") && e.Layer.Contains("-DRAI-") && e.Layer.Contains("-PIPE"));
                    return cond_matchlayer;
                }).ToList();//在列表A里面找到名字是block的图层的列表



            }

        }
    }

}
