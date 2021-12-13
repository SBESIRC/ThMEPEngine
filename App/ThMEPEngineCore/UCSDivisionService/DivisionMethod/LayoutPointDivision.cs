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
using ThCADExtension;
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
                .ToDictionary(x => x.Key, y => y.Value);
            var checkColumn = new Dictionary<Polyline, Point3d>();
            foreach (var pair in columnDics)
            {
                if (checkColumn.Where(x => x.Value.DistanceTo(pair.Value) < 1000).Count() <= 0)
                {
                    checkColumn.Add(pair.Key, pair.Value);
                }
            }

            //将polyline中的弧打成多段线
            var tesslatePoly = polyline.TessellatePolylineWithChord(3000);

            //将柱点转为柱网
            var columnsPolygon = StructPolyService.StructPointToPolygon(checkColumn.Values.ToCollection(), tesslatePoly, out List<Polyline> triangles);

            //进行第二步区域分割
            var region = StructPolyService.CutRegion(tesslatePoly, checkColumn, columnsPolygon, triangles);
            using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var item in region)
                {
                    item.ColorIndex = 1;
                    //db.ModelSpace.Add(item);
                }
            }

            //根据柱网进行分区
            UcsByPointsDivider byPointsDivider = new UcsByPointsDivider();
            byPointsDivider.Compute(tesslatePoly, region);
            var ucsPolys = byPointsDivider.UCSs;

            var resUcsPolys = StructPolyService.AdjustUCSPolys(ucsPolys, tesslatePoly);
            return resUcsPolys;
        }
    }
}
