using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPStructure.GirderConnect.Utils
{
    class GetObject
    {
        /// <summary>
        /// 获取带洞多边形
        /// </summary>
        /// <returns></returns>
        public static MPolygon GetMpolygon(AcadDatabase acdb)
        {
            var result = Active.Editor.GetSelection();
            if (result.Status != PromptStatus.OK)
            {
                return null;
            }
            var objs = new DBObjectCollection();
            foreach (var obj in result.Value.GetObjectIds())
            {
                objs.Add(acdb.Element<Entity>(obj));
            }
            return objs.BuildMPolygon();
        }

        /// <summary>
        /// 获取多边形
        /// </summary>
        /// <param name="acdb"></param>
        /// <returns></returns>
        public static Polyline GetPolyline(AcadDatabase acdb)
        {
            var result = Active.Editor.GetEntity("请选择多边形");
            if (result.Status != PromptStatus.OK)
            {
                return null;
            }
            return acdb.Element<Polyline>(result.ObjectId);
        }

        /// <summary>
        /// 获取图上的选取的点集合
        /// </summary>
        /// <returns></returns>
        public static List<Point3d> GetPoints()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            var points = new List<Point3d>();

            PromptSelectionResult psr = ed.GetSelection(); // 输入命令后再选择
            SelectionSet ss = psr.Value;
            using (Transaction trans = doc.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ss.GetObjectIds())
                {
                    DBPoint ent = (DBPoint)trans.GetObject(id, OpenMode.ForRead);
                    if (true)
                    {
                        points.Add(ent.Position);
                    }
                }
                trans.Commit();
            }
            return points;
        }

        /// <summary>
        /// 获得多个所选多边形的中点
        /// </summary>
        /// <param name="acdb"></param>
        /// <returns></returns>
        public static Point3dCollection GetCenters(AcadDatabase acdb)
        {
            var result = Active.Editor.GetSelection();
            var points = new Point3dCollection();
            if (result.Status == PromptStatus.OK)
            {
                foreach (var obj in result.Value.GetObjectIds())
                {
                    points.Add(acdb.Element<Entity>(obj).GeometricExtents.CenterPoint());
                }
            }
            return points;
        }

        /// <summary>
        /// 通过线找到包含这条线的内部为空多边形
        /// </summary>
        /// <param name="curLine"></param>
        /// <param name="LineVisit"></param>
        /// <returns></returns>
        public static List<Tuple<Point3d, Point3d>> GetPolygon(Tuple<Point3d, Point3d> curLine, Dictionary<Tuple<Point3d, Point3d>,int> LineVisit)
        {
            //List<Tuple<Point3d, Point3d>> lines = new List<Tuple<Point3d, Point3d>>();
            //lines.Add(curLine);
            //Tuple<Point3d, Point3d> tmpLine = new Tuple<Point3d, Point3d>();
            ////当能获得到下一根线的时候，就一直获取下一根线，如果获取不到了， 就返回null
            ////如果一开始就没有，
            //while ()
            //{
            //    lines.Add(GetNextLine(linesm ));
            //}

            return new List<Tuple<Point3d, Point3d>>();
        }

        /// <summary>
        /// 获取当前线所在多边形的下一条线段
        /// </summary>
        /// <param name="curLine"></param>
        /// <param name=""></param>
        /// <returns></returns>
        //public static Tuple<Point3d, Point3d> GetNextLine(Tuple<Point3d, Point3d> curLine, Dictionary<Tuple<Point3d, Point3d>, int> LineVisit)
        public static Tuple<Point3d, Point3d> GetNextLine(Tuple<Point3d, Point3d> curLine, Dictionary<Point3d, List<Point3d>> lines)
        {
            //如果下一个线不为空而且没有被访问过 ,则访问这个线
            //////////////////////////////////////////////////////////////////////////////////////////此代码可能有问题，参考上面的注释和参数列表
            double maxCmp = double.MinValue;
            Point3d nextPt = new Point3d();
            foreach (var point in lines[curLine.Item2])
            {
                if(point == curLine.Item1)
                {
                    continue;
                }
                var tmp = PointsDealer.DirectionCompair(curLine.Item1, curLine.Item2, point);
                if(tmp > maxCmp)
                {
                    maxCmp = tmp;
                    nextPt = point;
                }
            }
            return new Tuple<Point3d, Point3d>(curLine.Item1, nextPt);
        }
    }
}
