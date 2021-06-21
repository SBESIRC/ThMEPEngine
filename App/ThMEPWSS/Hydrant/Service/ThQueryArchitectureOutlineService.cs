using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPWSS.Hydrant.Service
{
    public class ThQueryArchitectureOutlineService : IArchitectureOutlineData
    {
        public string ElementLayer { get; set; }
        public List<Entity> Outlines  { get; set; }
        public ThQueryArchitectureOutlineService()
        {
            ElementLayer = "";
            Outlines = new List<Entity>();
        }

        public List<Entity> Query(Database db, Point3dCollection pts)
        {
            var extractService = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            extractService.Extract(db, pts);
            return extractService.Polys.Cast<Entity>().ToList();
        }
    }
}
