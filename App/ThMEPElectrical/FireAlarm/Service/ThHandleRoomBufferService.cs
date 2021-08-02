using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.FireAlarm.Service
{
    public class ThHandleRoomBufferService
    {
        public List<Entity> Rooms { get;  set; }
        public List<Entity> Walls { get;  private set; }
        public List<Entity> Parts { get; set; }
        private const double SmallAreaTolerance = 1.0;
        public ThHandleRoomBufferService(List<Entity> rooms)
        {
            Rooms = new List<Entity>();
            Walls = new List<Entity>();
            Parts = new List<Entity>();
        }
        public void Add(List<Entity> parts)
        {
            Parts.AddRange(parts);
        }
        public void Handle()
        {
            //构建索引
            var objs = new DBObjectCollection();
            Parts.ForEach(p=>objs.Add(p));
            Rooms.ForEach(r => objs.Add(r));
            objs = ThAuxiliaryUtils.FilterSmallArea(objs,1.0);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);

            var bufferService = new ThNTSBufferService();
            foreach (Entity room in Rooms)
            {
                var newBoundary = bufferService.Buffer(room, 200);
                var differSet = Difference(newBoundary, room);
                foreach (Entity set in differSet)
                {
                    var crossObjs = spatialIndex.SelectCrossingPolygon(set);
                    var results = DifferenceMP(set, crossObjs);
                    results = results.FilterSmallArea(SmallAreaTolerance);
                    Walls.AddRange(results.Cast<Entity>().ToList());
                }
            }
        }
        private DBObjectCollection Difference(Entity firstArea, Entity secondArea)
        {
            return firstArea.ToNTSPolygon().Difference(secondArea.ToNTSPolygon()).ToDbCollection(true);
        }
        private DBObjectCollection DifferenceMP(Entity area, DBObjectCollection intersectObjs)
        {
            return area.ToNTSPolygon().Difference(intersectObjs.UnionGeometries()).ToDbCollection(true);
        }
    }
}
