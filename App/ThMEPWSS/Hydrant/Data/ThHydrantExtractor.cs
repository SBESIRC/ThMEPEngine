using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPWSS.Engine;
using ThMEPWSS.Hydrant.Engine;

namespace ThMEPWSS.Hydrant.Data
{
    public class ThHydrantExtractor : ThExtractorBase
    {
        private List<DBPoint> Hydrants { get; set; }
        public ThHydrantExtractor()
        {
            Category = BuiltInCategory.Equipment.ToString();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            throw new NotImplementedException();
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
           


        }
    }
}
