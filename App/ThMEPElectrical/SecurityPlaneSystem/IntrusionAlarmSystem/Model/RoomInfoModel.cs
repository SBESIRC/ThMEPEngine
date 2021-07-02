using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem.Model
{
    public class RoomInfoModel
    {
        /// <summary>
        /// 房间a
        /// </summary>
        public List<string> roomA = new List<string>();

        /// <summary>
        /// 房间b
        /// </summary>
        public List<string> roomB = new List<string>();

        /// <summary>
        /// 楼层限制
        /// </summary>
        public string floorName { get; set; }

        /// <summary>
        /// 房间a采取的措施
        /// </summary>
        public LayoutType roomAHandle { get; set; }

        /// <summary>
        /// 房间b采取的措施
        /// </summary>
        public LayoutType roomBHandle { get; set; }

        /// <summary>
        /// 判断房间应该的连接类型
        /// </summary>
        public ConnectType connectType { get; set; }
    }

    public enum LayoutType
    {
        /// <summary>
        /// 不需要布置
        /// </summary>
        Nothing,

        /// <summary>
        /// 残卫报警
        /// </summary>
        DisabledToiletAlarm,

        /// <summary>
        /// 紧急报警
        /// </summary>
        EmergencyAlarm,

        /// <summary>
        /// 壁装红外
        /// </summary>
        InfraredWallMounting,

        /// <summary>
        /// 吸顶红外
        /// </summary>
        InfraredHoisting,

        /// <summary>
        /// 壁装双鉴
        /// </summary>
        DoubleWallMounting,

        /// <summary>
        /// 吸顶双鉴
        /// </summary>
        DoubleHositing,
    }

    public enum ConnectType
    {
        /// <summary>
        /// 正常房间连接
        /// </summary>
        Normal,

        /// <summary>
        /// 无
        /// </summary>
        NoCennect,

        /// <summary>
        /// All
        /// </summary>
        AllConnect,
    }
}
