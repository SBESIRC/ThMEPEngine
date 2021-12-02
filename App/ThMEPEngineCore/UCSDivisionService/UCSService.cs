using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.UCSDivisionService.DivisionMethod;

namespace ThMEPEngineCore.UCSDivisionService
{
    public class UCSService
    {
        /// <summary>
        /// 聚点(根据柱网进行UCS区域分割)
        /// </summary>
        public Dictionary<Polyline, Vector3d> UcsDivision(List<Polyline> columns, Polyline frame)
        {
            LayoutPointDivision layoutPointDivision = new LayoutPointDivision();
            return layoutPointDivision.Division(columns, frame);
        }

        public void UcsDivision(List<Curve> girds, Polyline frame)
        {

        }
    }
}
