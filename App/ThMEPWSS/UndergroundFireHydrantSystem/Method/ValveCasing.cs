using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Method
{
    public class ValveCasing
    {
        public Point3dEx Position { get; set; }
        public int Type { get; set; }//0 套管； 1 蝶阀； 2 闸阀

        public ValveCasing(Point3dEx pt, int type)
        {
            Position = pt;
            Type = type;
        }
    }
}
