using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
namespace ThMEPStructure.Reinforcement.Draw
{
    class GangJinLink:GangJinBase
    {
        Point3d point1,point2;

        protected void CalPositionL()
        {

        }

        protected void CalPositionT()
        {

        }
        protected void CalPositionR()
        {

        }

        protected void DrawExplode()
        {

        }

        /// <summary>
        /// 生成拉筋
        /// </summary>
        /// <param name="pt1">点筋位置1</param>
        /// <param name="pt2">点筋位置2</param>
        /// <param name="r">转角半径</param>
        /// <param name="width">线宽</param>
        /// <returns>拉筋</returns>
        protected Polyline DrawLink(Point3d pt1, Point3d pt2, double r, double width)
        {
            Polyline res = new Polyline();
            Vector3d direction = (pt2 - pt1).GetNormal();
            Vector3d vec = direction.GetPerpendicularVector() * r;
            Point3d pt = pt1 + vec + direction * 2 * r;
            res.AddVertexAt(0, new Point2d(pt.X, pt.Y), 0, width, width);
            pt = pt1 + vec;
            res.AddVertexAt(1, new Point2d(pt.X, pt.Y), 0, width, width);
            pt = pt1 - vec;
            res.AddVertexAt(2, new Point2d(pt.X, pt.Y), 1, width, width);
            pt += pt2 - pt1;
            res.AddVertexAt(3, new Point2d(pt.X, pt.Y), 0, width, width);
            pt += 2 * vec;
            res.AddVertexAt(4, new Point2d(pt.X, pt.Y), 1, width, width);
            pt += -direction * 2 * r;
            res.AddVertexAt(5, new Point2d(pt.X, pt.Y), 0, width, width);
            return res;
        }
        public override void Draw()
        {

            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            var documentlock = doc.LockDocument();
            Autodesk.AutoCAD.EditorInput.Editor ed = doc.Editor;
            ed.WriteMessage("我是拉筋");
            DrawExplode();
        }
    }
}
