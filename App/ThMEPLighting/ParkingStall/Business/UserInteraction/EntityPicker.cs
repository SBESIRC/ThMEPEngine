using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace ThMEPLighting.ParkingStall.Business.UserInteraction
{
    /// <summary>
    /// 用户手选图元提取器
    /// </summary>
    public class EntityPicker
    {
        // 选择的图元
        public static List<Polyline> MakeUserPickPolys()
        {
            var polylines = new List<Polyline>();
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择要布置的房间框线",
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
                    var curPoly = db.CurrentSpace.Element(polyId) as Polyline;
                    var ptS = curPoly.StartPoint;
                    var ptE = curPoly.EndPoint;

                    if (ptS.DistanceTo(ptE) < ParkingStallCommon.PolyClosedDistance)
                    {
                        var clonePoly = curPoly.Clone() as Polyline;
                        clonePoly.Closed = true;
                        if (clonePoly.Area > 1000)
                            polylines.Add(clonePoly);
                    }
                }
            }

            return polylines;
        }
    }
}
