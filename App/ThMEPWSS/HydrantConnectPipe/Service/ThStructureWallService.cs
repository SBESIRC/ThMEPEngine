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
    public class ThStructureWallService
    {
        public List<ThStructureWall> GetStructureWalls(Point3dCollection pts)
        {
            var results = new List<ThStructureWall>();
            using (var database = AcadDatabase.Active())
            using (var archWallEngine = new ThDB3ArchWallRecognitionEngine())
            {
                archWallEngine.Recognize(database.Database, pts);
                List<ThIfcWall> structureWalls = archWallEngine.Elements.Cast<ThIfcWall>().ToList();
                foreach (var structureWall in structureWalls)
                { 
                    results.Add(ThStructureWall.Create(structureWall.Outline));
                }
            }
            return results;
        }
    }
}
