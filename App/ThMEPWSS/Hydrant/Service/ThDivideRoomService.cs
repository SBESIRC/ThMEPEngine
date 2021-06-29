using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.OverlayNG;

namespace ThMEPWSS.Hydrant.Service
{
    public class ThDivideRoomService
    {
        /// <summary>
        /// 所有Entity不能带弧
        /// </summary>
        private Entity Room { get; set; }
        private List<Entity> CoverAreas { get; set; }
        public List<Entity> UnProtectAreas { get; set; }
        public List<Entity> ProtectAreas { get; set; }
        private double PolygonBufferLength = 5.0;
        public ThDivideRoomService(Entity room,List<Entity> coverAreas)
        {
            Room = room;
            CoverAreas = coverAreas;
            UnProtectAreas = new List<Entity>();
            ProtectAreas = new List<Entity>();
        }
        public void Divide()
        {
            if(CoverAreas.Count==0)
            {
                UnProtectAreas = new List<Entity>() { Room.Clone() as Entity};
                return;
            }
            UnProtectAreas = Subtract(); // 获取未保护区域
            var intersectAreas = Intersect(); // 获取房间与其它保护区域相交的区域
            ProtectAreas = Split(intersectAreas); // 分割相交的区域(如一个相交区域中有其它相交区域，需要分割)
        }

        private List<Entity> Subtract()
        {
            // 用房间轮廓去保护区域做差集      
            var results = Subtraction(Room, CoverAreas.ToCollection()).Cast<Entity>().ToList();
            return DuplicatedRemove(results);
        }

        private List<Entity> Intersect()
        {
            var results = new List<Entity>();
            CoverAreas.ForEach(e =>
            {
                var objs = Intersection(Room, e);
                results.AddRange(objs.Cast<Entity>());
            });
            return DuplicatedRemove(results); //去重
        }
        private List<Entity> Split(List<Entity> areas)
        {
            var results = new List<Entity>();
            var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(areas.ToCollection());
            areas.ForEach(o =>
            {
                var objs = spatialIndex.SelectCrossingPolygon(o);
                objs.Remove(o);
                if(objs.Count == 0)
                {
                    results.Add(o);
                }
                else
                {
                    var subRes = Subtraction(o, objs);
                    results.AddRange(DuplicatedRemove(subRes.Cast<Entity>().ToList()));
                }
            });
            return results;
        }
        private DBObjectCollection Intersection(Entity first, Entity other)
        {
            var results = ThCADCoreNTSEntityExtension.Intersection(first, other);
            var bufferDic = ThHydrantUtils.BufferPolygon(results.Cast<Entity>().ToList(), -1.0 * PolygonBufferLength);
            return bufferDic.Where(o => first.IsContains(o.Value)).Select(o => o.Key).ToCollection();
        }
        private DBObjectCollection Subtraction(Entity entity,DBObjectCollection objs)
        {
            //减去不在Entity里面的东西
            var results = entity.ToNTSPolygon()
                .Difference(CoverAreas.ToCollection().UnionGeometries())
                .ToDbCollection(true);
            var bufferDic = ThHydrantUtils.BufferPolygon(results.Cast<Entity>().ToList(), -1.0 * PolygonBufferLength);
            return bufferDic.Where(o => entity.IsContains(o.Value)).Select(o => o.Key).ToCollection();
        }
        private List<Entity> DuplicatedRemove(List<Entity> ents)
        {
            var sptialIndex = new ThCADCoreNTSSpatialIndex(ents.ToCollection());
            return sptialIndex.Geometries.Values.Cast<Entity>().ToList();
        }        
    }
}
