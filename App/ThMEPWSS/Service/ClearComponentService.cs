using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
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
                acadDatabase.Database.UnFrozenLayer(ThWSSCommon.SprayLayerName);
                acadDatabase.Database.UnLockLayer(ThWSSCommon.SprayLayerName);
                acadDatabase.Database.UnOffLayer(ThWSSCommon.SprayLayerName);
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
                acadDatabase.Database.UnFrozenLayer(ThWSSCommon.Layout_Line_LayerName);
                acadDatabase.Database.UnLockLayer(ThWSSCommon.Layout_Line_LayerName);
                acadDatabase.Database.UnOffLayer(ThWSSCommon.Layout_Line_LayerName);
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
                acadDatabase.Database.UnFrozenLayer(ThWSSCommon.BlindArea_LayerName);
                acadDatabase.Database.UnLockLayer(ThWSSCommon.BlindArea_LayerName);
                acadDatabase.Database.UnOffLayer(ThWSSCommon.BlindArea_LayerName);
                var bufferPoly = polyline.Buffer(-1)[0] as Polyline;
                var objs = new DBObjectCollection();
                var blindLines = acadDatabase.ModelSpace
                    .OfType<Polyline>()
                    .Where(o => o.Layer == ThWSSCommon.BlindArea_LayerName);
                blindLines.ForEach(x => objs.Add(x));

                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var bLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(bufferPoly).Cast<Polyline>().ToList();
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
                var hatchs = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(bufferPoly).Cast<Hatch>().ToList();
                foreach (Hatch bSolid in hatchs)
                {
                    bSolid.UpgradeOpen();
                    bSolid.Erase();
                }
            }
        }

        /// <summary>
        /// 删除有问题的喷头标记
        /// </summary>
        /// <param name="polyline"></param>
        public static void ClearErrorSprayMark(this Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(ThWSSCommon.Layout_Error_Spray_LayerName);
                acadDatabase.Database.UnLockLayer(ThWSSCommon.Layout_Error_Spray_LayerName);
                acadDatabase.Database.UnOffLayer(ThWSSCommon.Layout_Error_Spray_LayerName);
                var bufferPoly = polyline.Buffer(-1)[0] as Polyline;
                var objs = new DBObjectCollection();
                var errorCircles = acadDatabase.ModelSpace
                    .OfType<Circle>()
                    .Where(o => o.Layer == ThWSSCommon.Layout_Error_Spray_LayerName);
                errorCircles.ForEach(x => objs.Add(x));

                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var eCircle = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(bufferPoly).Cast<Circle>().ToList();
                foreach (var circle in eCircle)
                {
                    circle.UpgradeOpen();
                    circle.Erase();
                }
                objs.Clear();

                var blindSolid = acadDatabase.ModelSpace
                    .OfType<Hatch>()
                    .Where(o => o.Layer == ThWSSCommon.Layout_Error_Spray_LayerName);
                blindSolid.ForEachDbObject(o => objs.Add(o));

                thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var hatchs = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(bufferPoly).Cast<Hatch>().ToList();
                foreach (Hatch bSolid in hatchs)
                {
                    bSolid.UpgradeOpen();
                    bSolid.Erase();
                }
            }
        }

        /// <summary>
        /// 清除移动后喷淋位置对比标记
        /// </summary>
        /// <param name="polyline"></param>
        public static void ClearMoveSprayMark(this Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(ThWSSCommon.Layout_Origin_Spray_LayerName);
                acadDatabase.Database.UnLockLayer(ThWSSCommon.Layout_Origin_Spray_LayerName);
                acadDatabase.Database.UnOffLayer(ThWSSCommon.Layout_Origin_Spray_LayerName);
                var bufferPoly = polyline.Buffer(-1)[0] as Polyline;
                var objs = new DBObjectCollection();
                var errorCircles = acadDatabase.ModelSpace
                    .OfType<Circle>()
                    .Where(o => o.Layer == ThWSSCommon.Layout_Origin_Spray_LayerName);
                errorCircles.ForEach(x => objs.Add(x));

                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var eCircle = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(bufferPoly).Cast<Circle>().ToList();
                foreach (var circle in eCircle)
                {
                    circle.UpgradeOpen();
                    circle.Erase();
                }
                objs.Clear();

                var blindSolid = acadDatabase.ModelSpace
                    .OfType<Hatch>()
                    .Where(o => o.Layer == ThWSSCommon.Layout_Origin_Spray_LayerName);
                blindSolid.ForEachDbObject(o => objs.Add(o));

                thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var hatchs = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(bufferPoly).Cast<Hatch>().ToList();
                foreach (Hatch bSolid in hatchs)
                {
                    bSolid.UpgradeOpen();
                    bSolid.Erase();
                }
                objs.Clear();

                var connectPoly = acadDatabase.ModelSpace
                   .OfType<Polyline>()
                   .Where(o => o.Layer == ThWSSCommon.Layout_Origin_Spray_LayerName);
                connectPoly.ForEach(x => objs.Add(x));

                thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var cLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(bufferPoly).Cast<Polyline>().ToList();
                foreach (var line in cLines)
                {
                    line.UpgradeOpen();
                    line.Erase();
                }
            }
        }

        /// <summary>
        /// 清除可布置区域
        /// </summary>
        /// <param name="polyline"></param>
        public static void ClearLayouArea(this Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(ThWSSCommon.Layout_Area_LayerName);
                acadDatabase.Database.UnLockLayer(ThWSSCommon.Layout_Area_LayerName);
                acadDatabase.Database.UnOffLayer(ThWSSCommon.Layout_Area_LayerName);
                var objs = new DBObjectCollection();
                var layoutAreas = acadDatabase.ModelSpace
                    .OfType<Polyline>()
                    .Where(o => o.Layer == ThWSSCommon.Layout_Area_LayerName);
                layoutAreas.ForEach(x => objs.Add(x));

                var bufferPoly = polyline.Buffer(1)[0] as Polyline;
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var areaPolys = thCADCoreNTSSpatialIndex.SelectWindowPolygon(bufferPoly).Cast<Polyline>().ToList();

                foreach (var area in areaPolys)
                {
                    area.UpgradeOpen();
                    area.Erase();
                }
            }
        }
    }
}
