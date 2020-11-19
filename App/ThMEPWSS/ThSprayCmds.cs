using System;
using AcHelper;
using NFox.Cad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPWSS.Bussiness;
using ThMEPEngineCore.Progress;
using ThMEPWSS.Bussiness.LayoutBussiness;
using ThMEPWSS.Service;
using ThWSS;
using ThWSS.Bussiness;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Extension;
using Catel.IoC;

namespace ThMEPWSS
{
    public class ThSprayCmds
    {
        private IProgressHandler ProgressHandler
        {
            get
            {
                return ServiceLocator.Default.ResolveType<IProgressHandler>();
            }
        }

        [CommandMethod("TIANHUACAD", "THPLPT", CommandFlags.Modal)]
        public void ThAutomaticLayoutSpray()
        {
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择区域",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                RXClass.GetClass(typeof(Polyline)).DxfName,
            };
            var filter = ThSelectionFilterTool.Build(dxfNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return;
            }

            if (!CalWCSLayoutDirection(out Matrix3d matrix))
            {
                return;
            }

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                List<Polyline> polylines = new List<Polyline>();
                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var plBack = acdb.Element<Polyline>(frame);
                    var plFrame = ThMEPFrameService.Normalize(plBack);
                    polylines.Add(plFrame);
                }

                CalHolesService calHolesService = new CalHolesService();
                var holeDic = calHolesService.CalHoles(polylines);
                var meter = new ThMEPProgressMeter(ProgressHandler);
                foreach (var holeInfo in holeDic)
                {
                    var plFrame = holeInfo.Key;
                    var holes = holeInfo.Value;

                    //清除原有构件
                    plFrame.ClearSprayLines();
                    plFrame.ClearSpray();
                    plFrame.ClearBlindArea();
                    plFrame.ClearErrorSprayMark();
                    plFrame.ClearMoveSprayMark();
                    plFrame.ClearLayouArea();

                    //获取构建信息
                    var calStructPoly = (plFrame.Clone() as Polyline).Buffer(10000)[0] as Polyline;
                    GetStructureInfo(acdb, calStructPoly, plFrame, out List<Polyline> columns, out List<Polyline> beams, out List<Polyline> walls);

                    //转换usc
                    plFrame.TransformBy(matrix.Inverse());
                    holes.ForEach(x => x.TransformBy(matrix.Inverse()));
                    columns.ForEach(x => x.TransformBy(matrix.Inverse()));
                    beams.ForEach(x => x.TransformBy(matrix.Inverse()));
                    walls.ForEach(x => x.TransformBy(matrix.Inverse()));

                    //生成喷头
                    RayLayoutService layoutDemo = new RayLayoutService();
                    var sprayPts = layoutDemo.LayoutSpray(plFrame, columns, beams, walls, holes, matrix, false);

                    //放置喷头
                    InsertSprayService.InsertSprayBlock(sprayPts.Select(o => o.Position).ToList(), SprayType.SPRAYDOWN);

                    //打印喷头变化轨迹
                    MarkService.PrintOriginSpray(sprayPts);

                    plFrame.TransformBy(matrix);
                    holes.ForEach(x => x.TransformBy(matrix));
                    //打印喷淋点盲区
                    CalSprayBlindAreaService calSprayBlindAreaService = new CalSprayBlindAreaService(matrix);
                    calSprayBlindAreaService.CalSprayBlindArea(sprayPts, plFrame, holes);
                    meter.CurrentPercent = 100;
                }
                meter.Reset();
            }
        }

        [CommandMethod("TIANHUACAD", "THPLMQ", CommandFlags.Modal)]
        public void ThCreateBlindArea()
        {
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择区域",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                RXClass.GetClass(typeof(Polyline)).DxfName,
            };
            var filter = ThSelectionFilterTool.Build(dxfNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return;
            }

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                //获取方向线
                if (!CalWCSLayoutDirection(out Matrix3d matrix))
                {
                    return;
                }

                List<Polyline> polylines = new List<Polyline>();
                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var plBack = acdb.Element<Polyline>(frame);
                    var plFrame = ThMEPFrameService.Normalize(plBack);
                    polylines.Add(plFrame);
                }

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
                    GetStructureInfo(acdb, calStructPoly, plFrame, out List<Polyline> columns, out List<Polyline> beams, out List<Polyline> walls);
                    holes.AddRange(columns);
                    holes.AddRange(walls);

                    if (!CalSprayBlindArea(bufferPoly, holes, acdb, matrix))
                    {
                        CalSprayLineBlindArea(bufferPoly, holes, acdb);
                    }
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THPLKQ", CommandFlags.Modal)]
        public void ThCreateLayoutArea()
        {
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择区域",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                RXClass.GetClass(typeof(Polyline)).DxfName,
            };
            var filter = ThSelectionFilterTool.Build(dxfNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return;
            }

            if (!CalWCSLayoutDirection(out Matrix3d matrix))
            {
                return;
            }

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                List<Polyline> polylines = new List<Polyline>();
                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var plBack = acdb.Element<Polyline>(frame);
                    var plFrame = ThMEPFrameService.Normalize(plBack);
                    polylines.Add(plFrame);
                }

                CalHolesService calHolesService = new CalHolesService();
                var holeDic = calHolesService.CalHoles(polylines);
                foreach (var holeInfo in holeDic)
                {
                    var plFrame = holeInfo.Key;
                    var holes = holeInfo.Value;

                    //清除原有构件
                    plFrame.ClearLayouArea();

                    //获取构建信息
                    var calStructPoly = (plFrame.Clone() as Polyline).Buffer(10000)[0] as Polyline;
                    GetStructureInfo(acdb, calStructPoly, plFrame, out List<Polyline> columns, out List<Polyline> beams, out List<Polyline> walls);

                    //转换usc
                    plFrame.TransformBy(matrix.Inverse());
                    holes.ForEach(x => x.TransformBy(matrix.Inverse()));
                    columns.ForEach(x => x.TransformBy(matrix.Inverse()));
                    beams.ForEach(x => x.TransformBy(matrix.Inverse()));
                    walls.ForEach(x => x.TransformBy(matrix.Inverse()));
                    holes.AddRange(walls);

                    //计算可布置区域
                    var layoutAreas = CreateLayoutAreaService.GetLayoutArea(plFrame, beams, columns, holes, 300);

                    //打印可布置区域
                    MarkService.PrintLayoutArea(layoutAreas, matrix);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THPLCD", CommandFlags.Modal)]
        public void ThCreateLayoutPtByLine()
        {
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择布置线",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                RXClass.GetClass(typeof(Line)).DxfName,
                RXClass.GetClass(typeof(Polyline)).DxfName
            };
            var filter = ThSelectionFilterTool.Build(dxfNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return;
            }

            List<Curve> lines = new List<Curve>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var plBack = acdb.Element<Curve>(frame);
                    lines.Add(plBack);
                }
            }

            //预处理线
            DBObjectCollection objs = new DBObjectCollection();
            lines.ForEach(x => objs.Add(x));
            var handleLines = ThMEPLineExtension.LineSimplifier(objs, 20.0, 2.0, Math.PI / 180.0);
            objs = new DBObjectCollection();
            handleLines.ForEach(x => objs.Add(x));
            handleLines = objs.ToNTSNodedLineStrings().ToDbObjects()
                .SelectMany(x => {
                    DBObjectCollection entitySet = new DBObjectCollection();
                    (x as Polyline).Explode(entitySet);
                    return entitySet.Cast<Line>().ToList();
                })
                .ToList();

            LayoutSprayByLineService layoutSprayByLineService = new LayoutSprayByLineService();
            var layoutPts = layoutSprayByLineService.LayoutSprayByLine(handleLines, 3000);

            //放置喷头
            InsertSprayService.InsertSprayBlock(layoutPts, SprayType.SPRAYDOWN);
        }

        [CommandMethod("TIANHUACAD", "THPLZX", CommandFlags.Modal)]
        public void ThPTLayout()
        {
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择区域",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                RXClass.GetClass(typeof(Polyline)).DxfName,
            };
            var filter = ThSelectionFilterTool.Build(dxfNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return;
            }

            if (!CalWCSLayoutDirection(out Matrix3d matrix))
            {
                return;
            }

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                List<Polyline> polylines = new List<Polyline>();
                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var plBack = acdb.Element<Polyline>(frame);
                    var plFrame = ThMEPFrameService.Normalize(plBack);
                    polylines.Add(plFrame);
                }

                CalHolesService calHolesService = new CalHolesService();
                var holeDic = calHolesService.CalHoles(polylines);
                foreach (var holeInfo in holeDic)
                {
                    var plFrame = holeInfo.Key;
                    var holes = holeInfo.Value;

                    //清除原有构件
                    plFrame.ClearSprayLines();
                    plFrame.ClearSpray();
                    plFrame.ClearBlindArea();
                    plFrame.ClearErrorSprayMark();
                    plFrame.ClearMoveSprayMark();
                    plFrame.ClearLayouArea();

                    //获取构建信息
                    var calStructPoly = (plFrame.Clone() as Polyline).Buffer(10000)[0] as Polyline;
                    GetStructureInfo(acdb, calStructPoly, plFrame, out List<Polyline> columns, out List<Polyline> beams, out List<Polyline> walls);

                    //转换usc
                    plFrame.TransformBy(matrix.Inverse());
                    columns.ForEach(x => x.TransformBy(matrix.Inverse()));
                    beams.ForEach(x => x.TransformBy(matrix.Inverse()));
                    walls.ForEach(x => x.TransformBy(matrix.Inverse()));

                    //生成喷淋对象
                    RayLayoutService layoutDemo = new RayLayoutService();
                    layoutDemo.LayoutSpray(plFrame, columns, beams, walls, holes, matrix, true);
                }
            }
        }

        /// <summary>
        /// 计算排布方向
        /// </summary>
        /// <returns></returns>
        private bool CalWCSLayoutDirection(out Matrix3d matrix)
        {
            matrix = Active.Editor.CurrentUserCoordinateSystem;
            PromptPointOptions options = new PromptPointOptions("请选择排布方向起始点");
            var sResult = Active.Editor.GetPoint(options);

            if (sResult.Status == PromptStatus.OK)
            {
                var startPt = sResult.Value;
                var transPt = startPt.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                var endPt = Interaction.GetLineEndPoint("请选择终止点", transPt);

                if (System.Double.IsNaN(endPt.X) || System.Double.IsNaN(endPt.Y) || System.Double.IsNaN(endPt.Z))
                {
                    return false;
                }

                transPt = new Point3d(transPt.X, transPt.Y, 0);
                endPt = new Point3d(endPt.X, endPt.Y, 0);
                Vector3d xDir = (endPt - transPt).GetNormal();
                Vector3d yDir = xDir.GetPerpendicularVector().GetNormal();
                Vector3d zDir = Vector3d.ZAxis;

                matrix = new Matrix3d(new double[]{
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0});

                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取构建信息
        /// </summary>
        /// <param name="acdb"></param>
        /// <param name="polyline"></param>
        /// <param name="columns"></param>
        /// <param name="beams"></param>
        /// <param name="walls"></param>
        private void GetStructureInfo(AcadDatabase acdb, Polyline polyline, Polyline pFrame, out List<Polyline> columns, out List<Polyline> beams, out List<Polyline> walls)
        {
            var allStructure = ThBeamConnectRecogitionEngine.ExecutePreprocess(acdb.Database, polyline.Vertices());

            //获取柱
            columns = allStructure.ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            var objs = new DBObjectCollection();
            columns.ForEach(x => objs.Add(x));
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(pFrame).Cast<Polyline>().ToList();

            //获取梁
            var thBeams = allStructure.BeamEngine.Elements.Cast<ThIfcLineBeam>().ToList();
            thBeams.ForEach(x => x.ExtendBoth(20, 20));
            beams = thBeams.Select(o => o.Outline).Cast<Polyline>().ToList();
            objs = new DBObjectCollection();
            beams.ForEach(x => objs.Add(x));
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            beams = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(pFrame).Cast<Polyline>().ToList();

            //获取剪力墙
            walls = allStructure.ShearWallEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            objs = new DBObjectCollection();
            walls.ForEach(x => objs.Add(x));
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(pFrame).Cast<Polyline>().ToList();
        }

        /// <summary>
        /// 计算喷淋布置点盲区
        /// </summary>
        /// <param name="result"></param>
        /// <param name="acdb"></param>
        private bool CalSprayBlindArea(Polyline plFrame, List<Polyline> holes, AcadDatabase acdb, Matrix3d matrix)
        {
            var filterlist = OpFilter.Bulid(o =>
            o.Dxf((int)DxfCode.LayerName) == ThWSSCommon.SprayLayerName &
            o.Dxf((int)DxfCode.Start) == RXClass.GetClass(typeof(BlockReference)).DxfName);

            var dBObjectCollection = new DBObjectCollection();
            var allSprays = Active.Editor.SelectAll(filterlist);
            if (allSprays.Status == PromptStatus.OK)
            {
                foreach (ObjectId obj in allSprays.Value.GetObjectIds())
                {
                    dBObjectCollection.Add(acdb.Element<BlockReference>(obj));
                }
            }
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(dBObjectCollection);
            var sprays = thCADCoreNTSSpatialIndex.SelectWindowPolygon(plFrame).Cast<BlockReference>().ToList();

            CheckService checkService = new CheckService();
            //只需要显示出来的喷淋
            if(checkService.CheckLayerStatus(acdb.Database, ThWSSCommon.SprayLayerName))
            {
                return false;
            }

            //筛选出正确的喷淋图块
            sprays = sprays.Where(x => checkService.CheckSprayBlockSize(x, 300)).ToList();
            if (sprays.Count <= 0)
            {
                Active.Editor.WriteMessage("\n 喷淋暂未生成");
                return false;
            }
            var sprayPts = sprays.Select(x => x.Position).ToList();
            CalSprayBlindAreaService calSprayBlindAreaService = new CalSprayBlindAreaService(matrix);
            calSprayBlindAreaService.CalSprayBlindArea(sprayPts, plFrame, holes);

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
                Active.Editor.WriteMessage("\n 喷淋布置线暂未生成");
            }
        }
    }
}
