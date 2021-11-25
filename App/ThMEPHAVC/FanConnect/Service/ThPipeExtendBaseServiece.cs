using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.FanConnect.Model;

namespace ThMEPHVAC.FanConnect.Service
{
    public abstract class ThPipeExtendBaseServiece
    {
        public abstract void PipeExtend(ThFanTreeModel tree);
    }
}
