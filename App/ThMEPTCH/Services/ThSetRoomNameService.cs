using AcHelper;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.Services
{
    internal class ThSetRoomNameService
    {
        public static void SetFromCurrentDb(List<FloorCurveEntity> rooms)
        {
            var marks = GetRoomNames(Active.Database, new Point3dCollection());
            Set(rooms, marks);
        }

        public static void Set(List<FloorCurveEntity> rooms, List<ThIfcTextNote> roomMarks)
        {
            roomMarks.ForEach(o =>
            {
                if(o.Geometry.Area>0.0)
                {
                    // Mark的Geometry是矩形框
                    var pts = o.Geometry.Vertices();
                    if(pts.Count>3)
                    {
                        var center = pts[0].GetMidPt(pts[2]);
                        var mt = Matrix3d.Scaling(0.5, center);
                        o.Geometry.TransformBy(mt);
                    }
                }
            });
            var textOutlines = roomMarks.Select(o => o.Geometry).ToCollection();
            var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(textOutlines);
            rooms.ForEach(r =>
            {
                var roomProperty = r.Property as RoomProperty;
                var objs = spatialIndex.SelectWindowPolygon(r.EntityCurve);
                var notes = roomMarks.Where(o=>objs.Contains(o.Geometry)).Select(o=>o.Text).Distinct().ToList();
                roomProperty.Name = string.Join(",", notes);
            });
        }

 
        private static List<ThIfcTextNote> GetRoomNames(Database database, Point3dCollection pts)
        {
            var results = new List<ThIfcTextNote>();
            // 获取DB门标注
            var dbDoorMarks = RecognizeDBRoomMarks(database, pts);
            results.AddRange(dbDoorMarks);

            // 获取AI门标注
            var aiDoorMarks = RecognizeAIRoomMarks(database, pts);
            results.AddRange(aiDoorMarks);

            return results;
        }
        private static List<ThIfcTextNote> RecognizeAIRoomMarks(Database database, Point3dCollection pts)
        {
            var engine = new ThAIRoomMarkRecognitionEngine();
            engine.Recognize(database, pts);
            engine.RecognizeMS(database, pts);
            return engine.Elements.OfType<ThIfcTextNote>().ToList();
        }

        private static List<ThIfcTextNote> RecognizeDBRoomMarks(Database database, Point3dCollection pts)
        {
            var engine = new ThDB3RoomMarkRecognitionEngine();
            engine.Recognize(database,pts);
            return engine.Elements.OfType<ThIfcTextNote>().ToList();
        }
    }
}
