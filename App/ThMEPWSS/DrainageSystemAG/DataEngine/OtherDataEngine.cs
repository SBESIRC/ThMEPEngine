using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.DrainageSystemAG.DataEngine
{
    class OtherDataEngine
    {
        public static List<Polyline> AllDoors(Polyline polyline)
        {
            var doorPlines = new List<Polyline>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var doorEngine = new ThDB3DoorRecognitionEngine())
            {
                doorEngine.Recognize(acadDatabase.Database, polyline.Vertices());
                doorEngine.Elements.ForEach(o =>
                {
                    var pLine = o.Outline.ToNTSPolygon().ToDbPolylines().FirstOrDefault();
                    doorPlines.Add(pLine);
                });
            }
            return doorPlines;
        }
        public static List<Polyline> AllWindows(Polyline polyline)
        {
            var doorPlines = new List<Polyline>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var windowEngine = new ThDB3WindowRecognitionEngine())
            {
                windowEngine.Recognize(acadDatabase.Database, polyline.Vertices());
                windowEngine.Elements.ForEach(o =>
                {
                    var pLine = o.Outline.ToNTSPolygon().ToDbPolylines().FirstOrDefault();
                    doorPlines.Add(pLine);
                });
            }
            return doorPlines;
        }
    }
}
