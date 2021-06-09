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
using ThCADExtension;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Common
{
    public static class Utils
    {
        public static void CreateFloorFraming()
        {
            using (Active.Document.LockDocument())
            using (var acadDatabase = AcadDatabase.Active())
            {
                if (!acadDatabase.Blocks.Contains(WaterSuplyBlockNames.FloorFraming))
                {
                    using AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false);//////////
                    var objID = acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.FloorFraming));//楼层框定
                }
            }
            while (true)
            {
                var opt = new PromptPointOptions("点击进行楼层框定");
                var propmptResult = Active.Editor.GetPoint(opt);
                if (propmptResult.Status != PromptStatus.OK)
                {
                    break;
                }
                using (Active.Document.LockDocument())
                using (var acadDatabase = AcadDatabase.Active())
                {
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference("0", WaterSuplyBlockNames.FloorFraming,
                    propmptResult.Value, new Scale3d(1, 1, 1), 0);
                }
            }
        }

        public static Tuple<Point3d, Point3d> SelectPoints()
        {
            var ptLeftRes = Active.Editor.GetPoint("\n请您框选范围，先选择左上角点");
            Point3d leftDownPt = Point3d.Origin;
            if (ptLeftRes.Status == PromptStatus.OK)
            {
                leftDownPt = ptLeftRes.Value;
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }

            var ptRightRes = Active.Editor.GetCorner("\n再选择右下角点", leftDownPt);
            if (ptRightRes.Status == PromptStatus.OK)
            {
                return Tuple.Create(leftDownPt, ptRightRes.Value);
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }
        }
    }
}
