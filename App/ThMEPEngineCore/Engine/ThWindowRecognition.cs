using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThWindowExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var windowDbExtension = new ThWindowDbExtension(database))
            {
                windowDbExtension.BuildElementCurves();
                Results = windowDbExtension.Windows.Select(o => new ThRawIfcBuildingElementData()
                {
                    Geometry = o,
                }).ToList();
            }
        }
    }

    public class ThWindowRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThWindowExtractionEngine();
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
                Elements.Add(ThIfcWindow.Create(o));
            });
        }
    }
}

   

