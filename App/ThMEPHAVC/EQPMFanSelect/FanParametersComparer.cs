using System.Collections.Generic;
using ThMEPHVAC.EQPMFanModelEnums;

namespace ThMEPHVAC.EQPMFanSelect
{
    public class CCCFComparer : IEqualityComparer<FanParameters>
    {
        public bool Equals(FanParameters x, FanParameters y)
        {
            return x.CCCF_Spec == y.CCCF_Spec;
        }

        public int GetHashCode(FanParameters obj)
        {
            return obj.CCCF_Spec.GetHashCode();
        }
    }

    public class CCCFRpmComparer : IEqualityComparer<FanParameters>
    {
        public bool Equals(FanParameters x, FanParameters y)
        {
            return (x.CCCF_Spec == y.CCCF_Spec) && (x.Rpm == y.Rpm);
        }

        public int GetHashCode(FanParameters obj)
        {
            return obj.CCCF_Spec.GetHashCode() ^ obj.Rpm.GetHashCode();
        }
    }

    public class AxialModelNumberComparer : IEqualityComparer<AxialFanParameters>
    {
        public bool Equals(AxialFanParameters x, AxialFanParameters y)
        {
            return x.ModelNum == y.ModelNum;
        }

        public int GetHashCode(AxialFanParameters obj)
        {
            return obj.ModelNum.GetHashCode();
        }
    }
}
