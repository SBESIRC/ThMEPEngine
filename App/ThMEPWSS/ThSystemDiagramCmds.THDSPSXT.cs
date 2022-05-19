using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Command;
using ThMEPWSS.Common;
using ThMEPWSS.Model;

namespace ThMEPWSS
{
    public partial class ThSystemDiagramCmds
    {
        [CommandMethod("TIANHUACAD", "THDSYSPSXTTest", CommandFlags.Modal)]
        public void ThDrainSysAboveGround()
        {
            var res = FramedReadUtil.SelectFloorFramed(out List<FloorFramed> selectList);
            if (res && null != selectList && selectList.Count > 0)
            {
                ThDrainSystemAboveGroundCmd thDrainSystem = new ThDrainSystemAboveGroundCmd(selectList, null, null);
                thDrainSystem.Execute();
            }
        }
    }
}
