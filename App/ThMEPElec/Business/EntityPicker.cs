using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using NFox.Cad.Collections;
using System;
using System.Collections.Generic;
using Linq2Acad;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Business
{
    /// <summary>
    /// 用户手选图元提取器
    /// </summary>
    public class EntityPicker
    {
        public const string WallLayer = "outerWall";
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

            var filterlist = OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.Start) == RXClass.GetClass(typeof(Polyline)).DxfName
                & o.Dxf((int)DxfCode.LayerName) == WallLayer);
            var result = Active.Editor.GetSelection(options, filterlist);
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
