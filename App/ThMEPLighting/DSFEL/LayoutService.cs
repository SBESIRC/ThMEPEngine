using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.DSFEL.ExitService;

namespace ThMEPLighting.DSFEL
{
    public class LayoutService
    {
        public void LayoutFELService(List<KeyValuePair<Polyline, string>> roomInfo, List<Polyline> door)
        {
            //计算块出口
            CalExitService calExitService = new CalExitService();
            var exitInfo = calExitService.CalExit(roomInfo, door);

            using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            {

            }
        }
    }
}
