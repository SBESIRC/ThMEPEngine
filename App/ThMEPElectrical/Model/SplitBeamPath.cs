using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Model
{
    public class SplitBeamPath
    {
        public List<SecondBeamProfileInfo> pathNodes = new List<SecondBeamProfileInfo>();
        public HashSet<int> pathNums = new HashSet<int>();

        public SplitBeamPath()
        {
        }

        public SplitBeamPath(SplitBeamPath splitBeamPath)
        {
            pathNodes.AddRange(splitBeamPath.pathNodes);
            
            foreach (var orderNum in splitBeamPath.pathNums)
            {
                pathNums.Add(orderNum);
            }
        }

        public void Add(SecondBeamProfileInfo secondBeam)
        {
            if (pathNums.Contains(secondBeam.OrderNum))
                return;

            pathNums.Add(secondBeam.OrderNum);
            pathNodes.Add(secondBeam);
        }
    }
}
