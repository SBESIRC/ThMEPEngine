using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Service
{
    public class RoomPolyService
    {
        public List<Dictionary<Polyline, int>> GetRoomDeep(List<Polyline> rooms, List<Polyline> outUserFrames)
        {
            var bufferFrames = outUserFrames.Select(x => x.ExtendByLengthLine(100)).Where(x => x != null).ToList();
            var interDic = outUserFrames.ToDictionary(x => x, y => rooms.Where(x => x.IsIntersects(y)).ToList())
                .Where(x => x.Value.Count >= 0).ToDictionary(x => x.Key, y => y.Value);
            var oneDeepRooms = interDic.Where(x => x.Value.Count == 1).ToDictionary(x => x.Key, y => y.Value);

            var roomDeepDic = GetOneRoomDeepDic(oneDeepRooms, interDic);

            return roomDeepDic;
        }

        /// <summary>
        /// 计算房间深度
        /// </summary>
        /// <param name="oneDeepRooms"></param>
        /// <param name="allRoomDic"></param>
        /// <returns></returns>
        private List<Dictionary<Polyline, int>> GetOneRoomDeepDic(Dictionary<Polyline, List<Polyline>> oneDeepRooms, Dictionary<Polyline, List<Polyline>> allRoomDic)
        {
            var resRooms = new List<Dictionary<Polyline, int>>();
            allRoomDic = allRoomDic.Except(oneDeepRooms).ToDictionary(x => x.Key, y => y.Value); //去掉最开始深度的房间
            while (oneDeepRooms.Count > 0)
            {
                var firRoomDeep = oneDeepRooms.First();
                oneDeepRooms.Remove(firRoomDeep.Key);
                var firstDeepRooms = new Dictionary<Polyline, List<Polyline>>() { { firRoomDeep.Key, firRoomDeep.Value } };
                var resDeepRooms = GetRoomDeep(firstDeepRooms, allRoomDic, 1);
                allRoomDic = allRoomDic.Where(x => !x.Value.Any(y => resDeepRooms.Keys.Contains(y))).ToDictionary(x => x.Key, y => y.Value);
                oneDeepRooms = oneDeepRooms.Where(x => !x.Value.Any(y => resDeepRooms.Keys.Contains(y))).ToDictionary(x => x.Key, y => y.Value);
                resRooms.Add(resDeepRooms);
            }

            return resRooms;
        }

        /// <summary>
        /// 计算房间深度
        /// </summary>
        /// <param name="oneDeepRooms"></param>
        /// <param name="allRoomDic"></param>
        /// <param name="deep"></param>
        /// <returns></returns>
        private Dictionary<Polyline, int> GetRoomDeep(Dictionary<Polyline, List<Polyline>> firstDeepRooms, Dictionary<Polyline, List<Polyline>> allRoomDic, int deep)
        {
            var roomDeepDic = new Dictionary<Polyline, int>();
            allRoomDic = allRoomDic.Except(firstDeepRooms).ToDictionary(x => x.Key, y => y.Value); //去掉最开始深度的房间
            var deepRooms = new Dictionary<Polyline, List<Polyline>>();
            foreach (var rooms in firstDeepRooms)
            {
                foreach (var room in rooms.Value)
                {
                    if (!roomDeepDic.Keys.Contains(room))
                    {
                        roomDeepDic.Add(room, deep);
                    }
                }
            }

            var nextDeepRooms = allRoomDic.Where(x => roomDeepDic.Any(y => x.Value.Contains(y.Key))).ToDictionary(x => x.Key, y => y.Value);
            if (nextDeepRooms.Count > 0)
            {
                var getNextDeepRooms = GetRoomDeep(nextDeepRooms, allRoomDic, ++deep);
                foreach (var nRoom in getNextDeepRooms)
                {
                    if (!roomDeepDic.Keys.Contains(nRoom.Key))
                    {
                        roomDeepDic.Add(nRoom.Key, nRoom.Value);
                    }
                }
            }
            return roomDeepDic;
        }
    }
}
