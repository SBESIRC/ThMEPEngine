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
    class GangJinReinforce:GangJinBase
    {
        Point3d point;
        double r;
        protected void CalPositionL()
        {

        }

        protected void CalPositionT()
        {

        }
        protected void CalPositionR()
        {

        }

        protected void DrawLabel()
        {

        }

        /// <summary>
        /// 生成点筋
        /// </summary>
        /// <param name="position">点筋位置</param>
        /// <param name="r">点筋半径</param>
        /// <returns>点筋</returns>
        protected Polyline DrawReinforce(Point3d position, double r)
        {
            Polyline res = new Polyline();
            Point2d pt = new Point2d(position.X, position.Y);

            res.AddVertexAt(0, pt + new Vector2d(r / 2, 0), 1, r, r);
            res.AddVertexAt(1, pt + new Vector2d(-r / 2, 0), 1, r, r);
            res.Closed = true;
            return res;
        }

        public override void  Draw()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            var documentlock = doc.LockDocument();
            Autodesk.AutoCAD.EditorInput.Editor ed = doc.Editor;
            ed.WriteMessage("我是纵筋");
            DrawLabel();
        }
    }
}
