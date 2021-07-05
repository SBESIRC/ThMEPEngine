using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.DrainageSystemAG.DataEngine
{
    class DoorWindowEngine
    {
        ThDB3WindowExtractionEngine windowEngine = null;
        ThDB3DoorExtractionEngine doorEngine = null;
        public DoorWindowEngine(Database database) 
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database)) 
            {
                windowEngine = new ThDB3WindowExtractionEngine();
                windowEngine.Extract(database);

                doorEngine = new ThDB3DoorExtractionEngine();
                doorEngine.Extract(database);
            }
        }
        /*
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
                windowEngine.Recognize(, polyline.Vertices());
                windowEngine.Elements.ForEach(o =>
                {
                    var pLine = o.Outline.ToNTSPolygon().ToDbPolylines().FirstOrDefault();
                    doorPlines.Add(pLine);
                });
            }
            return doorPlines;
        }
        */
        public List<Polyline> AllDoors(Polyline polyline)
        {
            var doorPlines = new List<Polyline>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var doorRecognitionEngine = new ThDB3DoorRecognitionEngine())
            {
                doorRecognitionEngine.Recognize(doorEngine.Results, polyline.Vertices());
                doorRecognitionEngine.Elements.ForEach(o =>
                {
                    var pLine = o.Outline.ToNTSPolygon().ToDbPolylines().FirstOrDefault();
                    doorPlines.Add(pLine);
                });
            }
            return doorPlines;
        }
        public List<Polyline> AllWindows(Polyline polyline)
        {
            var doorPlines = new List<Polyline>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var recognitionEngine = new ThDB3WindowRecognitionEngine())
            {
                recognitionEngine.Recognize(windowEngine.Results, polyline.Vertices());
                recognitionEngine.Elements.ForEach(o =>
                {
                    var pLine = o.Outline.ToNTSPolygon().ToDbPolylines().FirstOrDefault();
                    doorPlines.Add(pLine);
                });
            }
            return doorPlines;
        }
    }
}
