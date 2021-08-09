using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADExtension;

namespace ThMEPElectrical.FireAlarm.Service
{
    public class ThHandleRoomBufferService
    {
        public List<Entity> Rooms { get;  set; }
        public List<Entity> Walls { get;  private set; }
        public List<Entity> Parts { get; set; }
        private const double SmallAreaTolerance = 1.0;
        private const double RoomBufferLength = 200.0;
        public ThHandleRoomBufferService(List<Entity> rooms)
        {
            Rooms = rooms;
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
                var newBoundary = bufferService.Buffer(room, RoomBufferLength);
                if(newBoundary==null)
                {
                    if (room is MPolygon mPolygon)
                    {
                        newBoundary = Buffer(mPolygon, RoomBufferLength);
                    }
                    else
                    {
                        continue;
                    }
                }
                if(newBoundary == null)
                {
                    continue;
                }
                var differSet = Difference(newBoundary, room);
                foreach (Entity set in differSet)
                {
                    var crossObjs = spatialIndex.SelectCrossingPolygon(set);
                    var results = DifferenceMP(set, Buffer(crossObjs));
                    results = results.FilterSmallArea(SmallAreaTolerance);
                    Walls.AddRange(results.Cast<Entity>().ToList());
                }
            }
        }
        private MPolygon Buffer(MPolygon mPolygon,double length)
        {
            var loops = mPolygon.Loops();
            var bufferService = new ThNTSBufferService();
            var shell = bufferService.Buffer(loops[0], length) as Polyline;
            if(shell == null)
            {
                return null;
            }
            var holes = new List<Curve>();
            for(int i=1;i<loops.Count;i++)
            {
                var hole = bufferService.Buffer(loops[i], -length);
                if(hole == null)
                {
                    continue;
                }
                else
                {
                    holes.Add(hole as Polyline);
                }
            }
            return ThMPolygonTool.CreateMPolygon(shell, holes);
        }
        private DBObjectCollection Buffer(DBObjectCollection objs)
        {
            var results = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            objs.Cast<Entity>().ForEach(e =>
            {
                var entity = bufferService.Buffer(e, 1.0);
                if(entity!=null)
                {
                    results.Add(entity);
                }
            });
            return results;
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
