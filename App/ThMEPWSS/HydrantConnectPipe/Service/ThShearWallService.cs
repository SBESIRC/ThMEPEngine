using Autodesk.AutoCAD.DatabaseServices;
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
    class ThShearWallService
    {
        public List<ThShearWall> GetWallEdges(Point3dCollection pts)
        {
            var results = new List<ThShearWall>();
            using (var database = AcadDatabase.Active())
            using (var shearWallEngine = new ThShearWallRecognitionEngine())
            {
                shearWallEngine.Recognize(database.Database, pts);
                List<ThIfcWall> shearWalls = shearWallEngine.Elements.Cast<ThIfcWall>().ToList();
                foreach(var shearWall in shearWalls)
                {
                    results.Add(ThShearWall.Create(shearWall.Outline));
                }
            }
            return results;
        }
    }
}
