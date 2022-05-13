using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Service;

namespace ThMEPWSS.Hydrant.Data
{
    public class ThHydrantDoorOpeningExtractor : ThDoorOpeningExtractor
    {
        private const double EnlargeTolerance = 5.0; // 用于判断门两边是否有房间
        private const double MinimumAreaTolerance = 100.0; // 把面积小于此值的门过滤掉
        private const double BufferTolerance = 10.0; // 房间扩大容差

        public override void Extract(Database database, Point3dCollection pts)
        {
            var doors = new DBObjectCollection();
            //消火栓校核用到的门来自“AI-门”图层
            //var doorEngine = new ThDB3DoorRecognitionEngine();
            //doorEngine.Recognize(database, pts);
            //doorEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ForEach(o => doors.Add(o));
            var instance = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            instance.Extract(database, pts);
            instance.Polys.ForEach(o=> doors.Add(o));
            if (FilterMode == FilterMode.Window)
            {
                doors = FilterWindowPolygon(pts, doors.Cast<Entity>().ToList()).ToCollection();
            }

            // 用从图纸提取的Polyine 进行清洗和加工
            var garbages = new DBObjectCollection();
            garbages = garbages.Union(doors); // doors是从图纸提取的Clone体

            var door1s = Clean(doors);
            garbages = garbages.Union(door1s);

            var door2s = door1s.UnionPolygons();
            garbages = garbages.Union(door2s);

            var door3s = Clean(door2s);
            garbages = garbages.Union(door3s);

            var door4s = Buffer(door3s,BufferTolerance);
            garbages = garbages.Union(door4s);

            var door5s = Clean(door4s);
            garbages = garbages.Union(door5s);

            Doors.AddRange(door5s.OfType<Polyline>().Select(o => o.GetMinimumRectangle()).ToList());

            garbages.MDispose();
        }
        private DBObjectCollection Clean(DBObjectCollection doors)
        {
            var simplifer = new ThPolygonalElementSimplifier()
            {
                AREATOLERANCE = MinimumAreaTolerance,
            };
            var cleaner = new ThPolygonCleanService(simplifer);
            return cleaner.Clean(doors);
        }

        private DBObjectCollection Buffer(DBObjectCollection doors,double distance)
        {
            var results = new DBObjectCollection();
            doors.OfType<Polyline>().ForEach(p =>
            {
                var objs = p.Buffer(distance);
                results = results.Union(objs);
            });                
            return results;
        }

        public void FilterOuterDoors(List<Entity> rooms,List<Polyline> outsideFrames)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(rooms.ToCollection());
            var outsideFrameSpatialIndex = new ThCADCoreNTSSpatialIndex(outsideFrames.ToCollection());
            Doors = Doors.Where(o =>
            {
                var neighbors = spatialIndex.SelectCrossingPolygon(Buffer(o));
                if (neighbors.Count > 1)
                {
                    return true;
                }
                else
                {
                    if (outsideFrameSpatialIndex.SelectCrossingPolygon(o).Count>0)
                    {
                        return true;
                    }
                    foreach (var frame in outsideFrames)
                    {
                        if (frame.Contains(o.GetCenter()))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }).ToList();
        }

        private Polyline Buffer(Polyline door)
        {
            var objs = door.Buffer(EnlargeTolerance);
            return objs.Cast<Polyline>().OrderByDescending(o => o.Area).First();
        }
    }
}