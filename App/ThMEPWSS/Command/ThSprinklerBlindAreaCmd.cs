using System;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using AcHelper.Commands;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.Service;
using ThMEPWSS.Bussiness;

namespace ThMEPWSS.Command
{
    public class ThSprinklerBlindAreaCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            var polylines = ThSprinklerLayoutCmdUtils.GetFrames();
            if (polylines.Count <= 0)
            {
                return;
            }
            if (!ThSprinklerLayoutCmdUtils.CalWCSLayoutDirection(out Matrix3d matrix))
            {
                return;
            }
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                CalHolesService calHolesService = new CalHolesService();
                var holeDic = calHolesService.CalHoles(polylines);
                foreach (var holeInfo in holeDic)
                {
                    var plFrame = holeInfo.Key;
                    var holes = holeInfo.Value;

                    var bufferPoly = plFrame.Buffer(1)[0] as Polyline;
                    //清除原有构件
                    plFrame.ClearBlindArea();

                    //获取构建信息
                    var calStructPoly = (plFrame.Clone() as Polyline).Buffer(10000)[0] as Polyline;
                    ThSprinklerLayoutCmdUtils.GetStructureInfo(acdb, calStructPoly, plFrame, out List<Polyline> columns, out List<Entity> beams, out List<Polyline> walls);
                    holes.AddRange(columns);
                    holes.AddRange(walls);

                    if (!CalSprayBlindArea(bufferPoly, holes, acdb, matrix))
                    {
                        CalSprayLineBlindArea(bufferPoly, holes, acdb);
                    }
                }
            }
        }

        /// <summary>
        /// 计算喷淋布置点盲区
        /// </summary>
        /// <param name="result"></param>
        /// <param name="acdb"></param>
        private bool CalSprayBlindArea(Polyline plFrame, List<Polyline> holes, AcadDatabase acdb, Matrix3d matrix)
        {
            var dxfNames = new string[]
            {
                ThCADCommon.DxfName_TCH_EQUIPMENT_16,
                ThCADCommon.DxfName_TCH_EQUIPMENT_12,
                RXClass.GetClass(typeof(BlockReference)).DxfName,
            };
            var filterlist = OpFilter.Bulid(o =>
            o.Dxf((int)DxfCode.LayerName) == ThWSSCommon.SprayLayerName &
            o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));

            var dBObjectCollection = new DBObjectCollection();
            var allSprays = Active.Editor.SelectAll(filterlist);
            if (allSprays.Status == PromptStatus.OK)
            {
                foreach (ObjectId obj in allSprays.Value.GetObjectIds())
                {
                    var transEnt = acdb.Element<Entity>(obj).Clone() as Entity;
                    transEnt.TransformBy(matrix.Inverse());
                    dBObjectCollection.Add(transEnt);
                }
            }

            //转换usc
            plFrame.TransformBy(matrix.Inverse());
            holes.ForEach(x => x.TransformBy(matrix.Inverse()));

            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(dBObjectCollection);
            var sprays = thCADCoreNTSSpatialIndex.SelectWindowPolygon(plFrame).Cast<Entity>().ToList();

            CheckService checkService = new CheckService();
            //只需要显示出来的喷淋
            if (checkService.CheckLayerStatus(acdb.Database, ThWSSCommon.SprayLayerName))
            {
                return false;
            }

            //筛选出正确的喷淋图块
            sprays = sprays.Where(x => checkService.CheckSprayBlockSize(x, 300)).ToList();
            if (sprays.Count <= 0)
            {
                Active.Editor.WriteMessage("\n 区域未检测到喷头");
                return false;
            }
            var sprayPts = sprays.Select(x =>
            {
                var pts = x.GeometricExtents;
                var newPt = new Point3d((pts.MinPoint.X + pts.MaxPoint.X) / 2, (pts.MinPoint.Y + pts.MaxPoint.Y) / 2, 0);
                return newPt.TransformBy(matrix);
            }).ToList();
            CalSprayBlindAreaService calSprayBlindAreaService = new CalSprayBlindAreaService(matrix);
            calSprayBlindAreaService.CalSprayBlindArea(sprayPts, plFrame, holes, ThWSSUIService.Instance.Parameter.protectRange);

            return true;
        }

        /// <summary>
        /// 计算喷淋布置线盲区
        /// </summary>
        /// <param name="result"></param>
        /// <param name="acdb"></param>
        private void CalSprayLineBlindArea(Polyline plFrame, List<Polyline> holes, AcadDatabase acdb)
        {
            var filterlist = OpFilter.Bulid(o =>
                   o.Dxf((int)DxfCode.LayerName) == ThWSSCommon.Layout_Line_LayerName &
                   o.Dxf((int)DxfCode.Start) == RXClass.GetClass(typeof(Line)).DxfName);

            var dBObjectCollection = new DBObjectCollection();
            var allLines = Active.Editor.SelectAll(filterlist);
            if (allLines.Status == PromptStatus.OK)
            {
                foreach (ObjectId obj in allLines.Value.GetObjectIds())
                {
                    dBObjectCollection.Add(acdb.Element<Line>(obj));
                }

                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(dBObjectCollection);
                var sprayLines = thCADCoreNTSSpatialIndex.SelectWindowPolygon(plFrame).Cast<Line>().ToList();

                CalSprayBlindLineAreaService calSprayBlindAreaService = new CalSprayBlindLineAreaService();
                calSprayBlindAreaService.CalSprayBlindArea(sprayLines, plFrame);
            }
            else
            {
                Active.Editor.WriteMessage("\n 区域未检测到喷头");
            }
        }
    }
}
