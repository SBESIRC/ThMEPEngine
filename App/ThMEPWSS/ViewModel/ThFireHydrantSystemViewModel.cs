using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using static ThMEPWSS.UndergroundFireHydrantSystem.Model.Block;

namespace ThMEPWSS.ViewModel
{
    public class ThFireHydrantSystemViewModel
    {
        public static void InsertLoopMark()
        {
            using (Active.Document.LockDocument())
            using (var acadDatabase = AcadDatabase.Active())  //要插入图纸的空间
            {
                WaterSuplyUtils.ImportNecessaryBlocks();
                var opt = new PromptPointOptions("请指定环管标记插入点");
                var pt = Active.Editor.GetPoint(opt);
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-NOTE", WaterSuplyBlockNames.LoopMark,
                                pt.Value, new Scale3d(1, 1, 1), 0);
            }
        }

        public static void InsertSubLoopMark()
        {
            using (Active.Document.LockDocument())
            using (var acadDatabase = AcadDatabase.Active())  //要插入图纸的空间
            {
                WaterSuplyUtils.ImportNecessaryBlocks();
                var opt = new PromptPointOptions("请指定环管节点标记插入点");
                var pt = Active.Editor.GetPoint(opt);
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-NOTE", WaterSuplyBlockNames.SubLoopMark,
                                pt.Value, new Scale3d(1, 1, 1), 0);
            }
        }
    }
}
