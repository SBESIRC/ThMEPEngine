using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

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
        private double AreaTolerance = 1.0;
        private double RoomOffsetLength = -1.0; //房间内缩的长度，防止房间的边与保护区域的边重合
        private double SplitAreaOffsetLength = -0.5; //房间分割区域内缩的长度，防止分割区域的边重复
        public ThDivideRoomService(Entity room,List<Entity> coverAreas)
        {
            if(!ThAuxiliaryUtils.DoubleEquals(RoomOffsetLength, 0.0))
            {
                var bufferService = new ThMEPEngineCore.Service.ThNTSBufferService();
                Room = bufferService.Buffer(room, RoomOffsetLength);
            }
            else
            {
                Room = room;
            }            
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
            UnProtectAreas = Subtraction(Room, CoverAreas.ToCollection()).Cast<Entity>().ToList(); // 获取未保护区域
            var intersectAreas = Intersect(); // 获取房间与其它保护区域相交的区域
            var bufferDic = ThHydrantUtils.BufferPolygon(intersectAreas, SplitAreaOffsetLength);//防止生成的面有边重复
            intersectAreas = bufferDic.Select(o => o.Value).ToList();
            ProtectAreas = Split(intersectAreas); // 分割相交的区域(如一个相交区域中有其它相交区域，需要分割)
        }

        private List<Entity> Intersect()
        {
            var results = new List<Entity>();
            CoverAreas.ForEach(e =>
            {
                var objs = Intersection(Room, e);
                results.AddRange(objs.Cast<Entity>());
            });
            return results; //去重
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
                    results.AddRange(subRes.Cast<Entity>().ToList());
                }
            });
            return results;
        }
        private DBObjectCollection Intersection(Entity first, Entity other)
        {
            var results = ThCADCoreNTSEntityExtension.Intersection(first, other);
            results = ClearZeroPolygon(results); //清除面积为零
            results = MakeValid(results); //解决自交的Case
            results = ClearZeroPolygon(results); //清除面积为零
            results = DuplicatedRemove(results); //去重
            var bufferDic = ThHydrantUtils.BufferPolygon(results.Cast<Entity>().ToList(), -1.0 * PolygonBufferLength);
            return bufferDic.Where(o => first.IsContains(o.Value)).Select(o => o.Key).ToCollection();
        }
        private DBObjectCollection Subtraction(Entity entity,DBObjectCollection objs)
        {
            //减去不在Entity里面的东西
            var results = ThCADCoreNTSEntityExtension.Difference(entity, objs,true);
            results = ClearZeroPolygon(results); //清除面积为零
            results = MakeValid(results); //解决自交的Case
            results = ClearZeroPolygon(results); //清除面积为零
            results = DuplicatedRemove(results); //去重
            var bufferDic = ThHydrantUtils.BufferPolygon(results.Cast<Entity>().ToList(), -1.0 * PolygonBufferLength);
            return bufferDic.Where(o => entity.IsContains(o.Value)).Select(o => o.Key).ToCollection();
        }
        private DBObjectCollection DuplicatedRemove(DBObjectCollection objs)
        {
            var sptialIndex = new ThCADCoreNTSSpatialIndex(objs);
            return sptialIndex.Geometries.Values.ToCollection();
        }     
        private DBObjectCollection ClearZeroPolygon(DBObjectCollection objs)
        {
            return objs.Cast<Entity>().Where(o =>
            {
                if(o is Polyline polyline)
                {
                    return polyline.Area > AreaTolerance;
                }
                else if(o is MPolygon mPolygon)
                {
                    return mPolygon.Area > AreaTolerance;
                }
                else
                {
                    return false;
                }
            }).ToCollection();
        }
        private DBObjectCollection MakeValid(DBObjectCollection polygons)
        {
            var results = new DBObjectCollection();
            polygons.Cast<Entity>().ForEach(o =>
            {
                if (o is Polyline polyline)
                {
                    var res = polyline.MakeValid();
                    res.Cast<Entity>().ForEach(e => results.Add(e));
                }
                else if(o is MPolygon mPolygon)
                {
                    var res = mPolygon.MakeValid();
                    res.Cast<Entity>().ForEach(e => results.Add(e));
                }
            });
            return results;
        }
    }
}
