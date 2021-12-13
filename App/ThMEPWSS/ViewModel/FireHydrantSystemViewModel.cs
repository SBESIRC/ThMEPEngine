using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using ThCADExtension;

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
                using (var acadDatabase = AcadDatabase.Active())  //要插入图纸的空间
                using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))//引用模块的位置
                {
                    if(!acadDatabase.Blocks.Contains("消火栓环管标记"))
                    {
                        acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault("消火栓环管标记"));
                        acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault("W-FRPT-NOTE"));
                    }
                }
                while (true)
                {
                    var opt = new PromptPointOptions("\n请指定环管标记插入点");
                    var pt = Active.Editor.GetPoint(opt);
                    if (pt.Status != PromptStatus.OK)
                    {
                        break;
                    }
                    
                    using (var acadDatabase = AcadDatabase.Active())  //要插入图纸的空间
                    {
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-NOTE", "消火栓环管标记",
                                    pt.Value.Ucs2Wcs(), new Scale3d(1, 1, 1), 0);
                    }
                }
            }
        }

        public static void InsertSubLoopMark()
        {
            Common.Utils.FocusMainWindow();
            using (Active.Document.LockDocument())
            {
                using (var acadDatabase = AcadDatabase.Active())  //要插入图纸的空间
                using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))//引用模块的位置
                {
                    if (!acadDatabase.Blocks.Contains("消火栓环管节点标记-2"))
                    {
                        acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault("消火栓环管节点标记-2"));
                        acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault("W-FRPT-NOTE"));
                    }
                }
                while (true)
                {
                    var opt = new PromptPointOptions("\n请指定环管节点标记插入点");
                    var pt = Active.Editor.GetPoint(opt);
                    if (pt.Status != PromptStatus.OK)
                    {
                        break;
                    }
                    using (var acadDatabase = AcadDatabase.Active())  //要插入图纸的空间
                    {
                        var valueDic = new Dictionary<string, string>();
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-NOTE", "消火栓环管节点标记-2",
                                    pt.Value.Ucs2Wcs(), new Scale3d(1, 1, 1), 0);
                    }
                }
            }
        }
    }
}
