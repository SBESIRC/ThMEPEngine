using AcHelper;
using Linq2Acad;
using ThMEPHAVC.Duct;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHAVC
{
    public class ThMEPHAVCApp : IExtensionApplication
    {
        public void Initialize()
        {
        }

        public void Terminate()
        {
        }

        [CommandMethod("TIANHUACAD", "THDuctGraph", CommandFlags.Modal)]
        public void THDuctGraph()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var lines = new DBObjectCollection();
                var entsresult = Active.Editor.GetSelection();
                if (entsresult.Status != PromptStatus.OK)
                {
                    return;
                }
                foreach (var item in entsresult.Value.GetObjectIds())
                {
                    lines.Add(acadDatabase.Element<Entity>(item));
                }

                var pointresult = Active.Editor.GetPoint("\n选则线路起点");
                if (pointresult.Status != PromptStatus.OK)
                {
                    return;
                }

                ThDuctGraphEngine ductGraphEngine = new ThDuctGraphEngine();
                ductGraphEngine.BuildGraph(lines, pointresult.Value);
            }
        }
    }
}
