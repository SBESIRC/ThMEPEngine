using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.DSFEL.Model
{
    public class ExitRoomModel
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
        /// 门开启方向
        /// </summary>
        public string openDoor { get; set; }
    }
}
