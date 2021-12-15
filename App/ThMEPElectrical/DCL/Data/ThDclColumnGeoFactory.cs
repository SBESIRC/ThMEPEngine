using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.DCL.Data
{
    internal class ThDclColumnGeoFactory : ThExtractorBase
    {
        private DBObjectCollection Columns { get; set; }
        public ThDclColumnGeoFactory(DBObjectCollection columns)
        {
            Columns = columns;
            Category =BuiltInCategory.Column.ToString();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            throw new NotImplementedException();
        }
    }
}
