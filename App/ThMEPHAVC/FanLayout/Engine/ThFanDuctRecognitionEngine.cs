using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPHVAC.Model;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using ThMEPHVAC.FanLayout.Service;
using ThMEPEngineCore.Model.Hvac;
using ThMEPEngineCore.Service.Hvac;
using ThMEPEngineCore.Service;

namespace ThMEPHVAC.FanLayout.Engine
{
    public class ThDuctInfo
    {
        public double width { set; get; }
        public double height { set; get; }
        public double markHeight { set; get; }
        public double fontHeight { set; get; }
    }
    public class ThFanDuctRecognitionEngine
    {
        public ObjectId GetFanDuctObjectId(Point3dCollection polygon)
        {
            using (var acadDb= Linq2Acad.AcadDatabase.Active())
            {
                var ductIds = ThDuctPortsReadComponent.Read_group_ids_by_type("Duct");
                var groups = ductIds.Select(o => acadDb.Element<Group>(o)).ToList();
                var entIds = groups.SelectMany(g => g.GetAllEntityIds().ToList());

                var ents  = entIds.Select(e => acadDb.Element<Entity>(e)).ToCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(ents);
                var dbObjects = spatialIndex.SelectCrossingPolygon(polygon);
                return groups.Where(g => dbObjects.OfType<Entity>().Where(e => g.Has(e)).Any()).Select(g => g.ObjectId).FirstOrDefault();

            }
        }

        public bool GetDuctInfo(Point3dCollection polygon,out ThDuctInfo info)
        {
            using (var database = Linq2Acad.AcadDatabase.Active())
            {
                info = new ThDuctInfo();

                double fontHeight = 300;

                var engine = new ThTCHDuctRecognitionEngine();
                engine.RecognizeEditor(polygon);
                var ductSeg = engine.Elements.OfType<ThIfcDuctSegment>().ToList();
                if(ductSeg.Count() == 0)
                {
                    ThMEPDuctExtractor AIDuctEngine = new ThMEPDuctExtractor();
                    AIDuctEngine.Recognize(database.Database, polygon);
                    ductSeg.AddRange(AIDuctEngine.Elements.OfType<ThIfcDuctSegment>());
                }
                if (ductSeg.Count() == 0)
                {
                    return false;
                }
                var param = ductSeg[0].Parameters;
                info.width = param.Width;
                info.height = param.Height;
                info.markHeight = param.MarkHeight;
                info.fontHeight = fontHeight;
                return true;
            }
        }
    }
}
