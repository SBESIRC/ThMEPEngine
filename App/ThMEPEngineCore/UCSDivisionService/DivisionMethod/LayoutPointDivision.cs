using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.UCSDivisionService.Utils;

namespace ThMEPEngineCore.UCSDivisionService.DivisionMethod
{
    public class LayoutPointDivision
    {
        public Dictionary<Polyline, Vector3d> Division(List<Polyline> columns, Polyline polyline)
        {
            //将柱转化为点
            var columnPts = columns.Select(x => StructUtils.GetColumnPoint(x)).ToCollection();

            //将柱点转为柱网
            var columnsPolygon = StructPolyService.StructPointToPolygon(columnPts);

            //根据柱网进行分区
            UcsByPointsDivider byPointsDivider = new UcsByPointsDivider();
            byPointsDivider.Compute(polyline, columns);
            return byPointsDivider.UCSs;
        }
    }
}
