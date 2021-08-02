using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using static ThMEPWSS.UndergroundFireHydrantSystem.Model.Block;

namespace ThMEPWSS.ViewModel
{
    public class ThFireHydrantSystemViewModel
    {
        public static void InsertLoopMark()
        {
            Common.Utils.FocusMainWindow();
            using (Active.Document.LockDocument())
            {
                //WaterSuplyUtils.ImportNecessaryBlocks();
                ThMEPWSS.Pipe.Service.ThRainSystemService.ImportElementsFromStdDwg();
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
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-NOTE", WaterSuplyBlockNames.LoopMark,
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
                //WaterSuplyUtils.ImportNecessaryBlocks();
                ThMEPWSS.Pipe.Service.ThRainSystemService.ImportElementsFromStdDwg();
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
                        var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-NOTE", WaterSuplyBlockNames.SubLoopMark,
                                    pt.Value, new Scale3d(1, 1, 1), 0, valueDic);
                    }
                }
            }
        }
    }
}
