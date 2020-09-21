﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace NFox.Cad
{
    /// <summary>
    /// 坐标系类型枚举
    /// </summary>
    public enum CoordinateSystemCode
    {
        /// <summary>
        /// 世界坐标系
        /// </summary>
        Wcs = 0,
        /// <summary>
        /// 用户坐标系
        /// </summary>
        Ucs,
        /// <summary>
        /// 模型空间坐标系
        /// </summary>
        MDcs,
        /// <summary>
        /// 图纸空间坐标系
        /// </summary>
        PDcs
    }

    /// <summary>
    /// 命令行扩展类
    /// </summary>
    public static class EditorEx
    {
        /*
        /// <summary>
        /// 2009版本提供了acedCmd函数的封装
        /// </summary>
        private static MethodInfo _runCommand =
            typeof(Editor).GetMethod(
                "RunCommand",
                BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// 反射调用AutoCad命令(2009版本以上)
        /// </summary>
        /// <param name="editor">Editor对象</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public static PromptStatus Command(this Editor editor, params object[] args)
        {
            return (PromptStatus)_runCommand.Invoke(editor, new object[] { args });
        }
        */

        /// <summary>
        /// 选择穿过一个点的对象
        /// </summary>
        /// <param name="editor">命令行对象</param>
        /// <param name="point">点</param>
        /// <returns>选择集结果类</returns>
        public static PromptSelectionResult SelectAtPoint(this Editor editor, Point3d point)
        {
            return editor.SelectCrossingWindow(point, point);
        }

        /// <summary>
        /// 选择穿过一个点的对象
        /// </summary>
        /// <param name="editor">命令行对象</param>
        /// <param name="point">点</param>
        /// <param name="filter">过滤器</param>
        /// <returns>选择集结果类</returns>
        public static PromptSelectionResult SelectAtPoint(this Editor editor, Point3d point, SelectionFilter filter)
        {
            return editor.SelectCrossingWindow(point, point, filter);
        }

        #region Info

        /// <summary>
        /// 带错误提示对话框的打印信息函数
        /// </summary>
        /// <param name="format">带格式项的字符串</param>
        /// <param name="args">指定格式化的对象数组</param>
        public static void StreamMessage(string format, params object[] args)
        {
            StreamMessage(string.Format(format, args));
        }//

        /// <summary>
        /// 带错误提示对话框的打印信息函数
        /// </summary>
        /// <param name="message">打印信息</param>
        public static void StreamMessage(string message)
        {
            try
            {
                if (HasEditor())
                    WriteMessage(message);
                else
                    InfoMessageBox(message);
            }
            catch (System.Exception ex)
            {
                Message(ex);
            }
        }//

        /// <summary>
        /// 异常信息对话框
        /// </summary>
        /// <param name="ex">异常</param>
        public static void Message(System.Exception ex)
        {
            try
            {
                System.Windows.Forms.MessageBox.Show(
                    ex.ToString(),
                    "Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
            catch
            {
            }
        }//

        /// <summary>
        /// 提示信息对话框
        /// </summary>
        /// <param name="caption">对话框的标题</param>
        /// <param name="message">对话框文本</param>
        public static void InfoMessageBox(string caption, string message)
        {
            try
            {
                System.Windows.Forms.MessageBox.Show(
                    message,
                    caption,
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (System.Exception ex)
            {
                Message(ex);
            }
        }//

        /// <summary>
        /// 提示信息对话框
        /// </summary>
        /// <param name="caption">对话框的标题</param>
        /// <param name="format">带格式化项的对话框文本</param>
        /// <param name="args">指定格式化的对象数组</param>
        public static void InfoMessageBox(string caption, string format, params object[] args)
        {
            InfoMessageBox(caption, string.Format(format, args));
        }

        /// <summary>
        /// 提示信息对话框，默认标题为NFox.Cad
        /// </summary>
        /// <param name="message">对话框文本</param>
        public static void InfoMessageBox(string message)
        {
            InfoMessageBox("NFox.Cad", message);
        }//

        /// <summary>
        /// 提示信息对话框
        /// </summary>
        /// <param name="format">带格式化项的对话框文本</param>
        /// <param name="args">指定格式化的对象数组</param>
        public static void InfoMessageBox(string format, params object[] args)
        {
            InfoMessageBox(string.Format(format, args));
        }//

        /// <summary>
        /// 命令行打印字符串
        /// </summary>
        /// <param name="message">字符串</param>
        public static void WriteMessage(string message)
        {
            try
            {
                if (Acceptable())
                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n" + message);
                else
                    return;
            }
            catch (System.Exception ex)
            {
                Message(ex);
            }
        }//

        /// <summary>
        /// 命令行打印字符串
        /// </summary>
        /// <param name="format">带格式化项的文本</param>
        /// <param name="args">指定格式化的对象数组</param>
        public static void WriteMessage(string format, params object[] args)
        {
            WriteMessage(string.Format(format, args));
        }

        /// <summary>
        /// 判断是否有活动的编辑器对象
        /// </summary>
        /// <returns><see langword="true"/>有，<see langword="false"/>没有</returns>
        public static bool HasEditor()
        {
            return Application.DocumentManager.MdiActiveDocument != null
                && Application.DocumentManager.Count != 0
                && Application.DocumentManager.MdiActiveDocument.Editor != null;
        }//

        /// <summary>
        /// 判断是否可以打印字符串
        /// </summary>
        /// <returns><see langword="true"/>可以打印，<see langword="false"/>不可以打印</returns>
        public static bool Acceptable()
        {
            return HasEditor()
                && !Application.DocumentManager.MdiActiveDocument.Editor.IsDragging;
        }//

        #endregion Info

        #region DrawVectors

        /// <summary>
        /// 根据点表返回矢量线的列表
        /// </summary>
        /// <param name="pnts">点表</param>
        /// <param name="isClosed">是否闭合，<see langword="true"/> 为闭合，<see langword="false"/> 为不闭合</param>
        /// <returns></returns>
        private static List<TypedValue> GetLines(IEnumerable<Point2d> pnts, bool isClosed)
        {
            var itor = pnts.GetEnumerator();
            if (!itor.MoveNext())
                return new List<TypedValue>();

            List<TypedValue> values =
                new List<TypedValue>();

            TypedValue tvFirst = new TypedValue((int)LispDataType.Point2d, itor.Current);
            TypedValue tv1;
            TypedValue tv2 = tvFirst;

            while (itor.MoveNext())
            {
                tv1 = tv2;
                tv2 = new TypedValue((int)LispDataType.Point2d, itor.Current);
                values.Add(tv1);
                values.Add(tv2);
            }

            if (isClosed)
            {
                values.Add(tv2);
                values.Add(tvFirst);
            }

            return values;
        }

        /// <summary>
        /// 画矢量线
        /// </summary>
        /// <param name="editor">编辑器对象</param>
        /// <param name="pnts">点表</param>
        /// <param name="colorIndex">颜色码</param>
        /// <param name="isClosed">是否闭合，<see langword="true"/> 为闭合，<see langword="false"/> 为不闭合</param>
        public static void DrawVectors(this Editor editor, IEnumerable<Point2d> pnts, short colorIndex, bool isClosed)
        {
            var rlst =
                new XdataList { { LispDataType.Int16, colorIndex } };
            rlst.AddRange(GetLines(pnts, isClosed));
            editor.DrawVectors(rlst, editor.CurrentUserCoordinateSystem);
        }

        /// <summary>
        /// 画矢量线
        /// </summary>
        /// <param name="editor">编辑器对象</param>
        /// <param name="pnts">点表</param>
        /// <param name="colorIndex">颜色码</param>
        public static void DrawVectors(this Editor editor, IEnumerable<Point2d> pnts, short colorIndex)
        {
            editor.DrawVectors(pnts, colorIndex, false);
        }

        /// <summary>
        /// 用矢量线画近似圆（正多边形）
        /// </summary>
        /// <param name="editor">编辑器对象</param>
        /// <param name="pnts">点表</param>
        /// <param name="colorIndex">颜色码</param>
        /// <param name="radius">半径</param>
        /// <param name="numEdges">多边形边的个数</param>
        public static void DrawCircles(this Editor editor, IEnumerable<Point2d> pnts, short colorIndex, double radius, int numEdges)
        {
            var rlst =
                new XdataList { { LispDataType.Int16, colorIndex } };

            foreach (Point2d pnt in pnts)
            {
                Vector2d vec = Vector2d.XAxis * radius;
                double angle = Math.PI * 2 / numEdges;

                List<Point2d> tpnts = new List<Point2d>();
                tpnts.Add(pnt + vec);
                for (int i = 1; i < numEdges; i++)
                {
                    tpnts.Add(pnt + vec.RotateBy(angle * i));
                }

                rlst.AddRange(GetLines(tpnts, true));
            }
            editor.DrawVectors(rlst, editor.CurrentUserCoordinateSystem);
        }

        /// <summary>
        /// 用矢量线画近似圆（正多边形）
        /// </summary>
        /// <param name="editor">编辑器对象</param>
        /// <param name="pnt">点</param>
        /// <param name="colorIndex">颜色码</param>
        /// <param name="radius">半径</param>
        /// <param name="numEdges">多边形边的个数</param>
        public static void DrawCircle(this Editor editor, Point2d pnt, short colorIndex, double radius, int numEdges)
        {
            Vector2d vec = Vector2d.XAxis * radius;
            double angle = Math.PI * 2 / numEdges;

            List<Point2d> pnts = new List<Point2d>();
            pnts.Add(pnt + vec);
            for (int i = 1; i < numEdges; i++)
            {
                pnts.Add(pnt + vec.RotateBy(angle * i));
            }

            editor.DrawVectors(pnts, colorIndex, true);
        }

        #endregion DrawVectors

        #region Matrix

        /// <summary>
        /// 获取UCS到WCS的矩阵
        /// </summary>
        /// <param name="editor">编辑器对象</param>
        /// <returns>变换矩阵</returns>
        public static Matrix3d GetMatrixFromUcsToWcs(this Editor editor)
        {
            return editor.CurrentUserCoordinateSystem;
        }

        /// <summary>
        /// 获取WCS到UCS的矩阵
        /// </summary>
        /// <param name="editor">编辑器对象</param>
        /// <returns>变换矩阵</returns>
        public static Matrix3d GetMatrixFromWcsToUcs(this Editor editor)
        {
            return editor.CurrentUserCoordinateSystem.Inverse();
        }

        /// <summary>
        /// 获取MDCS(模型空间)到WCS的矩阵
        /// </summary>
        /// <param name="editor">编辑器对象</param>
        /// <returns>变换矩阵</returns>
        public static Matrix3d GetMatrixFromMDcsToWcs(this Editor editor)
        {
            Matrix3d mat;
            using (ViewTableRecord vtr = editor.GetCurrentView())
            {
                mat = Matrix3d.PlaneToWorld(vtr.ViewDirection);
                mat = Matrix3d.Displacement(vtr.Target - Point3d.Origin) * mat;
                return Matrix3d.Rotation(-vtr.ViewTwist, vtr.ViewDirection, vtr.Target) * mat;
            }
        }

        /// <summary>
        /// 获取WCS到MDCS(模型空间)的矩阵
        /// </summary>
        /// <param name="editor">编辑器对象</param>
        /// <returns>变换矩阵</returns>
        public static Matrix3d GetMatrixFromWcsToMDcs(this Editor editor)
        {
            return editor.GetMatrixFromMDcsToWcs().Inverse();
        }

        /// <summary>
        /// 获取MDCS(模型空间)到PDCS(图纸空间)的矩阵
        /// </summary>
        /// <param name="editor">编辑器对象</param>
        /// <returns>变换矩阵</returns>
        public static Matrix3d GetMatrixFromMDcsToPDcs(this Editor editor)
        {

            if ((short)Application.GetSystemVariable("TILEMODE") == 1)
                throw new Autodesk.AutoCAD.Runtime.Exception(ErrorStatus.InvalidInput, "Espace papier uniquement");

            Database db = editor.Document.Database;
            Matrix3d mat;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Viewport vp =
                    (Viewport)tr.GetObject(editor.CurrentViewportObjectId, OpenMode.ForRead);
                if (vp.Number == 1)
                {
                    try
                    {
                        editor.SwitchToModelSpace();
                        vp = (Viewport)tr.GetObject(editor.CurrentViewportObjectId, OpenMode.ForRead);
                        editor.SwitchToPaperSpace();
                    }
                    catch
                    {
                        throw new Autodesk.AutoCAD.Runtime.Exception(ErrorStatus.InvalidInput, "Aucun fenêtre active");
                    }
                }
                Point3d vCtr = new Point3d(vp.ViewCenter.X, vp.ViewCenter.Y, 0.0);
                mat = Matrix3d.Displacement(vCtr.GetAsVector().Negate());
                mat = Matrix3d.Displacement(vp.CenterPoint.GetAsVector()) * mat;
                mat = Matrix3d.Scaling(vp.CustomScale, vp.CenterPoint) * mat;
                tr.Commit();
            }
            return mat;
        }

        /// <summary>
        /// 获取PDCS(图纸空间)到MDCS(模型空间)的矩阵
        /// </summary>
        /// <param name="editor">编辑器对象</param>
        /// <returns>变换矩阵</returns>
        public static Matrix3d GetMatrixFromPDcsToMDcs(this Editor editor)
        {
            return editor.GetMatrixFromMDcsToPDcs().Inverse();
        }

        /// <summary>
        /// 获取变换矩阵
        /// </summary>
        /// <param name="editor">编辑器对象</param>
        /// <param name="from">源坐标系</param>
        /// <param name="to">目标坐标系</param>
        /// <returns>变换矩阵</returns>
        public static Matrix3d GetMatrix(this Editor editor, CoordinateSystemCode from, CoordinateSystemCode to)
        {
            switch (from)
            {
                case CoordinateSystemCode.Wcs:
                    switch (to)
                    {
                        case CoordinateSystemCode.Ucs:
                            return editor.GetMatrixFromWcsToUcs();

                        case CoordinateSystemCode.MDcs:
                            return editor.GetMatrixFromMDcsToWcs();

                        case CoordinateSystemCode.PDcs:
                            throw new Autodesk.AutoCAD.Runtime.Exception(
                                ErrorStatus.InvalidInput,
                                "To be used only with DCS");
                    }
                    break;

                case CoordinateSystemCode.Ucs:
                    switch (to)
                    {
                        case CoordinateSystemCode.Wcs:
                            return editor.GetMatrixFromUcsToWcs();

                        case CoordinateSystemCode.MDcs:
                            return editor.GetMatrixFromUcsToWcs() * editor.GetMatrixFromWcsToMDcs();

                        case CoordinateSystemCode.PDcs:
                            throw new Autodesk.AutoCAD.Runtime.Exception(
                                ErrorStatus.InvalidInput,
                                "To be used only with DCS");         
                    }
                    break;

                case CoordinateSystemCode.MDcs:
                    switch (to)
                    {
                        case CoordinateSystemCode.Wcs:
                            return editor.GetMatrixFromMDcsToWcs();

                        case CoordinateSystemCode.Ucs:
                            return editor.GetMatrixFromMDcsToWcs() * editor.GetMatrixFromWcsToUcs();

                        case CoordinateSystemCode.PDcs:
                            return editor.GetMatrixFromMDcsToPDcs();
                    }
                    break;

                case CoordinateSystemCode.PDcs:
                    switch (to)
                    {
                        case CoordinateSystemCode.Wcs:
                            throw new Autodesk.AutoCAD.Runtime.Exception(
                                ErrorStatus.InvalidInput,
                                "To be used only with DCS");
                        case CoordinateSystemCode.Ucs:
                            throw new Autodesk.AutoCAD.Runtime.Exception(
                                ErrorStatus.InvalidInput,
                                "To be used only with DCS");
                        case CoordinateSystemCode.MDcs:
                            return editor.GetMatrixFromPDcsToMDcs();
                    }
                    break;
            }

            return Matrix3d.Identity;
        }

#endregion Matrix

#region Zoom

        /// <summary>
        /// 缩放窗口范围
        /// </summary>
        /// <param name="ed">编辑器对象</param>
        /// <param name="minPoint">窗口左下点</param>
        /// <param name="maxPoint">窗口右上点</param>
        public static void ZoomWindow(this Editor ed, Point3d minPoint, Point3d maxPoint)
        {
            ViewTableRecord cvtr = ed.GetCurrentView();
            ViewTableRecord vtr = new ViewTableRecord();
            vtr.CopyFrom(cvtr);

            Point3d[] oldpnts = new Point3d[] { minPoint, maxPoint };
            Point3d[] pnts = new Point3d[8];
            Point3d[] dpnts = new Point3d[8];
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        int n = i * 4 + j * 2 + k;
                        pnts[n] = new Point3d(oldpnts[i][0], oldpnts[j][1], oldpnts[k][2]);
                        dpnts[n] = pnts[n].TransformBy(ed.GetMatrixFromWcsToMDcs());
                    }
                }
            }

            double xmin, xmax, ymin, ymax;
            xmin = xmax = dpnts[0][0];
            ymin = ymax = dpnts[0][1];
            for (int i = 1; i < 8; i++)
            {
                xmin = Math.Min(xmin, dpnts[i][0]);
                xmax = Math.Max(xmax, dpnts[i][0]);
                ymin = Math.Min(ymin, dpnts[i][1]);
                ymax = Math.Max(ymax, dpnts[i][1]);
            }

            vtr.Width = xmax - xmin;
            vtr.Height = ymax - ymin;
            vtr.CenterPoint = (dpnts[0] + (dpnts[7] - dpnts[0]) / 2).Convert2d(new Plane());

            ed.SetCurrentView(vtr);
            ed.Regen();
        }

        /// <summary>
        /// 缩放窗口范围
        /// </summary>
        /// <param name="ed">编辑器对象</param>
        /// <param name="ext">窗口范围点</param>
        public static void ZoomWindow(this Editor ed, Extents3d ext)
        {
            ZoomWindow(ed, ext.MinPoint, ext.MaxPoint);
        }

        /// <summary>
        /// 动态缩放
        /// </summary>
        /// <param name="ed">编辑器对象</param>
        public static void ZoomExtents(this Editor ed)
        {
            Database db = ed.Document.Database;
            db.UpdateExt(true);
            if (db.Extmax.X < db.Extmin.X)
            {
                Plane plane = new Plane();
                ZoomWindow(
                    ed,
                    new Point3d(plane, db.Limmin),
                    new Point3d(plane, db.Limmax));
            }
            else
            {
                ZoomWindow(ed, db.Extmin, db.Extmax);
            }
        }

        /// <summary>
        /// 缩放对象
        /// </summary>
        /// <param name="ed">编辑器对象</param>
        /// <param name="entity">对象</param>
        public static void ZoomObject(this Editor ed, Entity entity)
        {
            ZoomWindow(ed, entity.GeometricExtents);
        }

#endregion Zoom

#region 执行lisp
        [System.Security.SuppressUnmanagedCodeSecurity]
        [DllImport("accore.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "?acedEvaluateLisp@@YAHPEB_WAEAPEAUresbuf@@@Z")]
        private static extern int AcedEvaluateLisp(string lispLine, out IntPtr result);

        [DllImport("accore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "acedInvoke")]
        private static extern int AcedInvoke(IntPtr args, out IntPtr result);

        /// <summary>
        /// 发送lisp语句字符串到cad执行
        /// </summary>
        /// <param name="ed">编辑器对象</param>
        /// <param name="arg">lisp语句</param>
        /// <returns>缓冲结果,返回值</returns>
        public static ResultBuffer RunLisp(this Editor ed, string arg)
        {
            AcedEvaluateLisp(arg, out IntPtr rb);
            if (rb != IntPtr.Zero)
            {
                try
                {
                    var rbb = DisposableWrapper.Create(typeof(ResultBuffer), rb, true) as ResultBuffer;
                    return rbb;
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

#endregion
    }
}