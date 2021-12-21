using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using System.Linq;
using ThMEPWSS.DrainageSystemDiagram;
using Autodesk.AutoCAD.Runtime;
using ThMEPEngineCore.Command;
using Linq2Acad;

namespace ThMEPWSS.SprinklerConnect.cmd
{

    public partial class ThSprinklerConnectNoUICmd
    {
        [CommandMethod("TIANHUACAD", "THIntersectTest", CommandFlags.Modal)]
        public void IntersectExecute()
        {

            //using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            //using (AcadDatabase acadDatabase = AcadDatabase.Active())
            //{
                //private List<Polyline> intersectSet { get; set; } = new List<Polyline>();
                Polyline targetPoly = new Polyline();
                Polyline crossPoly = new Polyline();
                targetPoly.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
                targetPoly.AddVertexAt(1, new Point2d(15, 0), 0, 0, 0);
                targetPoly.AddVertexAt(2, new Point2d(15, 10), 0, 0, 0);
                targetPoly.AddVertexAt(3, new Point2d(0, 10), 0, 0, 0);
                crossPoly.AddVertexAt(0, new Point2d(10, 5), 0, 0, 0);
                crossPoly.AddVertexAt(1, new Point2d(30, 5), 0, 0, 0);
                crossPoly.AddVertexAt(2, new Point2d(30, 15), 0, 0, 0);
                crossPoly.AddVertexAt(3, new Point2d(10, 15), 0, 0, 0);
                var intersect = ThCADCoreNTSEntityExtension.Intersection(targetPoly, crossPoly).Cast<Polyline>().ToList();
                DrawUtils.ShowGeometry(intersect, "testForIntersection", 1);
            //}
        
        }
        //public void SeparateTower()
        //{
        //    var cmd = new IntersectionTest();
        //    cmd.Execute();
        //}
    }

    //public class IntersectionTest : ThMEPBaseCommand
    //{        

    //    public IntersectionTest()
    //    {
    //    }
    //    public override void SubExecute()
    //    {
    //        IntersectExecute();
    //    }



    //}
}