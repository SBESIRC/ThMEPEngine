using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThCADExtension;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.WaterSupplyPipeSystem;

namespace ThMEPWSS.Common
{
    public static class Utils
    {
        public static void CreateFloorFraming(bool focus = true)
        {
            if (focus) ThMEPWSS.Common.Utils.FocusMainWindow();
            using (Active.Document.LockDocument())
            using (var acadDatabase = AcadDatabase.Active())
            {
                //if (!acadDatabase.Blocks.Contains(WaterSuplyBlockNames.FloorFraming))
                //{
                    
                //}
                using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
                {
                    var objID = acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterSuplyBlockNames.FloorFraming), true);//楼层框定
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
                    propmptResult.Value.Point3dZ0().TransformBy(Active.Editor.UCS2WCS()), new Scale3d(1, 1, 1), 0, new Dictionary<string, string> { { "楼层编号", "1" } });
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

            var ptRightRes = Active.Editor.GetCorner("\n再选择右下角点\n", leftDownPt);
            if (ptRightRes.Status == PromptStatus.OK)
            {
                //return Tuple.Create(leftDownPt.Point3dZ0().TransformBy(Active.Editor.UCS2WCS()), ptRightRes.Value.Point3dZ0().TransformBy(Active.Editor.UCS2WCS()));
                return Tuple.Create(leftDownPt, ptRightRes.Value);
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }
        }


        public static Point3dCollection SelectAreas()
        {
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return new Point3dCollection();
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());
                return frame.Vertices();
            }
        }

        public static void FocusMainWindow()
        {
#if ACAD_ABOVE_2014
            Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Focus();
#else
FocusToCAD();
#endif
        }
        public static void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
                    Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
