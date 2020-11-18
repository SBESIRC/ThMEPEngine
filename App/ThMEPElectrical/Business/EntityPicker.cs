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
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Assistant;

namespace ThMEPElectrical.Business
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

                    if (ptS.DistanceTo(ptE) < ThMEPCommon.PolyClosedDistance)
                    {
                        var clonePoly = curPoly.Clone() as Polyline;
                        clonePoly.Closed = true;
                        var bufferPoly = GeomUtils.BufferPoly(clonePoly);
                        if (bufferPoly != null && bufferPoly.Area > 1000)
                            polylines.Add(bufferPoly);
                    }
                }
            }

            return polylines;
        }

        public static List<Curve> MakeUserPickCurves()
        {
            var curves = new List<Curve>();
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择要布置的房间框线",
                RejectObjectsOnLockedLayers = true,
            };

            var result = Active.Editor.GetSelection(options);
            if (result.Status != PromptStatus.OK)
            {
                return curves;
            }

            using (var db = AcadDatabase.Active())
            {
                foreach (ObjectId curveId in result.Value.GetObjectIds())
                {
                    var entity = db.CurrentSpace.Element(curveId);
                    if (entity is Polyline || entity is Line)
                    {
                        var curve = entity as Curve;
                        curves.Add(GeomUtils.ExtendCurve(curve, ThMEPCommon.EntityExtendDistance));
                    }
                }
            }

            curves = GeomUtils.EraseSameObjects(curves);
            return curves;
        }
    }
}
