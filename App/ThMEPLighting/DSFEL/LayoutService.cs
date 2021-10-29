using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;
using ThMEPLighting.DSFEL.ExitService;
using ThMEPLighting.DSFEL.Service;

namespace ThMEPLighting.DSFEL
{
    public class LayoutService
    {
        public void LayoutFELService(List<ThIfcRoom> roomInfo, List<Polyline> door, List<Line> centerLines, List<Polyline> holes)
        {
            //计算块出口
            CalExitService calExitService = new CalExitService();
            var exitInfo = calExitService.CalExit(roomInfo, door);

            //创建疏散路径
            CreateEvacuationPathService evacuationPath = new CreateEvacuationPathService();
            var cvaPaths = evacuationPath.CreatePath(exitInfo, centerLines, holes);
            using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var item in cvaPaths)
                {
                    foreach (var line in item.evacuationPaths)
                    {
                        db.ModelSpace.Add(line);
                    }
                }
            }
        }
    }
}
