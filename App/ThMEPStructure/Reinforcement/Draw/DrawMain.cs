using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
namespace ThMEPStructure.Reinforcement.Draw
{
    public class DrawMain
    {
        [CommandMethod("Draw", "DrawGangjin", CommandFlags.Session)]
        public void test()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            var documentlock = doc.LockDocument();
            Autodesk.AutoCAD.EditorInput.Editor ed = doc.Editor;
            ed.WriteMessage("Draw begin");
            //需要对一行上的所有暗柱计算表格第一行截面行的长和宽

            DrawObjectLType drawObjectLType = new DrawObjectLType();
            //drawObjectLType.Init();
            drawObjectLType.CalGangjinPosition();
            drawObjectLType.DrawGangJin();
        }
    }
}
