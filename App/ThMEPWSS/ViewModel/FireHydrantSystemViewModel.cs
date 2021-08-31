using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System.Collections.Generic;

namespace ThMEPWSS.ViewModel
{
    public class FireHydrantSystemViewModel
    {
        public FireHydrantSystemViewModel()
        {

        }
        public FireHydrantSystemSetViewModel SetViewModel { get; set; } = new FireHydrantSystemSetViewModel();
        public static void InsertLoopMark()
        {
            Common.Utils.FocusMainWindow();
            using (Active.Document.LockDocument())
            {
                Pipe.Service.ThRainSystemService.ImportElementsFromStdDwg();
                while (true)
                {
                    var opt = new PromptPointOptions("请指定环管标记插入点: \n");
                    var pt = Active.Editor.GetPoint(opt);
                    if (pt.Status != PromptStatus.OK)
                    {
                        break;
                    }
                    using (var acadDatabase = AcadDatabase.Active())  //要插入图纸的空间
                    {
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-NOTE", "消火栓环管标记",
                                    pt.Value, new Scale3d(1, 1, 1), 0);
                    }
                }
            }
        }

        public static void InsertSubLoopMark()
        {
            Common.Utils.FocusMainWindow();
            using (Active.Document.LockDocument())
            {
                Pipe.Service.ThRainSystemService.ImportElementsFromStdDwg();
                while (true)
                {
                    var opt = new PromptPointOptions("请指定环管节点标记插入点: \n");
                    var pt = Active.Editor.GetPoint(opt);
                    if (pt.Status != PromptStatus.OK)
                    {
                        break;
                    }
                    using (var acadDatabase = AcadDatabase.Active())  //要插入图纸的空间
                    {
                        var valueDic = new Dictionary<string, string>();
                        valueDic.Add("节点1", "A");
                        valueDic.Add("节点2", "A'");
                        var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-NOTE", "消火栓环管节点标记",
                                    pt.Value, new Scale3d(1, 1, 1), 0, valueDic);
                    }
                }
            }
        }
    }
}
