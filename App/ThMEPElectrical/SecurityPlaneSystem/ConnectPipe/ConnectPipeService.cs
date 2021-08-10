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
            var iaPipes = connectService.ConnectPipe(connectBlock, rooms, columns, doors, floor);
            InsertConnectPipeService.InsertConnectPipe(iaPipes, ThMEPCommon.IA_PIPE_LAYER_NAME, ThMEPCommon.IA_PIPE_LINETYPE);

            AccessControlConnectService accessControlConnectService = new AccessControlConnectService(); 
            var acPipe = accessControlConnectService.ConnectPipe(connectBlock, rooms, columns, doors, floor);
            InsertConnectPipeService.InsertConnectPipe(acPipe, ThMEPCommon.AC_PIPE_LAYER_NAME, ThMEPCommon.AC_PIPE_LINETYPE);
        }
    }
}
