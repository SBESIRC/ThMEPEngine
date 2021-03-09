using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThDoorRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public double FindRatio { get; set; } = 1.0;
        public override void Recognize(Database database, Point3dCollection polygon)
        {        
            // 搜集门垛
            var doorStones = GetDoorStones(database, polygon);
            // 搜集门标注
            var doorMarks = GetDoorMarks(database, polygon);
            // 搜集门标注范围内的门垛
            var buildService = new ThCollectDoorStoneService(
                doorStones.Select(o=>o.Geometry).ToCollection(),
                doorMarks.Select(o => o.Data as Entity).ToCollection());
            buildService.Build();

            //构件障碍物索引服务
            ThObstacleSpatialIndexService.Instance.Build(database, polygon);

            // 创建门
            var buildDoor = new ThBuildDoorService();
            buildDoor.Build(buildService.Results);

            buildDoor.Outlines.ForEach(o => Elements.Add(new ThIfcDoor { Outline = o }));
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            var curves = new DBObjectCollection();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                var doorStoneSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                foreach (var filterObj in doorStoneSpatialIndex.SelectCrossingPolygon(polygon))
                {
                    curves.Add(filterObj as Curve);
                }
            }
            else
            {
                curves = objs;
            }
            curves.Cast<Curve>().ForEach(o => Elements.Add(new ThIfcDoor() { Outline = o }));
        }
        private List<ThRawIfcBuildingElementData> GetDoorStones(Database database, Point3dCollection polygon)
        {
            var engine = new ThDoorStoneExtractionEngine();
            engine.Extract(database);
            if (polygon.Count > 0)
            {
                var dbObjs = new DBObjectCollection();
                engine.Results.ForEach(o => dbObjs.Add(o.Geometry));
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                var filterObjs = spatialIndex.SelectCrossingPolygon(polygon);
                return engine.Results.Where(o=>filterObjs.Contains(o.Geometry)).ToList();
            }
            else
            {
                return engine.Results;
            }
        }
        private List<ThRawIfcBuildingElementData> GetDoorMarks(Database database, Point3dCollection polygon)
        {
            var engine = new ThDoorMarkExtractionEngine();
            engine.Extract(database);
            if (polygon.Count > 0)
            {
                var dbObjs = new DBObjectCollection();
                engine.Results.ForEach(o => dbObjs.Add(o.Geometry));
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                var filterObjs = spatialIndex.SelectCrossingPolygon(polygon);
                return engine.Results.Where(o => filterObjs.Contains(o.Geometry)).ToList();
            }
            else
            {
                return engine.Results;
            }
        }
    }
}
