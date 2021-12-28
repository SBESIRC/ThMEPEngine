using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;

namespace ThMEPElectrical.AFAS.Model
{
    public class ThAFASDataPass
    {
        public List<ThExtractorBase> Extractors { get; set; }
        public ThMEPOriginTransformer Transformer { get; set; }
        public Point3dCollection SelectPts { get; set; }

        public static ThAFASDataPass Instance = new ThAFASDataPass();
        public ThAFASDataPass()
        {
            Transformer = new ThMEPOriginTransformer();
            Extractors = new List<ThExtractorBase>();
            SelectPts = new Point3dCollection();
        }



    }
}
