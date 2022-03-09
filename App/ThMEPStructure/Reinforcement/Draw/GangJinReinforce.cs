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
