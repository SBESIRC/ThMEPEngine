using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;
using ThMEPWSS.Model;

namespace ThMEPWSS.DrainageSystemAG.Models
{
    
    /// <summary>
    /// 管径房间和其它房间(卫生间、厨房)的关系
    /// </summary>
    class TubeWellsRoomModel 
    {
        public string roomSpaceUuid { get; }
        public RoomModel roomModel { get; }
        public Point3d midPoint3d { get; }
        public List<string> intersectRoomIds { get; }
        public List<string> innerRoomIds { get; }
        public TubeWellsRoomModel(RoomModel room, Point3d midPoint3d) 
        {
            this.roomModel = room;
            this.midPoint3d = midPoint3d;
            this.roomSpaceUuid = room.thIFCRoom.Uuid;
            this.intersectRoomIds = new List<string>();
            this.innerRoomIds = new List<string>();
        }
    }
}
