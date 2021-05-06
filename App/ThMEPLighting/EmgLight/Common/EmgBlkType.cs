using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.EmgLight.Common
{
    public class EmgBlkType
    {
        public enum BlockType
        {
            emgLight = 0,
            evac = 1,
            exit = 2,
            evacCeiling = 3,
            enter = 4,
            ale = 5,
        }
    }
}
