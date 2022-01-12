using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;

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

                //获取喷淋
                var dxfNames = new string[]
                {
                    ThCADCommon.DxfName_TCH_EQUIPMENT_16,
                    ThCADCommon.DxfName_TCH_EQUIPMENT_12,
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filterlist = OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.LayerName) == ThWSSCommon.SprayLayerName &
                o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
                var sprays = new List<Entity>();
                var allSprays = Active.Editor.SelectAll(filterlist);
                if (allSprays.Status == PromptStatus.OK)
                {
                    using (AcadDatabase acdb = AcadDatabase.Active())
                    {
                        foreach (ObjectId obj in allSprays.Value.GetObjectIds())
                        {
                            sprays.Add(acdb.Element<Entity>(obj));
                        }
                    }
                }
                var objs = new DBObjectCollection();
                sprays.Where(o => {
                    var pts = o.GeometricExtents;
                    var position = new Point3d((pts.MinPoint.X + pts.MaxPoint.X) / 2, (pts.MinPoint.Y + pts.MaxPoint.Y) / 2, 0);
                    return polyline.Contains(position);
                })
                .ForEachDbObject(o => objs.Add(o));
                foreach (Entity spray in objs)
                {
                    spray.UpgradeOpen();
                    spray.Erase();
                }
            }
        }

        /// <summary>
        /// 删除走道线喷淋图块
        /// </summary>
        /// <param name="polyline"></param>
        public static void ClearSprayByLine(this List<Line> lines)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(ThWSSCommon.SprayLayerName);
                acadDatabase.Database.UnLockLayer(ThWSSCommon.SprayLayerName);
                acadDatabase.Database.UnOffLayer(ThWSSCommon.SprayLayerName);
                var objs = new DBObjectCollection();
                var sprays = acadDatabase.ModelSpace
                    .OfType<Entity>()
                    .Where(o => o.Layer == ThWSSCommon.SprayLayerName);
                sprays.ForEach(x => objs.Add(x));

                var crossingSprays = lines.Select(x => x.ToPolyline()).SelectMany(x =>
                {
                    ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                    return thCADCoreNTSSpatialIndex.SelectFence(x).Cast<Entity>().ToList();
                }).Distinct();
                foreach (Entity spray in crossingSprays)
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
                acadDatabase.Database.UnFrozenLayer(ThWSSCommon.Blind_Zone_LayerName);
                acadDatabase.Database.UnLockLayer(ThWSSCommon.Blind_Zone_LayerName);
                acadDatabase.Database.UnOffLayer(ThWSSCommon.Blind_Zone_LayerName);
                var bufferPoly = polyline.Buffer(-1)[0] as Polyline;
                var objs = new DBObjectCollection();
                var blindLines = acadDatabase.ModelSpace
                    .OfType<Polyline>()
                    .Where(o => o.Layer == ThWSSCommon.Blind_Zone_LayerName);
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
                    .Where(o => o.Layer == ThWSSCommon.Blind_Zone_LayerName);
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

                using(var ov = new ThCADCoreNTSArcTessellationLength(100.0))
                {
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
