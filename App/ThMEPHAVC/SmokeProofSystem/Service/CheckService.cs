using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPHVAC.SmokeProofSystem.Model;

namespace ThMEPHVAC.SmokeProofSystem.Service
{
    public static class CheckService
    {
        /// <summary>
        /// 判断房间所有的其他风井能否全部布置下这个房间所需要的风井块
        /// </summary>
        /// <param name="AirShaftRoom"></param>
        /// <param name="smkProofLayoutModel"></param>
        /// <returns></returns>
        public static bool CheckRoomLayout(Polyline AirShaftRoom, SmkProofLayoutModel smkProofLayoutModel)
        {
            var otherAirShafts = smkProofLayoutModel.AirShaftRooms.Where(x => x != AirShaftRoom).ToList();
            var canUseArea = otherAirShafts.Sum(x => { var bufferRoom = x.Buffer(-100)[0] as Polyline; return bufferRoom.Area; });
            var needArea = smkProofLayoutModel.AirVolume / (20 * 3600);
            if (smkProofLayoutModel.RoomType == RoomType.StairRoom && smkProofLayoutModel.CompressionBlock.Count > 1)
            {
                smkProofLayoutModel.CompressionBlock = smkProofLayoutModel.CompressionBlock.OrderByDescending(x => x.AirVolume).ToList();
                needArea = smkProofLayoutModel.CompressionBlock[0].AirVolume / (20 * 3600) + smkProofLayoutModel.CompressionBlock[1].AirVolume / (20 * 3600); //一个房间内最多需要两个送风块
            }
            
            return canUseArea > needArea;
        }
    }
}
