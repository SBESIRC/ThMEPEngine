using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.SmokeProofSystem.Model
{
    public class SmkProofLayoutModel
    {
        /// <summary>
        /// 房间信息
        /// </summary>
        public KeyValuePair<Polyline, List<string>> Room { get; set; }

        /// <summary>
        /// 风井
        /// </summary>
        public List<Polyline> AirShaftRooms { get; set; }

        /// <summary>
        /// 房间类型
        /// </summary>
        public RoomType RoomType { get; set; }

        /// <summary>
        /// 风量
        /// </summary>
        public double AirVolume { get; set; }

        /// <summary>
        /// 房间内的加压送风块
        /// </summary>
        public List<SmkBlockModel> CompressionBlock = new List<SmkBlockModel>();

        /// <summary>
        /// 房间内的自然送风块
        /// </summary>
        public List<SmkBlockModel> NaturalBlock = new List<SmkBlockModel>();
    }

    public enum RoomType
    {
        /// <summary>
        /// 前室
        /// </summary>
        FrontRoom,

        /// <summary>
        /// 楼梯间
        /// </summary>
        StairRoom,
    }
}
