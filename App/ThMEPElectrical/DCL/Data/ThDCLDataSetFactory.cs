using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Data;

namespace ThMEPElectrical.DCL.Data
{
    internal class ThDCLDataSetFactory : ThMEPDataSetFactory
    {
        public ThDCLDataSetFactory()
        {
        }
        protected override ThMEPDataSet BuildDataSet()
        {
            throw new NotImplementedException();
        }

        protected override void GetElements(Database database, Point3dCollection collection)
        {
            throw new NotImplementedException();
        }
    }
}
