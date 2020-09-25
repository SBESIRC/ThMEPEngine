using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using NFox.Cad;
using System;
using System.Collections.Generic;
using Linq2Acad;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace ThMEPElectrical.Business
{
    /// <summary>
    /// 用户手选图元提取器
    /// </summary>
    public class EntityPicker
    {
        // 选择的图元
        public static List<Polyline> MakeUserPickEntities()
        {
            var polylines = new List<Polyline>();
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择墙轮廓",
                RejectObjectsOnLockedLayers = true,
            };

            var dxfNames = new string[]
            {
                RXClass.GetClass(typeof(Polyline)).DxfName,
            };

            var filter = ThSelectionFilterTool.Build(dxfNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return polylines;
            }

            using (var db = AcadDatabase.Active())
            {
                foreach (ObjectId polyId in result.Value.GetObjectIds())
                {
                    polylines.Add(db.CurrentSpace.Element(polyId) as Polyline);
                }
            }

            return polylines;
        }
    }
}
