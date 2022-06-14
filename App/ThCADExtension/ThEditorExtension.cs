using System;
using AcHelper;
using Linq2Acad;
using System.Windows;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ThCADExtension
{
    public static class ThEditorExtension
    {
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
            pt = pt.TransformBy(ed.WCS2UCS());
            points.Add(pt);

            p = new Point(screenPt.X + pSize, screenPt.Y - pSize);
            pt = ed.PointToWorld(p, 1);
            pt = pt.TransformBy(ed.WCS2UCS());
            points.Add(pt);

            p = new Point(screenPt.X + pSize, screenPt.Y + pSize);
            pt = ed.PointToWorld(p, 1);
            pt = pt.TransformBy(ed.WCS2UCS());
            points.Add(pt);

            p = new Point(screenPt.X - pSize, screenPt.Y + pSize);
            pt = ed.PointToWorld(p, 1);
            pt = pt.TransformBy(ed.WCS2UCS());
            points.Add(pt);

            return ed.SelectCrossingPolygon(points);
        }

        public static void ZoomWindow(this Editor ed, Extents3d ext)
        {
            ext.TransformBy(ed.WCS2UCS());
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