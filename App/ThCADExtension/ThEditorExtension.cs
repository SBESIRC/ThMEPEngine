using System;
using AcHelper;
using Linq2Acad;
#if ACAD2012
    using System.Drawing;
#else
using System.Windows;
#endif
using ThCADExtension;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using AcRegion = Autodesk.AutoCAD.DatabaseServices.Region;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ThCADExtension
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
                Active.Editor.ZoomToObject(plineObjId);

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
                Active.Editor.ZoomToObject(plineObjId);

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

        public static PromptSelectionResult SelectByWindow(this Editor ed,
            Point3d pt1,
            Point3d pt2,
            PolygonSelectionMode mode,
            SelectionFilter filter)
        {
            // 保存当前view
            ViewTableRecord view = ed.GetCurrentView();

            // zoom到polygon
            Active.Editor.ZoomWindow(new Extents3d(pt1, pt2));

            // 选择
            PromptSelectionResult result;
            if (mode == PolygonSelectionMode.Crossing)
                result = ed.SelectCrossingWindow(pt1, pt2, filter);
            else
                result = ed.SelectWindow(pt1, pt2, filter);

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
                var polygon = acadDatabase.Element<AcRegion>(regionId).Vertices();
                return ed.SelectByPolygon(polygon, mode, filter);
            }
        }

        public static PromptSelectionResult SelectAtPickBox(this Editor ed, Point3d pickBoxCentre)
        {
            //Get pick box's size on screen
            Point screenPt = ed.PointToScreen(pickBoxCentre, 1);

            //Get pickbox's size. Note, the number obtained from
            //system variable "PICKBOX" is actually the half of
            //pickbox's width/height
            object pBox = AcadApp.GetSystemVariable("PICKBOX");
            int pSize = Convert.ToInt32(pBox);

            //Define a Point3dCollection for CrossingWindow selecting
            Point3dCollection points = new Point3dCollection();

            Point p;
            Point3d pt;

            p = new Point(screenPt.X - pSize, screenPt.Y - pSize);
            pt = ed.PointToWorld(p, 1);
            points.Add(pt);

            p = new Point(screenPt.X + pSize, screenPt.Y - pSize);
            pt = ed.PointToWorld(p, 1);
            points.Add(pt);

            p = new Point(screenPt.X + pSize, screenPt.Y + pSize);
            pt = ed.PointToWorld(p, 1);
            points.Add(pt);

            p = new Point(screenPt.X - pSize, screenPt.Y + pSize);
            pt = ed.PointToWorld(p, 1);
            points.Add(pt);

            return ed.SelectCrossingPolygon(points);
        }

        public static void ZoomWindow(this Editor ed, Extents3d ext)
        {
            ext.TransformBy(ed.CurrentUserCoordinateSystem.Inverse());
            COMTool.ZoomWindow(ext.MinPoint, ext.MaxPoint);
        }

        public static void ZoomToObject(this Editor ed, ObjectId entId)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(entId.Database))
            {
                var obj = acadDatabase.Element<Entity>(entId);
                ed.ZoomToObjects(new Entity[] { obj }, 1.0);
            }
        }

        public static void ZoomToObjects(this Editor ed, Entity[] entities, double scale)
        {
            try
            {
                var extents = new Extents3d();
                entities.ForEachDbObject(e =>
                {
                    Plane XYPlane = new Plane(Point3d.Origin, Vector3d.ZAxis);
                    Matrix3d matrix = Matrix3d.Projection(XYPlane, XYPlane.Normal);
                    var projectExtents = new Extents3d(
                        e.GeometricExtents.MinPoint.TransformBy(matrix),
                        e.GeometricExtents.MaxPoint.TransformBy(matrix));
                    extents.AddExtents(projectExtents);
                });
                //wcs下的Extend中心点
                var center = extents.CenterPoint();
                //Extend的宽和高
                double projExtendHeight = extents.Height();
                double projExtendWidth = extents.Width();
                //以Extentd中心为ocs坐标原点，获取ocs下的Extend的最大最小点坐标
                Point3d ocsMinPoint = new Point3d(-0.5 * projExtendWidth, -0.5 * projExtendHeight, 0);
                Point3d ocsMaxPoint = new Point3d(0.5 * projExtendWidth, 0.5 * projExtendHeight, 0);
                var ocsExtents = new Extents3d(ocsMinPoint, ocsMaxPoint);
                // 转换到WCS
                Matrix3d rotationMat = Matrix3d.Identity;
                Matrix3d scaleMat = Matrix3d.Scaling(scale, Point3d.Origin);
                Matrix3d movementMat = Matrix3d.Displacement(center.GetAsVector());
                ocsExtents.TransformBy(scaleMat.PreMultiplyBy(rotationMat).PreMultiplyBy(movementMat));
                // 在WCS下缩放到范围内
                COMTool.ZoomWindow(ocsExtents.MinPoint, ocsExtents.MaxPoint);
            }
            catch
            {
                // 在缺失字体的情况下，BlockReference.GeometricExtents会抛出eInvalidExtents异常
                // 若异常发生，不执行任何操作
            }
        }

        public static void PickFirstObjects(this Editor ed, ObjectId[] objs)
        {
            // 首先清空现有的PickFirst选择集
            Active.Editor.SetImpliedSelection(new ObjectId[0]);
            // 接着讲模型添加到PickFirst选择集
            Active.Editor.SetImpliedSelection(objs);
        }
    }
}