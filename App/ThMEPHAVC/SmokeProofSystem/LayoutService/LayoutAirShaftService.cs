using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPHVAC.SmokeProofSystem.Model;
using ThMEPHVAC.SmokeProofSystem.Service;
using ThMEPHVAC.SmokeProofSystem.Utils;

namespace ThMEPHVAC.SmokeProofSystem.LayoutService
{
    public class LayoutAirShaftService
    {
        Dictionary<Polyline, List<string>> roomInfo;
        List<SmkBlockModel> blocks;
        List<Polyline> doors;
        double doorDis = 500;
        public LayoutAirShaftService(Dictionary<Polyline, List<string>> _roomInfo, List<SmkBlockModel> _blocks, List<Polyline> _doors)
        {
            roomInfo = _roomInfo;
            blocks = _blocks;
            doors = _doors;
        }

        public void Layout()
        {
            GetLayoutRoomInfo getLayoutRoomInfo = new GetLayoutRoomInfo(roomInfo, blocks);
            var layoutInfo = getLayoutRoomInfo.GetLayoutRoom();
            foreach (var lInfo in layoutInfo)
            {
                if (lInfo.CompressionBlock.Count > 0)           //机械送风(布置风井风口和标注)
                {
                    lInfo.AirShaftRooms = lInfo.AirShaftRooms.OrderByDescending(x => x.Area).ToList();
                    var otherInfos = layoutInfo.Where(x => x != lInfo).ToList();
                    var roomMatchInfos = lInfo.AirShaftRooms.ToDictionary(x => x, y => otherInfos.Where(z => z.AirShaftRooms.Contains(y)).ToList());
                    var aloneRooms = roomMatchInfos.Where(x => x.Value.Count == 0).ToList();
                    var otherRooms = roomMatchInfos.Where(x => x.Value.Count > 0).ToDictionary(x => x.Key, y => y.Value);
                    var publicRoomDic = new Dictionary<Polyline, List<SmkProofLayoutModel>>();
                    foreach (var room in otherRooms)
                    {
                        publicRoomDic.Add(room.Key, CalNeedUseSameRoomInfo(room.Key, room.Value));
                    }
                    //布置立管
                    ///
                    //
                }
                else if (lInfo.NaturalBlock.Count > 0)          //自然送风(布置标注)
                {

                }
            }
        }

        /// <summary>
        /// 判断是否需要公用一个风井
        /// </summary>
        /// <param name="AirShaftRoom"></param>
        /// <param name="smkProofLayoutModels"></param>
        /// <returns></returns>
        private List<SmkProofLayoutModel> CalNeedUseSameRoomInfo(Polyline AirShaftRoom, List<SmkProofLayoutModel> smkProofLayoutModels)
        {
            var needUseRooms = new List<SmkProofLayoutModel>();
            foreach (var lModel in smkProofLayoutModels)
            {
                if (lModel.AirShaftRooms.Count == 1)
                {
                    needUseRooms.Add(lModel);
                    continue;
                }
                if (lModel.RoomType == RoomType.StairRoom && lModel.CompressionBlock.Count > 1)
                {
                    if (CheckService.CheckRoomLayout(AirShaftRoom, lModel))
                    {
                        lModel.AirShaftRooms.Remove(AirShaftRoom);
                        continue;
                    }
                }
                else
                {
                    lModel.AirShaftRooms.Remove(AirShaftRoom);
                    continue;
                }
                needUseRooms.Add(lModel);
            }

            return needUseRooms;
        }

        private void LayoutRiser(SmkProofLayoutModel smkProofLayoutModel, Dictionary<Polyline, List<SmkProofLayoutModel>> publicRoomDic)
        {
            var aloneRooms = smkProofLayoutModel.AirShaftRooms.Where(x => !publicRoomDic.Keys.Contains(x)).ToList();
            smkProofLayoutModel.CompressionBlock = smkProofLayoutModel.CompressionBlock.OrderBy(x => x.AirVolume).ToList(); //多个块就拿风量小的块优先
            if (aloneRooms.Count > 0)
            {
                var needBlock = new List<SmkBlockModel>() { smkProofLayoutModel.CompressionBlock.First() };
                if (CheckService.CheckRoomLayout(aloneRooms[0], smkProofLayoutModel))
                {
                    if (smkProofLayoutModel.RoomType == RoomType.StairRoom && smkProofLayoutModel.CompressionBlock.Count > 1)
                    {
                        needBlock.Add(smkProofLayoutModel.CompressionBlock[1]);
                    }
                }
            }
            if (smkProofLayoutModel.RoomType == RoomType.StairRoom && smkProofLayoutModel.CompressionBlock.Count > 1)
            {

            }
        }

        private void LayoutBlock(Polyline AirShaftRoom, Polyline layoutRoom, List<SmkBlockModel> smkBlockModels)
        {
            var parallelDic = AirShaftRoom.GetTangentEdge(layoutRoom, 410);
            if (parallelDic.Count > 0)
            {
                if (parallelDic.Count > 1)
                {

                }
            }

        }
    }
}
