using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.AutoCAD.Utility.ExtensionTools
{
    public static class SelectionTool
    {
        /// <summary>
        /// 获取选中的实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T ChooseEntity<T>() where T : Entity
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            var results = new List<T>();
            ////创建选择集
            PromptSelectionResult psr = ed.GetSelection();
            //没有选择，返回空
            if (psr.Status != PromptStatus.OK) return default(T);
            SelectionSet ss = psr.Value;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ss.GetObjectIds())
                {
                    var ent = trans.GetObject(id, OpenMode.ForWrite) as T;
                    if (ent != null)
                    {
                        results.Add(ent);
                    }
                }
            }

            return results.First();
        }


        public static List<T> ChooseEntitys<T>() where T : Entity
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            var results = new List<T>();
            ////创建选择集
            PromptSelectionResult psr = ed.GetSelection();
            //没有选择，返回空
            if (psr.Status != PromptStatus.OK) return default(List<T>);
            SelectionSet ss = psr.Value;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ss.GetObjectIds())
                {
                    var ent = trans.GetObject(id, OpenMode.ForWrite) as T;
                    if (ent != null)
                    {
                        results.Add(ent);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 各种类型的选择
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public static List<T> DocChoose<T>(Func<PromptSelectionResult> func) where T : Entity
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            var db = doc.Database;

            var results = new List<T>();

            PromptSelectionResult psr = func();
            //没有选择，返回空
            if (psr.Status != PromptStatus.OK) return null;
            SelectionSet ss = psr.Value;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ss.GetObjectIds())
                {
                    var ent = trans.GetObject(id, OpenMode.ForWrite, false, true) as T;
                    results.Add(ent);
                }
            }

            return results;
        }

        /// <summary>
        /// 带事件的自定义选择过滤器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="onSelectionAdded"></param>
        /// <returns></returns>
        public static List<T> DocChoose<T>(Func<PromptSelectionResult> func, SelectionAddedEventHandler onSelectionAdded) where T : Entity
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            var db = doc.Database;

            var results = new List<T>();

            ed.SelectionAdded += onSelectionAdded;
            PromptSelectionResult psr = func();
            ed.SelectionAdded -= onSelectionAdded;

            //没有选择，返回空
            if (psr.Status != PromptStatus.OK) return null;
            SelectionSet ss = psr.Value;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ss.GetObjectIds())
                {
                    var ent = trans.GetObject(id, OpenMode.ForWrite, false, true) as T;
                    results.Add(ent);
                }
            }

            return results;
        }


        /// <summary>
        /// 各种类型的选择,可设定是否打开锁定图层
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="openForceLayer"></param>
        /// <returns></returns>
        public static List<T> DocChoose<T>(Func<PromptSelectionResult> func, bool openForceLayer) where T : Entity
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            var db = doc.Database;

            var results = new List<T>();

            PromptSelectionResult psr = func();
            //没有选择，返回空
            if (psr.Status != PromptStatus.OK) return null;
            SelectionSet ss = psr.Value;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ss.GetObjectIds())
                {
                    var ent = trans.GetObject(id, OpenMode.ForWrite, false, openForceLayer) as T;
                    results.Add(ent);
                }
            }

            return results;
        }

    }
}
