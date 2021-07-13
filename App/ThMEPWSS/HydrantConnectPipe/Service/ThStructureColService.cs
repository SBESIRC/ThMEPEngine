using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPWSS.HydrantConnectPipe.Model;

namespace ThMEPWSS.HydrantConnectPipe.Service
{
    public class ThStructureColService
    {
        public List<ThStructureCol> GetStructureCols(Point3dCollection pts)
        {
            var results = new List<ThStructureCol>();
            using (var database = AcadDatabase.Active())
            using (var columnEngine = new ThColumnRecognitionEngine())
            {
                columnEngine.Recognize(database.Database, pts);
                List<ThIfcColumn> structureCols = columnEngine.Elements.Cast<ThIfcColumn>().ToList();
                foreach (var structureCol in structureCols)
                {
                    results.Add(ThStructureCol.Create(structureCol.Outline));
                }
            }
            return results;
        }
    }
}
