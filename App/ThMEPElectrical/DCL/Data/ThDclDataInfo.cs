using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.DCL.Data
{
    internal class ThDclDataInfo
    {
        public ThMEPOriginTransformer Transformer { get; private set; }
        public ThDclDataInfo()
        {
            Transformer = new ThMEPOriginTransformer(Point3d.Origin);
        }
        public void Build(Database database,Point3dCollection pts)
        {

        }
    }
}
