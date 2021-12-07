using System.Collections.Generic;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDrawEndComp
    {
        public string layer;
        public string flipDown45;
        public string brokenLine;
        public string verticalPipe;

        public ThDuctPortsDrawEndComp(string flipDown45, string brokenLine, string verticalPipe, string layer)
        {
            this.layer = layer;
            this.flipDown45 = flipDown45;
            this.brokenLine = brokenLine;
            this.verticalPipe = verticalPipe;
        }

        public void InsertFlipDown45(Point3d insertP, double width, double height, double angle)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var obj = db.ModelSpace.ObjectId.InsertBlockReference(layer, flipDown45, insertP, new Scale3d(), angle);
                ThDuctPortsDrawService.SetFlipDown45DynBlockProperity(obj, width, height);
            }
        }
        public void InsertBrokenLine(Point3d insertP, double width, double angle, double airVolume)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var attNameValues = new Dictionary<string, string> { { "风量", airVolume.ToString() + "m3/h" } };
                var obj = db.ModelSpace.ObjectId.InsertBlockReference(layer, brokenLine, insertP, new Scale3d(), angle, attNameValues);
                ThDuctPortsDrawService.SetBrokenLineDynBlockProperity(obj, width);
            }
        }
        public void InsertVerticalPipe(Point3d insertP, double width, double height, double angle)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var obj = db.ModelSpace.ObjectId.InsertBlockReference(layer, verticalPipe, insertP, new Scale3d(), angle);
                ThDuctPortsDrawService.SetVerticalPipeDynBlockProperity(obj, width, height);
            }
        }
    }
}
