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
            var columnDics = columns.ToDictionary(x => x, y => StructUtils.GetColumnPoint(y))
                .Where(x => polyline.Contains(x.Value))
                .ToDictionary(x => x, y => y.Value);

            //将柱点转为柱网
            var columnsPolygon = StructPolyService.StructPointToPolygon(columnDics.Values.ToCollection(), polyline);
            using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var item in columnsPolygon)
                {
                    //db.ModelSpace.Add(item);
                }
            }
            //根据柱网进行分区
            UcsByPointsDivider byPointsDivider = new UcsByPointsDivider();
            byPointsDivider.Compute(polyline, columns);
            return byPointsDivider.UCSs;
        }
    }
}
