using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.GridOperation.Model;

namespace ThMEPEngineCore.UCSDivisionService.Utils
{
    public static class CheckService
    {
        /// <summary>
        /// 判断轴网类型
        /// </summary>
        /// <param name="grids"></param>
        /// <returns></returns>
        public static GridType GetGridType(List<Curve> grids)
        {
            if (grids.Any(x=>x is Arc))
            {
                return GridType.ArcGrid;
            }
            else
            {
                return GridType.LineGrid;
            }
        }
    }
}
