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
using Autodesk.AutoCAD.Colors;
using System.IO;
using Autodesk.AutoCAD.DatabaseServices.Filters;
namespace ThMEPStructure.Reinforcement.Draw
{
    public class Test
    {
        [CommandMethod("Draw", "DrawZhuJin", CommandFlags.Session)]
        public void test()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            var documentlock = doc.LockDocument();
            Autodesk.AutoCAD.EditorInput.Editor ed = doc.Editor;
            ed.WriteMessage("Test!");
        }
    }
}
