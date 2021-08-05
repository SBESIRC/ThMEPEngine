using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe
{
    public class ConnectPipeService
    {
        public void ConnectPipe(List<BlockReference> connectBlock, List<ThIfcRoom> rooms, List<Polyline> doors, List<Polyline> columns, List<Polyline> walls, ThEStoreys floor)
        {
            IntrucsionAlarmConnectService connectService = new IntrucsionAlarmConnectService();
            var pipes = connectService.ConnectPipe(connectBlock, rooms, columns, doors, floor);

            AccessControlConnectService accessControlConnectService = new AccessControlConnectService(); 
            pipes.AddRange(accessControlConnectService.ConnectPipe(connectBlock, rooms, columns, doors, floor));
            using (AcadDatabase db = AcadDatabase.Active())
            {
                foreach (var item in pipes)
                {
                    db.ModelSpace.Add(item);
                }
            }
        }
    }
}
