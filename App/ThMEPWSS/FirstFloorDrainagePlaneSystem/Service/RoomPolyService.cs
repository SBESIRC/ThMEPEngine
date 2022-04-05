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
        public List<Dictionary<KeyValuePair<Polyline, List<string>>, int>> GetRoomDeep(Dictionary<Polyline, List<string>> rooms, List<Polyline> outUserFrames)
        {
            var bufferFrames = outUserFrames.Select(x => x.ExtendByLengthLine(100)).Where(x => x != null).ToList();
            var interDic = bufferFrames.ToDictionary(x => x, y => rooms.Where(x => x.Key.IsIntersects(y)).ToList())
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
        private List<Dictionary<KeyValuePair<Polyline, List<string>>, int>> GetOneRoomDeepDic(Dictionary<Polyline, List<KeyValuePair<Polyline, List<string>>>> oneDeepRooms, Dictionary<Polyline, List<KeyValuePair<Polyline, List<string>>>> allRoomDic)
        {
            var resRooms = new List<Dictionary<KeyValuePair<Polyline, List<string>>, int>>();
            allRoomDic = allRoomDic.Except(oneDeepRooms).ToDictionary(x => x.Key, y => y.Value); //去掉最开始深度的房间
            while (oneDeepRooms.Count > 0)
            {
                var firRoomDeep = oneDeepRooms.First();
                oneDeepRooms.Remove(firRoomDeep.Key);
                var firstDeepRooms = new Dictionary<Polyline, List<KeyValuePair<Polyline, List<string>>>>() { { firRoomDeep.Key, firRoomDeep.Value } };
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
        private Dictionary<KeyValuePair<Polyline, List<string>>, int> GetRoomDeep(Dictionary<Polyline, List<KeyValuePair<Polyline, List<string>>>> firstDeepRooms, Dictionary<Polyline, List<KeyValuePair<Polyline, List<string>>>> allRoomDic, int deep)
        {
            var roomDeepDic = new Dictionary<KeyValuePair<Polyline, List<string>>, int>();
            allRoomDic = allRoomDic.Except(firstDeepRooms).ToDictionary(x => x.Key, y => y.Value); //去掉最开始深度的房间
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
