using Linq2Acad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPWSS.Pipe.Model;
using System.Linq;
using ThMEPEngineCore.Model;
using NFox.Cad;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWellExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var windowDbExtension = new ThWellsDbExtension(database))
            {
                windowDbExtension.BuildElementCurves();
                Results = windowDbExtension.Wells.Select(o => new ThRawIfcBuildingElementData()
                {
                    Geometry = o,
                }).ToList();
            }
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new System.NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            throw new System.NotImplementedException();
        }
    }
    public class ThWWellRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public IEnumerable<object> Datas { get; internal set; }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThWellExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            List<Entity> ents = new List<Entity>();
            var dbObjs = datas.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                foreach (var filterObj in spatialIndex.SelectCrossingPolygon(polygon))
                {
                    ents.Add(filterObj as Entity);
                }
            }
            else
            {
                ents = dbObjs.Cast<Entity>().ToList();
            }
            ents.ForEach(o =>
            {
                Elements.Add(ThWWell.Create(o));
            });
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }
    }
}
