using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Command;
using ThMEPWSS.Service;

namespace ThMEPWSS
{
    public class ThFirstFloorDrainningPlaneSystemCmd
    {
        [CommandMethod("TIANHUACAD", "THPSLY", CommandFlags.Modal)] //天华排水路由
        public void CreateFirstFloorDrainningPlane()
        {
            ThFirstFloorDrainageCmd drainageCmd = new ThFirstFloorDrainageCmd(ThWSSUIService.Instance.PSPMParameter.config,
                ThWSSUIService.Instance.PSPMParameter.paraSettingViewModel, ThWSSUIService.Instance.PSPMParameter.firstFloorPlaneViewModel);
            drainageCmd.Execute();
        }
    }
}
