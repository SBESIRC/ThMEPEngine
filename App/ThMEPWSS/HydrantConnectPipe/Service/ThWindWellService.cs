using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
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
    public class ThWindWellService
    {
        public List<ThWindWell> GetWindWell(Point3dCollection selectArea)
        {
            using (var database = AcadDatabase.Active())
            using (var roomEngine = new ThRoomBuilderEngine())
            {
                var rooms = roomEngine.BuildFromMS(database.Database, selectArea);
                List<ThWindWell> windWells = new List<ThWindWell>();
                foreach (var room in rooms)
                {
                    bool isWindWell = false;
                    foreach(var tag in room.Tags)
                    {
                        if (tag.Contains("机房"))
                        {
                            break;
                        }
                        else if (tag.Contains("风") || tag.Contains("加压")
                            || tag.Contains("烟") || tag.Contains("井"))
                        {
                            isWindWell = true;
                            break;
                        }
                    }
                    if(isWindWell)
                    {
                        windWells.Add(ThWindWell.Create(room.Boundary));
                    }
                }
                return windWells;
            }
        }
    }
}
