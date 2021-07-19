using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Engine;
using ThMEPWSS.HydrantConnectPipe.Model;

namespace ThMEPWSS.HydrantConnectPipe.Service
{
    public class ThStairsRoomService
    {
        public List<ThStairsRoom> GetStairsRoom(Point3dCollection selectArea)
        {
            using (var database = AcadDatabase.Active())
            using (var roomEngine = new ThRoomBuilderEngine())
            {
                var rooms = roomEngine.BuildFromMS(database.Database, selectArea);
                List<ThStairsRoom> stairsRooms = new List<ThStairsRoom>();
                foreach (var room in rooms)
                {
                    bool isStairRoom = false;
                    foreach(var tag in room.Tags)
                    {
                        if (tag.Contains("楼梯") || tag.Contains("LT"))
                        {
                            isStairRoom = true;
                            break;
                        }
                    }
                    if (isStairRoom)
                    {
                        stairsRooms.Add(ThStairsRoom.Create(room.Boundary));
                    }
                }
                return stairsRooms;
            }
        }
    }
}
