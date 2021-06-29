using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.HydrantConnectPipe.Model;

namespace ThMEPWSS.HydrantConnectPipe.Command
{
    public static class ThHydrantConnectPipeUtils
    {
        public static double GetDistFireHydrantToPipe(ThHydrant fireHydrant, ThHydrantPipe pipe)
        {
            return ThCADCoreNTSDistance.Distance(fireHydrant.FireHydrantObb,pipe.PipePosition);
        }
        public static bool HydrantIsContainPipe(ThHydrant fireHydrant, List<ThHydrantPipe> pipes)
        {
            double minDist = 9999.0;
            foreach(var pipe in pipes)
            {
                if (fireHydrant.IsContainsPipe(pipe, 500.0))
                {
                    double tmpDist = ThHydrantConnectPipeUtils.GetDistFireHydrantToPipe(fireHydrant, pipe);
                    if(minDist > tmpDist)
                    {
                        fireHydrant.FireHydrantPipe = pipe;
                        minDist = tmpDist;
                    }
                }
            }
            pipes.Remove(fireHydrant.FireHydrantPipe);
            return false;
        }
    }
}
