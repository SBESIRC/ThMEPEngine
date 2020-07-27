using Linq2Acad;
using AcHelper;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.AutoCAD.Utility.ExtensionTools;
using GeometryExtensions;

namespace Autodesk.AutoCAD.EditorInput
{
    public enum PolygonSelectionMode
    {
        Crossing,
        Window
    }

    public static class ThEditorExtension
    {
        // Select object inside a polyline
        //  https://forums.autodesk.com/t5/net/select-object-inside-a-polyline/td-p/6018866
        public static PromptSelectionResult SelectByPolyline(this Editor ed, 
            ObjectId plineObjId, 
            PolygonSelectionMode mode,
            SelectionFilter filter)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(plineObjId.Database))
            {
                // 保存当前view
                ViewTableRecord view = ed.GetCurrentView();

                // zoom到pline
                Active.Editor.ZoomObject(plineObjId);

                // 计算选择范围
                // 由于绘图不规范，对于一些“奇异”的多段线，用它作为选择的轮廓线会导致选择失败。
                // 这里对于已知的情况做一些特殊处理：
                //  1. 剔除重复顶点
                var pline = acadDatabase.Element<Polyline>(plineObjId);
                Point3dCollection points = pline.Vertices();
                Point3dCollection polygon = new Point3dCollection();
                for (int i = 0; i < points.Count; i++)
                {
                    var pt = points[i];
                    if (!polygon.Contains(pt, ThCADCommon.Global_Tolerance))
                    {
                        polygon.Add(pt);
                    }
                }

                // 选择
                PromptSelectionResult result;
                if (mode == PolygonSelectionMode.Crossing)
                    result = ed.SelectCrossingPolygon(polygon, filter);
                else
                    result = ed.SelectWindowPolygon(polygon, filter);

                // 恢复view
                ed.SetCurrentView(view);
                return result;
            }
        }

        public static PromptSelectionResult SelectByFence(this Editor ed,
            ObjectId plineObjId,
            Point3dCollection fence,
            SelectionFilter filter)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 保存当前view
                ViewTableRecord view = ed.GetCurrentView();

                // zoom到pline
                Active.Editor.ZoomObject(plineObjId);

                // 选择
                PromptSelectionResult result;
                result = ed.SelectFence(fence, filter);

                // 恢复view
                ed.SetCurrentView(view);
                return result;
            }
        }

        public static PromptSelectionResult SelectByPolygon(this Editor ed,
            Point3dCollection polygon,
            PolygonSelectionMode mode,
            SelectionFilter filter)
        {
            // 保存当前view
            ViewTableRecord view = ed.GetCurrentView();

            // zoom到polygon
            Active.Editor.ZoomWindow(polygon.ToExtents3d());

            // 选择
            PromptSelectionResult result;
            if (mode == PolygonSelectionMode.Crossing)
                result = ed.SelectCrossingPolygon(polygon, filter);
            else
                result = ed.SelectWindowPolygon(polygon, filter);

            // 恢复view
            ed.SetCurrentView(view);
            return result;

        }

        public static PromptSelectionResult SelectByRegion(this Editor ed,
            ObjectId regionId,
            PolygonSelectionMode mode,
            SelectionFilter filter)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(regionId.Database))
            {
                var polygon = acadDatabase.Element<Region>(regionId).Vertices();
                return ed.SelectByPolygon(polygon, mode, filter);
            }
        }

        public static void ZoomWindow(this Editor ed, Extents3d ext)
        {
            ext.TransformBy(ed.CurrentUserCoordinateSystem.Inverse());
            COMTool.ZoomWindow(ext.MinPoint, ext.MaxPoint);
        }

        public static void ZoomObject(this Editor ed, ObjectId entId)
        {
            Database db = ed.Document.Database;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //获取实体对象
                Entity ent = trans.GetObject(entId, OpenMode.ForRead) as Entity;
                if (ent == null) return;
                //根据实体的范围对视图进行缩放
                Extents3d ext = ent.GeometricExtents;
                ext.TransformBy(ed.CurrentUserCoordinateSystem.Inverse());
                COMTool.ZoomWindow(ext.MinPoint, ext.MaxPoint);
                trans.Commit();
            }
        }
    }
}