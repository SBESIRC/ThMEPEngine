using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThWSS;

namespace ThMEPWSS.Service
{
    public static class ClearComponentService
    {
        /// <summary>
        /// 删除喷淋图块
        /// </summary>
        /// <param name="polyline"></param>
        public static void ClearSpray(this Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var objs = new DBObjectCollection();
                var sprays = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => o.Layer == ThWSSCommon.SprayLayerName);
                sprays.Where(o => polyline.Contains(o.Position))
                         .ForEachDbObject(o => objs.Add(o));
                foreach (BlockReference spray in objs)
                {
                    spray.UpgradeOpen();
                    spray.Erase();
                }
            }
        }

        /// <summary>
        /// 删除喷淋布置线
        /// </summary>
        /// <param name="polyline"></param>
        public static void ClearSprayLines(this Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var objs = new DBObjectCollection();
                var sprays = acadDatabase.ModelSpace
                    .OfType<Line>()
                    .Where(o => o.Layer == ThWSSCommon.Layout_Line_LayerName);
                sprays.ForEach(x => objs.Add(x));

                var bufferPoly = polyline.Buffer(1)[0] as Polyline;
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var sprayLines = thCADCoreNTSSpatialIndex.SelectWindowPolygon(bufferPoly).Cast<Line>().ToList();

                foreach (var sLine in sprayLines)
                {
                    sLine.UpgradeOpen();
                    sLine.Erase();
                }
            }
        }

        /// <summary>
        /// 删除盲区信息
        /// </summary>
        /// <param name="polyline"></param>
        public static void ClearBlindArea(this Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var objs = new DBObjectCollection();
                var blindLines = acadDatabase.ModelSpace
                    .OfType<Polyline>()
                    .Where(o => o.Layer == ThWSSCommon.BlindArea_LayerName);
                blindLines.ForEach(x => objs.Add(x));

                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var bLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();
                foreach (var line in bLines)
                {
                    line.UpgradeOpen();
                    line.Erase();
                }
                objs.Clear();

                var blindSolid = acadDatabase.ModelSpace
                    .OfType<Hatch>()
                    .Where(o => o.Layer == ThWSSCommon.BlindArea_LayerName);
                blindSolid.ForEachDbObject(o => objs.Add(o));

                thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var hatchs = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Hatch>().ToList();
                foreach (Hatch bSolid in hatchs)
                {
                    bSolid.UpgradeOpen();
                    bSolid.Erase();
                }
            }
        }
    }
}
