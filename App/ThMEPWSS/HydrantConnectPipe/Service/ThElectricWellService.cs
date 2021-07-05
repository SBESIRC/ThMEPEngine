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
    public class ThElectricWellService
    {
        public List<ThElectricWell> GetElectricWell(Point3dCollection selectArea)
        {
            using (var database = AcadDatabase.Active())
            using (var roomEngine = new ThRoomBuilderEngine())
            {
                var rooms = roomEngine.BuildFromMS(database.Database, selectArea);
                List<ThElectricWell> fireHydrantPipes = new List<ThElectricWell>();
                foreach (var room in rooms)
                {
                    bool isElectricWell = false;
                    foreach(var tag in room.Tags)
                    {
                        if (tag.Contains("IT") || tag.Contains("数据") || tag.Contains("通讯")
                            || tag.Contains("消控") ||tag.Contains("消防控制") || tag.Contains("PT")
                            || tag.Contains("KT") ||tag.Contains("电梯") || tag.Contains("移动")
                            || tag.Contains("联通") ||tag.Contains("货梯") || tag.Contains("进线")
                            || tag.Contains("专变") ||tag.Contains("公变") || tag.Contains("变电所")
                            || tag.Contains("配电") || tag.Contains("弱电") || tag.Contains("强电")
                            || tag.Contains("开闭所"))
                        {
                            isElectricWell = true;
                            break;
                        }
                    }

                    if (isElectricWell)
                    {
                        fireHydrantPipes.Add(ThElectricWell.Create(room.Boundary));
                    }
                }
                return fireHydrantPipes;
            }
        }
    }
}
