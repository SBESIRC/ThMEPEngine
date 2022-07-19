using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPHVAC.SmokeProofSystem.Model;

namespace ThMEPHVAC.SmokeProofSystem.LayoutService
{
    public class GetLayoutRoomInfo
    {
        readonly double bufferDis = 210;
        Dictionary<Polyline, List<string>> roomInfo;
        List<SmkBlockModel> blocks;
        List<string> airShaftLst = new List<string>() {
            "风",
            "正压",
        };

        public GetLayoutRoomInfo(Dictionary<Polyline, List<string>> _roomInfo, List<SmkBlockModel> _blocks)
        {
            roomInfo = _roomInfo;
            blocks = _blocks;
        }

        public List<SmkProofLayoutModel> GetLayoutRoom()
        {
            var models = GetRoomInfo();
            var airShaftRooms = GetAirShaftRoom();
            return MatchLayoutRoom(models, airShaftRooms);
        }

        /// <summary>
        /// 匹配送风房间和风井
        /// </summary>
        /// <param name="smkProofRooms"></param>
        /// <param name="airShaftRooms"></param>
        /// <returns></returns>
        private List<SmkProofLayoutModel> MatchLayoutRoom(List<SmkProofLayoutModel> smkProofRooms, Dictionary<Polyline, List<string>> airShaftRooms)
        {
            var bufferAirShaftRooms = airShaftRooms.ToDictionary(x => x.Key, y => y.Key.Buffer(bufferDis)[0] as Polyline);
            foreach (var smkModel in smkProofRooms)
            {
                if (smkModel.CompressionBlock.Count > 0)            //加压送风下才需要布置风口
                {
                    var bufferRoom = smkModel.Room.Key.Buffer(bufferDis)[0] as Polyline;
                    smkModel.AirShaftRooms = bufferAirShaftRooms.Where(x => x.Value.IsIntersects(bufferRoom)).Select(x => x.Key).ToList();
                }
            }

            foreach (var smkModel in smkProofRooms)
            {
                if (smkModel.AirShaftRooms.Count > 0)
                {
                    var otherRooms = smkProofRooms.Where(x => x != smkModel).ToList();
                    var modelAirShaftRooms = smkModel.AirShaftRooms;
                    foreach (var room in modelAirShaftRooms)
                    {
                        var sameRooms = otherRooms.Where(x => x.AirShaftRooms.Any(y => y == room)).ToList();
                        if (sameRooms.Count > 0)
                        {
                            if (smkModel.RoomType == RoomType.StairRoom && smkModel.AirShaftRooms.Count > 2 && smkModel.CompressionBlock.Count >= 2)
                            {
                                smkModel.AirShaftRooms.Remove(room);
                            }
                            else if (smkModel.AirShaftRooms.Count > 1)
                            {
                                smkModel.AirShaftRooms.Remove(room);
                            }
                            foreach (var sRoom in sameRooms)
                            {
                                if (sRoom.RoomType == RoomType.StairRoom && sRoom.AirShaftRooms.Count > 2 && sRoom.CompressionBlock.Count >= 2)
                                {
                                    sRoom.AirShaftRooms.Remove(room);
                                }
                                else if (sRoom.AirShaftRooms.Count > 1)
                                {
                                    sRoom.AirShaftRooms.Remove(room);
                                }
                            }
                        }
                    }
                }
            }

            return smkProofRooms;
        }

        /// <summary>
        /// 获取风井房间
        /// </summary>
        /// <returns></returns>
        private Dictionary<Polyline, List<string>> GetAirShaftRoom()
        {
            return roomInfo.Where(x => x.Value.Any(y => airShaftLst.Any(z => y.Contains(z)))).ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// 筛选出需要送风的房间
        /// </summary>
        /// <returns></returns>
        private List<SmkProofLayoutModel> GetRoomInfo()
        {
            List<SmkProofLayoutModel> models = new List<SmkProofLayoutModel>();
            foreach (var info in roomInfo)
            {
                var roomBlocks = blocks.Where(x => info.Key.Contains(x.Position)).ToList();
                if (roomBlocks.Count > 0)
                {
                    SmkProofLayoutModel smkProofLayoutModel = new SmkProofLayoutModel();
                    smkProofLayoutModel.Room = info;
                    smkProofLayoutModel.RoomType = roomBlocks.First().RoomType;
                    smkProofLayoutModel.AirVolume = 0;
                    if (roomBlocks.Count >= 0)
                    {
                        foreach (var rBlock in roomBlocks)
                        {
                            if (rBlock.BlockType == BlockType.Compression)
                            {
                                smkProofLayoutModel.CompressionBlock.Add(rBlock);
                                if (rBlock.AirVolume > smkProofLayoutModel.AirVolume)
                                {
                                    smkProofLayoutModel.AirVolume = rBlock.AirVolume;
                                }
                            }
                            else if (rBlock.BlockType == BlockType.Natural)
                            {
                                smkProofLayoutModel.NaturalBlock.Add(rBlock);
                            }
                        }
                    }
                    models.Add(smkProofLayoutModel);
                }
            }

            return models.Where(x => x.NaturalBlock.Count > 0 || x.CompressionBlock.Count > 0).ToList(); ;
        }
    }
}
