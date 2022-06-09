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
    public class ThBuildRoomService
    {
        public List<ThBuildRoom> GetBuildRoom(Point3dCollection selectArea)
        {
            using (var database = AcadDatabase.Active())
            using (var roomEngine = new ThRoomBuilderEngine())
            {
                var rooms = roomEngine.BuildFromMS(database.Database, selectArea, false);
                List<ThBuildRoom> buildRooms = new List<ThBuildRoom>();
                foreach (var room in rooms)
                {
                    bool isBuildRoom = true;
                    foreach (var tag in room.Tags)
                    {
                        if (tag.Contains("楼梯") || tag.Contains("LT"))
                        {
                            isBuildRoom = false;
                            break;
                        }
                        if (tag.Contains("IT") || tag.Contains("数据") || tag.Contains("通讯")
                            || tag.Contains("消控") || tag.Contains("消防控制") || tag.Contains("PT")
                            || tag.Contains("KT") || tag.Contains("电梯") || tag.Contains("移动")
                            || tag.Contains("联通") || tag.Contains("货梯") || tag.Contains("进线")
                            || tag.Contains("专变") || tag.Contains("公变") || tag.Contains("变电所")
                            || tag.Contains("配电") || tag.Contains("弱电") || tag.Contains("强电")
                            || tag.Contains("开闭所"))
                        {
                            isBuildRoom = false;
                            break;
                        }
                        if ((tag.Contains("风") || tag.Contains("加压")|| tag.Contains("烟") || tag.Contains("井")) && !tag.Contains("机房"))
                        {
                            isBuildRoom = false;
                            break;
                        }
                    }
                    if (isBuildRoom)
                    {
                        buildRooms.Add(ThBuildRoom.Create(room.Boundary));
                    }
                }
                return buildRooms;
            }
        }
        public List<ThBuildRoom> GetBuildRoom(Point3dCollection selectArea,List<string> strNames)
        {
            using (var database = AcadDatabase.Active())
            using (var roomEngine = new ThRoomBuilderEngine())
            {
                var rooms = roomEngine.BuildFromMS(database.Database, selectArea);
                List<ThBuildRoom> buildRooms = new List<ThBuildRoom>();
                foreach (var room in rooms)
                {
                    bool isBuildRoom = false;
                    foreach (var tag in room.Tags)
                    {
                        if (strNames.Contains(tag))
                        {
                            isBuildRoom = true;
                            break;
                        }
                    }
                    if (isBuildRoom)
                    {
                        buildRooms.Add(ThBuildRoom.Create(room.Boundary));
                    }
                }
                return buildRooms;
            }
        }
        public List<ThBuildRoom> GetAllBuildRoom(Point3dCollection selectArea)
        {
            using (var database = AcadDatabase.Active())
            using (var roomEngine = new ThRoomBuilderEngine())
            {
                var rooms = roomEngine.BuildFromMS(database.Database, selectArea);
                List<ThBuildRoom> buildRooms = new List<ThBuildRoom>();
                foreach (var room in rooms)
                {
                    buildRooms.Add(ThBuildRoom.Create(room.Boundary));
                }
                return buildRooms;
            }
        }
    }
}
