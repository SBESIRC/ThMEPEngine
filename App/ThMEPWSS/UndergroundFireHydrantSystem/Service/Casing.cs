using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.Command
{
    class Casing
    {
        
        public static int HasCasing(List<Point3dEx> ptList, FireHydrantSystemIn fireHydrantSysIn)
        {
            int isCasing = 0;//0 没有套管； 1 套管在左边； 2 套管在右边
            /*
            for (int i = 0; i < ptList.Count - 1; i++)
            {
                var p1 = ptList[i];
                var p2 = ptList[i + 1];
                if (fireHydrantSysIn.ptTypeDic[p1].Contains("Valve") && fireHydrantSysIn.ptTypeDic[p2].Contains("Valve"))
                {
                    if (fireHydrantSysIn.ptTypeDic[p1].Contains("casing"))
                    {
                        isCasing = 1;
                        break;
                    }
                    else if (fireHydrantSysIn.ptTypeDic[p2].Contains("casing"))
                    {
                        isCasing = 2;
                        break;
                    }
                    
                }
            }
            */
            if (fireHydrantSysIn.ptTypeDic[ptList[0]].Contains("casing"))
            {
                isCasing = 1;
            }
            else
            {
                isCasing = 2;
            }
            return isCasing;
        }
    }
}
