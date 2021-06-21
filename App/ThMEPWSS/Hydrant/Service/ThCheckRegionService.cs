using System;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Hydrant.Service
{ 
    public class ThCheckRegionService
    {
        /// <summary>
        /// 房间的分割区域受保护情况
        /// key->房间轮廓
        /// value->(Item1->分割区域,Item2->校核是否通过,Item3->打印颜色)
        /// </summary>
        public Dictionary<Entity, List<Tuple<Entity,bool, int>>> CheckResults { get; private set; }
        /// <summary>
        /// 单股或双股检查(外部传入)
        /// </summary>
        public bool IsSingleStrands { get; set; }
        /// <summary>
        /// 要检查的房间(外部传入)
        /// </summary>
        public List<ThIfcRoom> Rooms { get; set; }
        /// <summary>
        /// 受消火栓或灭火器保护的区域(外部传入)
        /// </summary>
        public List<Entity> Covers { get; set; }
        public ThCheckRegionService()
        {
            IsSingleStrands = true; //不是单股，就是双股
            Covers = new List<Entity>();
            Rooms = new List<ThIfcRoom>();
            CheckResults = new Dictionary<Entity, List<Tuple<Entity, bool, int>>>();
        }
        public void Check()
        {
            var divideService  = new ThDivideRoomService(Rooms.Select(o => o.Boundary).ToList(), Covers);
            divideService.Divide();
            divideService.Results.ForEach(o =>
            {
                CheckResults.Add(o.Key, Check(o.Value));
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="roomSplitAreaBelongedProtectAreas"></param>
        /// <returns></returns>
        private List<Tuple<Entity, bool, int>> Check(Dictionary<Entity,List<Entity>> roomSplitAreaBelongedProtectAreas)
        {
            //key->房间的子区域，value->保护到此子区域的区域
            var results = new List<Tuple<Entity, bool, int>>();
            roomSplitAreaBelongedProtectAreas.ForEach(o =>
            {
                if (IsSingleStrands)
                {
                    // 区域被一个及以上的消火栓保护到即视为校核通过
                    if (o.Value.Count == 0)
                    {
                        //没有被消火栓保护到的区域为红色
                        results.Add(Tuple.Create(o.Key, false, 1));
                    }
                    else
                    {
                        //被一个及以上消火栓保护的区域不填充
                        results.Add(Tuple.Create(o.Key, true, 3));
                    }
                }
                else
                {
                    //区域被至少两个消火栓保护到才视为校核通过。
                    if (o.Value.Count == 0)
                    {
                        //没有被消火栓保护到的区域为红色
                        results.Add(Tuple.Create(o.Key, false, 1));
                    }
                    else if (o.Value.Count == 1)
                    {
                        //只被一个消火栓保护的区域为黄色
                        results.Add(Tuple.Create(o.Key, false, 2));
                    }
                    else
                    {
                        //被两个及以上消火栓保护的区域不填充
                        results.Add(Tuple.Create(o.Key, true, 3));
                    }
                }
            });
            return results;
        }
    }
}
