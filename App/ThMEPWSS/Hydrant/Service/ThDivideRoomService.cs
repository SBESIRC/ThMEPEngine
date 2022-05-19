using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

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

            // 再做一步清洗
            var protectAreaObjs = ProtectAreas.ToCollection().Clean();
            ProtectAreas = protectAreaObjs.OfType<Entity>().ToList();

            var unProtectAreaObjs = UnProtectAreas.ToCollection().Clean();
            UnProtectAreas = unProtectAreaObjs.OfType<Entity>().ToList();
        }

        private void Print(DBObjectCollection objs)
        {
            // for test
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                var clones = objs.Clone();
                clones.OfType<Entity>().ToList().ForEach(e =>
                {
                    acadDb.ModelSpace.Add(e);
                    e.ColorIndex = 6;
                });
            }
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
            areas = areas.ToCollection().ClearZeroPolygon().Cast<Entity>().ToList();
            areas = areas.ToCollection().MakeValid().Cast<Entity>().ToList();
            areas = areas.ToCollection().ClearZeroPolygon().Cast<Entity>().ToList();
            var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(areas.ToCollection());
            var bufferService = new ThMEPEngineCore.Service.ThNTSBufferService();
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
                    var entity = bufferService.Buffer(o, -0.1); // fixed TopologyException
                    var subRes = Subtraction(entity, objs);
                    results.AddRange(subRes.Cast<Entity>().ToList());
                }
            });
            return results;
        }

        private DBObjectCollection Intersection(Entity first, Entity other)
        {
            var results = ThCADCoreNTSEntityExtension.Intersection(first, other,true);
            results = RemoveDBpoints(results);
            results = results.ClearZeroPolygon(); //清除面积为零
            results = results.MakeValid(); //解决自交的Case
            results = results.ClearZeroPolygon(); //清除面积为零
            results = DuplicatedRemove(results); //去重
            var bufferDic = ThHydrantUtils.BufferPolygon(results.Cast<Entity>().ToList(), -1.0 * PolygonBufferLength);
            return bufferDic.Where(o => first.EntityContains(o.Value) && other.EntityContains(o.Value)).Select(o => o.Key).ToCollection();
        }

        private DBObjectCollection Subtraction(Entity entity,DBObjectCollection objs)
        {
            //减去不在Entity里面的东西
            var results = ThCADCoreNTSEntityExtension.Difference(entity, objs,true);
            results = RemoveDBpoints(results);
            results = results.ClearZeroPolygon(); //清除面积为零
            results = results.MakeValid(); //解决自交的Case
            results = results.ClearZeroPolygon(); //清除面积为零
            results = DuplicatedRemove(results); //去重
            var bufferDic = ThHydrantUtils.BufferPolygon(results.Cast<Entity>().ToList(), -1.0 * PolygonBufferLength);
            return bufferDic.Where(o => entity.EntityContains(o.Value)).Select(o => o.Key).ToCollection();
        }

        private DBObjectCollection RemoveDBpoints(DBObjectCollection objs)
        {
            return objs.OfType<Entity>().Where(e => !(e is DBPoint)).ToCollection();
        }

        private DBObjectCollection DuplicatedRemove(DBObjectCollection objs)
        {
            return ThCADCoreNTSGeometryFilter.GeometryEquality(objs);
        }     
    }
}
