using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.Command
{
    class Casing
    {
        public static int HasCasing(List<Point3dEx> ptList, FireHydrantSystemIn fireHydrantSysIn)
        {
            int isCasing = 0;//0 没有套管； 1 套管在左边； 2 套管在右边
            if (fireHydrantSysIn.PtTypeDic[ptList[0]].Contains("casing"))
            {
                isCasing = 1;
            }
            if (fireHydrantSysIn.PtTypeDic[ptList[1]].Contains("casing"))
            {
                isCasing = 2;
            }
            return isCasing;
        }
    }
}
