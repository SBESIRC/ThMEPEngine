using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPWSS.FlushPoint.Service;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.IO;

namespace ThMEPWSS.FlushPoint.Data
{
    public class ThObstacleExtractor : ThExtractorBase, IPrint
    {
        public Dictionary<string, List<Curve>> ObstacleDic { get; private set; }

        public ThObstacleExtractor()
        {
            ObstacleDic = new Dictionary<string, List<Curve>>();
            Category = BuiltInCategory.Obstacle.ToString();
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            ObstacleDic.ForEach(o =>
            {
                o.Value.ForEach(e =>
                {
                    var geometry = new ThGeometry();
                    geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                    //geometry.Properties.Add(NamePropertyName, o.Key);
                    geometry.Boundary = e;
                    geos.Add(geometry);
                });
            });
            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            // 1.  需要躲避的元素
            // 1.1 给排水元素
            var extractService = new ThObstacleExtractService();
            extractService.Extract(database, pts);
            ObstacleDic = extractService.BlkEntityDic;

            // 1.2 门&防火卷帘
            var doorService = new ThDoorOpeningExtractor();
            doorService.Extract(database, pts);
            ObstacleDic.Add("DoorOpening", doorService.Doors.Cast<Curve>().ToList());

            // 1.3 窗户
            var windowService = new ThWindowExtractor();
            windowService.Extract(database, pts);
            ObstacleDic.Add("Window", windowService.Windows.Cast<Curve>().ToList());
        }

        public void Print(Database database)
        {
            ObstacleDic.ForEach(o =>
            {
                o.Value.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
            });
        }
    }
}
