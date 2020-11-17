using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPWSS.Bussiness;
using ThMEPWSS.Bussiness.LayoutBussiness;
using ThMEPWSS.Service;
using ThWSS;
using ThMEPWSS.Pipe;
using ThWSS.Bussiness;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Extension;
using System;
using ThMEPWSS.Pipe.Engine;
using DotNetARX;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Service;


namespace ThMEPWSS
{
    public class ThMEPWSSApp : IExtensionApplication
    {
        public void Initialize()
        {
            //throw new System.NotImplementedException();
        }

        public void Terminate()
        {
            //throw new System.NotImplementedException();
        }

        #region 喷淋布置
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
                }
            }
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

        #region 暂时不要
        //[CommandMethod("TIANHUACAD", "THPLPT", CommandFlags.Modal)]
        //public void ThGenerateSpary()
        //{
        //    PromptSelectionOptions options = new PromptSelectionOptions()
        //    {
        //        AllowDuplicates = false,
        //        MessageForAdding = "选择区域",
        //        RejectObjectsOnLockedLayers = true,
        //    };
        //    var dxfNames = new string[]
        //    {
        //        RXClass.GetClass(typeof(Polyline)).DxfName,
        //    };
        //    var filter = ThSelectionFilterTool.Build(dxfNames);
        //    var result = Active.Editor.GetSelection(options, filter);
        //    if (result.Status != PromptStatus.OK)
        //    {
        //        return;
        //    }

        //    using (AcadDatabase acdb = AcadDatabase.Active())
        //    {
        //        foreach (ObjectId frame in result.Value.GetObjectIds())
        //        {
        //            var plBack = acdb.Element<Polyline>(frame);
        //            var plFrame = ThMEPFrameService.Normalize(plBack);
        //            plFrame = plFrame.Buffer(5)[0] as Polyline;

        //            //清除原有构件
        //            plFrame.ClearSpray();
        //            plFrame.ClearBlindArea();

        //            var filterlist = OpFilter.Bulid(o =>
        //            o.Dxf((int)DxfCode.LayerName) == ThWSSCommon.Layout_Line_LayerName &
        //            o.Dxf((int)DxfCode.Start) == RXClass.GetClass(typeof(Line)).DxfName);

        //            var dBObjectCollection = new DBObjectCollection();
        //            var allLines = Active.Editor.SelectAll(filterlist);
        //            if (allLines.Status == PromptStatus.OK)
        //            {
        //                foreach (ObjectId obj in allLines.Value.GetObjectIds())
        //                {
        //                    dBObjectCollection.Add(acdb.Element<Line>(obj));
        //                }
        //            }

        //            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(dBObjectCollection);
        //            var sprayLines = thCADCoreNTSSpatialIndex.SelectWindowPolygon(plFrame).Cast<Line>().ToList();

        //            GenerateSpraysPointService generateSpraysService = new GenerateSpraysPointService();
        //            var sprayData = generateSpraysService.GenerateSprays(sprayLines);

        //            //放置喷头
        //            InsertSprayService.InsertSprayBlock(sprayData.Select(o => o.Position).ToList(), SprayType.SPRAYDOWN);

        //            plFrame.ClearSprayLines();
        //        }
        //    }
        //}
        #endregion

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
            };
            var filter = ThSelectionFilterTool.Build(dxfNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return; 
            }

            List<Line> lines = new List<Line>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var plBack = acdb.Element<Line>(frame);
                    lines.Add(plBack);
                }
            }
            LayoutSprayByLineService layoutSprayByLineService = new LayoutSprayByLineService();
            layoutSprayByLineService.LayoutSprayByLine(lines, 3000);
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
            objs.Clear();

            //获取梁
            var thBeams = allStructure.BeamEngine.Elements.Cast<ThIfcLineBeam>().ToList();
            thBeams.ForEach(x => x.ExtendBoth(20, 20));
            beams = thBeams.Select(o => o.Outline).Cast<Polyline>().ToList();
            objs = new DBObjectCollection();
            beams.ForEach(x => objs.Add(x));
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            beams = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(pFrame).Cast<Polyline>().ToList();
            objs.Clear();

            //获取剪力墙
            walls = allStructure.ShearWallEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            objs = new DBObjectCollection();
            walls.ForEach(x => objs.Add(x));
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(pFrame).Cast<Polyline>().ToList();
        }
        #endregion

        [CommandMethod("TIANHUACAD", "THLG", CommandFlags.Modal)]
        public void ThConnectPipe()
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
                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var plBack = acdb.Element<Polyline>(frame);
                    var plFrame = ThMEPFrameService.Normalize(plBack);

                    var filterlist = OpFilter.Bulid(o =>
                        o.Dxf((int)DxfCode.LayerName) == ThWSSCommon.PipeLine_LayerName &
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
                        var pipeLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(plFrame).Cast<Line>().ToList();

                    }
                }
            }
        }
        //厨房立管
        [CommandMethod("TIANHUACAD", "THKITCHENPIPE", CommandFlags.Modal)]
        public void ThKitchenpipe()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var kitchenEngine = new ThKitchenContainerRecognitionEngine())
            {
                PromptIntegerOptions parameter_floor = new PromptIntegerOptions("请输入楼层");
                PromptIntegerResult floor = Active.Editor.GetInteger(parameter_floor);
                if (floor.Status != PromptStatus.OK)
                {
                    return;
                }

                var zone = new ThWPipeZone();
                var parameters = new ThWKitchenPipeParameters(1, floor.Value);
                kitchenEngine.Recognize(acadDatabase.Database, new Point3dCollection());
                var validKitchenContainers = kitchenEngine.KitchenContainers.Where(o => IsValidKitchenContainer(o));
                foreach (var kitchen in validKitchenContainers)
                {
                    Polyline Boundry = kitchen.Kitchen.Boundary as Polyline;                  
                    Polyline Outline = kitchen.DrainageWells[0].Boundary as Polyline;
                    BlockReference Basinline = kitchen.BasinTools[0].Outline as BlockReference;
                    Polyline Pype = new Polyline();
                    if (kitchen.Pypes.Count>0)
                    {
                       Pype = kitchen.Pypes[0].Boundary as Polyline;
                    }
                    else
                    {
                       Pype = new Polyline();
                    }
                    var engine = new ThWKitchenPipeEngine()
                    {
                        Zone = zone,
                        Parameters = parameters,
                    };

                    engine.Run(Boundry, Outline, Basinline, Pype);
                    foreach (Point3d pt in engine.Pipes)
                    {
                        acadDatabase.ModelSpace.Add(new DBPoint(pt));
                        acadDatabase.ModelSpace.Add(new Circle() { Radius = floor.Value/2, Center = pt });
                        DBText taggingtext = new DBText()
                        {
                            Height = 20,
                            Position = pt,
                            TextString = engine.Parameters.Identifier,
                        };
                        acadDatabase.ModelSpace.Add(taggingtext);
                    }
                }
            }
        }

        //卫生间立管
        [CommandMethod("TIANHUACAD", "THTOILETPIPE", CommandFlags.Modal)]
        public void ThToiletPipe()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var toiletEngine = new ThToiletContainerRecognitionEngine())
            {
                var separation_key = new PromptKeywordOptions("\n污废分流");
                separation_key.Keywords.Add("是", "Y", "是(Y)");
                separation_key.Keywords.Add("否", "N", "否(N)");
                separation_key.Keywords.Default = "否";
                var result = Active.Editor.GetKeywords(separation_key);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                bool isSeparation = result.StringResult == "是";

                var caisson_key = new PromptKeywordOptions("\n沉箱");
                caisson_key.Keywords.Add("有", "Y", "有(Y)");
                caisson_key.Keywords.Add("没有", "N", "没有(N)");
                caisson_key.Keywords.Default = "没有";
                result = Active.Editor.GetKeywords(caisson_key);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                bool isCaisson = result.StringResult == "有";

                var parameter_floor = new PromptIntegerOptions("请输入楼层");
                var floorResult = Active.Editor.GetInteger(parameter_floor);
                if (floorResult.Status != PromptStatus.OK)
                {
                    return;
                }

                toiletEngine.Recognize(acadDatabase.Database, new Point3dCollection());
                var validToiletContainers = toiletEngine.ToiletContainers.Where(o => IsValidToiletContainer(o));
                foreach (var toilet in validToiletContainers)
                {
                    Polyline boundry = toilet.Toilet.Boundary as Polyline;
                    Polyline well = toilet.DrainageWells[0].Boundary as Polyline;
                    Polyline closestool = toilet.Closestools[0].Outline as Polyline;
                    var zone = new ThWPipeZone();
                    var parameters = new ThWToiletPipeParameters(isSeparation, isCaisson, floorResult.Value);
                    var engine = new ThWToiletPipeEngine()
                    {
                        Zone = zone,
                        Parameters = parameters,
                    };
                    engine.Run(boundry, well, closestool);
                    for (int i = 0; i < parameters.Number; i++)
                    {
                        acadDatabase.ModelSpace.Add(new DBPoint(engine.Pipes[i]));
                        acadDatabase.ModelSpace.Add(new Circle() { Radius = parameters.Diameter[i] / 2, Center = engine.Pipes[i] });
                        DBText taggingtext = new DBText()
                        {
                            Height = 20,
                            Position = engine.Pipes[i],
                            TextString = engine.Parameters.Identifier[i],
                        };
                        acadDatabase.ModelSpace.Add(taggingtext);
                    }
                }
            }
        }
        private bool IsValidToiletContainer(ThToiletContainer toiletContainer)
        {
            return
                toiletContainer.Toilet != null &&
                toiletContainer.DrainageWells.Count==1 &&
                toiletContainer.Closestools.Count == 1 &&
                toiletContainer.FloorDrains.Count > 0; 
        }
        private bool IsValidKitchenContainer(ThKitchenContainer kitchenContainer)
        {
            return
                kitchenContainer.Kitchen != null &&
                kitchenContainer.DrainageWells.Count == 1;         
        }


        [CommandMethod("TIANHUACAD", "THPIPECOMPOSITE", CommandFlags.Modal)]
        public void Thpipecomposite()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n选择厨房框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var result2 = Active.Editor.GetEntity("\n选择厨房管井/框线外管井");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }
                var result3 = Active.Editor.GetEntity("\n选择厨房台盆");
                if (result3.Status != PromptStatus.OK)
                {
                    return;
                }
                var result4 = Active.Editor.GetEntity("\n选择厨房排烟管");
                if (result3.Status != PromptStatus.OK)
                {
                    return;
                }
                var result5 = Active.Editor.GetEntity("\n选择卫生间框线");
                if (result3.Status != PromptStatus.OK)
                {
                    return;
                }
                var result6 = Active.Editor.GetEntity("\n选择卫生间管井");
                if (result3.Status != PromptStatus.OK)
                {
                    return;
                }
                var result7 = Active.Editor.GetEntity("\n选择卫生间坐便器");
                if (result3.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline boundary = acadDatabase.Element<Polyline>(result.ObjectId);
                Polyline outline = acadDatabase.Element<Polyline>(result2.ObjectId);
                Polyline pype = acadDatabase.Element<Polyline>(result4.ObjectId);
                Polyline boundary1 = acadDatabase.Element<Polyline>(result5.ObjectId);
                Polyline outline1 = acadDatabase.Element<Polyline>(result6.ObjectId);
                Polyline urinal = acadDatabase.Element<Polyline>(result7.ObjectId);
                BlockReference basinline = acadDatabase.Element<BlockReference>(result3.ObjectId);

                var zone = new ThWPipeZone();
                var toiletEngine = new ThWToiletPipeEngine()
                {
                    Zone = zone,
                    Parameters = new ThWToiletPipeParameters(true, true, 150),
                };
                var kitchenEngine = new ThWKitchenPipeEngine()
                {
                    Zone = zone,
                    Parameters = new ThWKitchenPipeParameters(1, 100),
                };
                var compositeEngine = new ThWCompositePipeEngine(kitchenEngine, toiletEngine);
                compositeEngine.Run(boundary, outline, basinline, pype, boundary1, outline1, urinal);
                foreach (Point3d pt in compositeEngine.KitchenPipes)
                {
                    acadDatabase.ModelSpace.Add(new DBPoint(pt));
                    acadDatabase.ModelSpace.Add(new Circle() { Radius = 50, Center = pt });
                }
                for (int i = 0; i < compositeEngine.ToiletPipes.Count; i++)
                {
                    var toilet = compositeEngine.ToiletPipes[i];
                    var radius = compositeEngine.ToiletPipeEngine.Parameters.Diameter[i] / 2.0;
                    acadDatabase.ModelSpace.Add(new DBPoint(toilet));
                    acadDatabase.ModelSpace.Add(new Circle() { Radius = radius, Center = toilet });

                }
            }
        }

        [CommandMethod("TIANHUACAD", "THFLOORDRAIN", CommandFlags.Modal)]
        public void Thfloordrain()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                Active.Editor.WriteMessage("\n 选择卫生间地漏");
                TypedValue[] tvs = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"Insert")
                };
                SelectionFilter sf = new SelectionFilter(tvs);
                var result = Active.Editor.GetSelection(sf);
                var tfloordrain = new List<BlockReference>();
                if (result.Status == PromptStatus.OK)
                {
                    //块的集合

                    foreach (var objId in result.Value.GetObjectIds())
                    {
                        tfloordrain.Add(acadDatabase.Element<BlockReference>(objId));
                    }
                }
                var result1 = Active.Editor.GetEntity("\n选择卫生间框线");
                if (result1.Status != PromptStatus.OK)
                {
                    return;
                }
                Active.Editor.WriteMessage("\n 选择阳台地漏");
                TypedValue[] tvs1 = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"Insert")
                };
                SelectionFilter sf1 = new SelectionFilter(tvs1);
                var result2 = Active.Editor.GetSelection(sf1);
                //块的集合
                var bfloordrain = new List<BlockReference>();
                if (result2.Status == PromptStatus.OK)
                {
                    foreach (var objId in result2.Value.GetObjectIds())
                    {
                        bfloordrain.Add(acadDatabase.Element<BlockReference>(objId));
                    }
                }

                var result3 = Active.Editor.GetEntity("\n选择阳台框线");
                if (result3.Status != PromptStatus.OK)
                {
                    return;
                }
                var result4 = Active.Editor.GetEntity("\n选择雨水管");
                if (result4.Status != PromptStatus.OK)
                {
                    return;
                }
                var result5 = Active.Editor.GetEntity("\n选择排水管");
                if (result5.Status != PromptStatus.OK)
                {
                    return;
                }
                var result6 = Active.Editor.GetEntity("\n选择洗衣机");
                if (result6.Status != PromptStatus.OK)
                {
                    return;
                }
                var result7 = Active.Editor.GetEntity("\n设备平台框线");
                if (result7.Status != PromptStatus.OK)
                {
                    return;
                }
                var result8 = Active.Editor.GetEntity("\n冷凝管或雨水管");
                if (result8.Status != PromptStatus.OK)
                {
                    return;
                }
                var result9 = Active.Editor.GetEntity("\n另一侧设备平台");
                if (result9.Status != PromptStatus.OK)
                {
                    return;
                }
                var result10 = Active.Editor.GetEntity("\n设备平台地漏");
                if (result10.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline tboundary = acadDatabase.Element<Polyline>(result1.ObjectId);
                Polyline bboundary = acadDatabase.Element<Polyline>(result3.ObjectId);
                Polyline rainpipe = acadDatabase.Element<Polyline>(result4.ObjectId);
                Polyline downspout = acadDatabase.Element<Polyline>(result5.ObjectId);
                BlockReference washingmachine = acadDatabase.Element<BlockReference>(result6.ObjectId);
                Polyline device = acadDatabase.Element<Polyline>(result7.ObjectId);
                Polyline condensepipe = acadDatabase.Element<Polyline>(result8.ObjectId);
                Polyline device_other = acadDatabase.Element<Polyline>(result9.ObjectId);
                BlockReference devicefloordrain = acadDatabase.Element<BlockReference>(result10.ObjectId);
                var thWBalconyFloordrainEngine = new ThWBalconyFloordrainEngine();
                var thWToiletFloordrainEngine = new ThWToiletFloordrainEngine();
                var thWDeviceFloordrainEngine = new ThWDeviceFloordrainEngine();
                var FloordrainEngine = new ThWCompositeFloordrainEngine(thWBalconyFloordrainEngine, thWToiletFloordrainEngine, thWDeviceFloordrainEngine);
                FloordrainEngine.Run(bfloordrain, bboundary, rainpipe, downspout, washingmachine, device, device_other, condensepipe, tfloordrain, tboundary, devicefloordrain);
                //            


                for (int i = 0; i < FloordrainEngine.Floordrain_toilet.Count; i++)
                {
                    Matrix3d scale = Matrix3d.Scaling(2.0, FloordrainEngine.Floordrain_toilet[i]);
                    var ent = tfloordrain[i].GetTransformedCopy(scale);
                    acadDatabase.ModelSpace.Add(ent);
                }
                //卫生间输出完毕

                for (int i = 0; i < FloordrainEngine.Floordrain.Count; i++)
                {
                    Matrix3d scale = Matrix3d.Scaling(2.0, FloordrainEngine.Floordrain[i].Position);
                    var ent = FloordrainEngine.Floordrain[i].GetTransformedCopy(scale);
                    acadDatabase.ModelSpace.Add(ent);
                }
                Matrix3d scale_washing = Matrix3d.Scaling(1.0, FloordrainEngine.Floordrain_washing[0].Position);
                var ent_washing = FloordrainEngine.Floordrain_washing[0].GetTransformedCopy(scale_washing);
                acadDatabase.ModelSpace.Add(ent_washing);
                for (int i = 0; i < FloordrainEngine.Downspout_to_Floordrain.Count - 1; i++)
                {

                    Polyline ent_line1 = new Polyline();
                    ent_line1.AddVertexAt(0, FloordrainEngine.Downspout_to_Floordrain[i].ToPoint2d(), 0, 35, 35);
                    ent_line1.AddVertexAt(1, FloordrainEngine.Downspout_to_Floordrain[i + 1].ToPoint2d(), 0, 35, 35);
                    ent_line1.Linetype = "DASHDED";
                    ent_line1.Layer = "W-DRAI-DOME-PIPE";
                    ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line1);
                }
                acadDatabase.ModelSpace.Add(FloordrainEngine.new_circle);
                for (int i = 0; i < FloordrainEngine.Rainpipe_to_Floordrain.Count - 1; i++)
                {
                    Polyline ent_line1 = new Polyline();
                    ent_line1.AddVertexAt(0, FloordrainEngine.Rainpipe_to_Floordrain[i].ToPoint2d(), 0, 35, 35);
                    ent_line1.AddVertexAt(1, FloordrainEngine.Rainpipe_to_Floordrain[i + 1].ToPoint2d(), 0, 35, 35);
                    ent_line1.Linetype = "DASHDOT";
                    ent_line1.Layer = "W-RAIN-PIPE";
                    ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line1);
                }
                //阳台输出完毕
                for (int i = 0; i < FloordrainEngine.Devicefloordrain.Count; i++)
                {
                    Matrix3d scale = Matrix3d.Scaling(2.0, FloordrainEngine.Devicefloordrain[i]);
                    var ent = devicefloordrain.GetTransformedCopy(scale);
                    acadDatabase.ModelSpace.Add(ent);
                }
                for (int i = 0; i < FloordrainEngine.Condensepipe_tofloordrain.Count - 1; i++)
                {
                    Polyline ent_line1 = new Polyline();
                    ent_line1.AddVertexAt(0, FloordrainEngine.Condensepipe_tofloordrain[i].ToPoint2d(), 0, 35, 35);
                    ent_line1.AddVertexAt(1, FloordrainEngine.Condensepipe_tofloordrain[i + 1].ToPoint2d(), 0, 35, 35);
                    ent_line1.Linetype = "DASHDOT";
                    ent_line1.Layer = "W-RAIN-PIPE";
                    ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line1);
                }
                for (int i = 0; i < FloordrainEngine.Rainpipe_tofloordrain.Count - 1; i++)
                {
                    Polyline ent_line1 = new Polyline();
                    ent_line1.AddVertexAt(0, FloordrainEngine.Rainpipe_tofloordrain[i].ToPoint2d(), 0, 35, 35);
                    ent_line1.AddVertexAt(1, FloordrainEngine.Rainpipe_tofloordrain[i + 1].ToPoint2d(), 0, 35, 35);
                    ent_line1.Linetype = "DASHDOT";
                    ent_line1.Layer = "W-RAIN-PIPE";
                    ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line1);
                }
                //设备平台输出完毕
            }
        }
        [CommandMethod("TIANHUACAD", "DEVICE", CommandFlags.Modal)]
        public void Device()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result4 = Active.Editor.GetEntity("\n选择雨水立管");
                if (result4.Status != PromptStatus.OK)
                {
                    return;
                }
                var result7 = Active.Editor.GetEntity("\n设备平台框线");
                if (result7.Status != PromptStatus.OK)
                {
                    return;
                }
                var result8 = Active.Editor.GetEntity("\n冷凝管或雨水管");
                if (result8.Status != PromptStatus.OK)
                {
                    return;
                }
                var result10 = Active.Editor.GetEntity("\n设备平台地漏");
                if (result10.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline rainpipe = acadDatabase.Element<Polyline>(result4.ObjectId);
                Polyline device = acadDatabase.Element<Polyline>(result7.ObjectId);
                Polyline condensepipe = acadDatabase.Element<Polyline>(result8.ObjectId);
                BlockReference devicefloordrain = acadDatabase.Element<BlockReference>(result10.ObjectId);
                var FloordrainEngine = new ThWDeviceFloordrainEngine();
                FloordrainEngine.Run(rainpipe, device, condensepipe, devicefloordrain);
                for (int i = 0; i < FloordrainEngine.Devicefloordrain.Count; i++)
                {
                    Matrix3d scale = Matrix3d.Scaling(2.0, FloordrainEngine.Devicefloordrain[i]);
                    var ent = devicefloordrain.GetTransformedCopy(scale);
                    acadDatabase.ModelSpace.Add(ent);
                }
                for (int i = 0; i < FloordrainEngine.Condensepipe_tofloordrain.Count - 1; i++)
                {
                    Polyline ent_line1 = new Polyline();
                    ent_line1.AddVertexAt(0, FloordrainEngine.Condensepipe_tofloordrain[i].ToPoint2d(), 0, 35, 35);
                    ent_line1.AddVertexAt(1, FloordrainEngine.Condensepipe_tofloordrain[i + 1].ToPoint2d(), 0, 35, 35);
                    ent_line1.Linetype = "DASHDOT";
                    ent_line1.Layer = "W-RAIN-PIPE";
                    ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line1);
                }
                for (int i = 0; i < FloordrainEngine.Rainpipe_tofloordrain.Count - 1; i++)
                {
                    Line ent_line = new Line(FloordrainEngine.Rainpipe_tofloordrain[i], FloordrainEngine.Rainpipe_tofloordrain[i + 1]);
                    acadDatabase.ModelSpace.Add(ent_line);
                }


            }
        }
        [CommandMethod("TIANHUACAD", "THPIPEINDEX", CommandFlags.Modal)]
        public void Thpipeindex()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                PromptIntegerOptions ppo = new PromptIntegerOptions("请输入楼层");
                PromptIntegerResult floor = Active.Editor.GetInteger(ppo);

                Active.Editor.WriteMessage("\n 选择废气F管");
                TypedValue[] tvs = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"LWPolyLine")
                };
                SelectionFilter sf = new SelectionFilter(tvs);
                var result = Active.Editor.GetSelection(sf);
                var fpipe = new List<Polyline>();
                if (result.Status == PromptStatus.OK)
                {
                    //块的集合

                    foreach (var objId in result.Value.GetObjectIds())
                    {
                        fpipe.Add(acadDatabase.Element<Polyline>(objId));
                    }
                }
                Active.Editor.WriteMessage("\n 选择通气T管");
                TypedValue[] tvs1 = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"Insert")
                };
                SelectionFilter sf1 = new SelectionFilter(tvs);
                var result1 = Active.Editor.GetSelection(sf);
                var tpipe = new List<Polyline>();
                if (result1.Status == PromptStatus.OK)
                {
                    //块的集合

                    foreach (var objId in result1.Value.GetObjectIds())
                    {
                        tpipe.Add(acadDatabase.Element<Polyline>(objId));
                    }
                }
                Active.Editor.WriteMessage("\n 选择污水W管");
                TypedValue[] tvs2 = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"Insert")
                };
                SelectionFilter sf2 = new SelectionFilter(tvs);
                var result2 = Active.Editor.GetSelection(sf);
                var wpipe = new List<Polyline>();
                if (result2.Status == PromptStatus.OK)
                {
                    //块的集合

                    foreach (var objId in result2.Value.GetObjectIds())
                    {
                        wpipe.Add(acadDatabase.Element<Polyline>(objId));
                    }
                }
                Active.Editor.WriteMessage("\n 选择污废合流P管");
                TypedValue[] tvs3 = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"Insert")
                };
                SelectionFilter sf3 = new SelectionFilter(tvs);
                var result3 = Active.Editor.GetSelection(sf);
                var ppipe = new List<Polyline>();
                if (result3.Status == PromptStatus.OK)
                {
                    //块的集合

                    foreach (var objId in result3.Value.GetObjectIds())
                    {
                        ppipe.Add(acadDatabase.Element<Polyline>(objId));
                    }
                }
                Active.Editor.WriteMessage("\n 沉箱D");
                TypedValue[] tvs4 = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"Insert")
                };
                SelectionFilter sf4 = new SelectionFilter(tvs);
                var result4 = Active.Editor.GetSelection(sf);
                var dpipe = new List<Polyline>();
                if (result4.Status == PromptStatus.OK)
                {
                    //块的集合

                    foreach (var objId in result4.Value.GetObjectIds())
                    {
                        dpipe.Add(acadDatabase.Element<Polyline>(objId));
                    }
                }
                Active.Editor.WriteMessage("\n 冷凝N管");
                TypedValue[] tvs5 = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"Insert")
                };
                SelectionFilter sf5 = new SelectionFilter(tvs);
                var result5 = Active.Editor.GetSelection(sf);
                var npipe = new List<Polyline>();
                if (result5.Status == PromptStatus.OK)
                {
                    //块的集合

                    foreach (var objId in result5.Value.GetObjectIds())
                    {
                        npipe.Add(acadDatabase.Element<Polyline>(objId));
                    }
                }
                Active.Editor.WriteMessage("\n 阳台雨水立管");
                TypedValue[] tvs6 = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"Insert")
                };
                SelectionFilter sf6 = new SelectionFilter(tvs);
                var result6 = Active.Editor.GetSelection(sf);
                var rainpipe = new List<Polyline>();
                if (result6.Status == PromptStatus.OK)
                {
                    //块的集合

                    foreach (var objId in result6.Value.GetObjectIds())
                    {
                        rainpipe.Add(acadDatabase.Element<Polyline>(objId));
                    }
                }
                var result7 = Active.Editor.GetEntity("\n楼层外框");
                if (result7.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline pboundary = acadDatabase.Element<Polyline>(result7.ObjectId);
                var PipeindexEngine = new ThWInnerpipeindexEngine();
                PipeindexEngine.Run(fpipe, tpipe, wpipe, ppipe, dpipe, npipe, rainpipe, pboundary);
                for (int i = 0; i < PipeindexEngine.Fpipeindex.Count - 1; i++)
                {
                    Line ent_line = new Line(PipeindexEngine.Fpipeindex[i], PipeindexEngine.Fpipeindex_tag[3 * i]);
                    Line ent_line1 = new Line(PipeindexEngine.Fpipeindex_tag[3 * i], PipeindexEngine.Fpipeindex_tag[3 * i + 1]);
                    //ent_line.Layer = "W-DRAI-NOTE";
                    ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line);
                    acadDatabase.ModelSpace.Add(ent_line1);
                    DBText taggingtext = new DBText()
                    {
                        Height = 200,
                        Position = PipeindexEngine.Fpipeindex_tag[3 * i + 2],
                        TextString = $"FL{floor.Value}-{i}",
                    };
                    acadDatabase.ModelSpace.Add(taggingtext);
                }
                for (int i = 0; i < PipeindexEngine.Tpipeindex.Count - 1; i++)
                {
                    Line ent_line = new Line(PipeindexEngine.Tpipeindex[i], PipeindexEngine.Tpipeindex_tag[3 * i]);
                    Line ent_line1 = new Line(PipeindexEngine.Tpipeindex_tag[3 * i], PipeindexEngine.Tpipeindex_tag[3 * i + 1]);
                    //ent_line.Layer = "W-DRAI-NOTE";
                    ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line);
                    acadDatabase.ModelSpace.Add(ent_line1);
                    DBText taggingtext = new DBText()
                    {
                        Height = 200,
                        Position = PipeindexEngine.Tpipeindex_tag[3 * i + 2],
                        TextString = $"TL{floor.Value}-{i}",

                    };
                    acadDatabase.ModelSpace.Add(taggingtext);
                }
                for (int i = 0; i < PipeindexEngine.Wpipeindex.Count - 1; i++)
                {
                    Line ent_line = new Line(PipeindexEngine.Wpipeindex[i], PipeindexEngine.Wpipeindex_tag[3 * i]);
                    Line ent_line1 = new Line(PipeindexEngine.Wpipeindex_tag[3 * i], PipeindexEngine.Wpipeindex_tag[3 * i + 1]);
                    //ent_line.Layer = "W-DRAI-NOTE";
                    ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line);
                    acadDatabase.ModelSpace.Add(ent_line1);
                    DBText taggingtext = new DBText()
                    {
                        Height = 200,
                        Position = PipeindexEngine.Wpipeindex_tag[3 * i + 2],
                        TextString = $"WL{floor.Value}-{i}",

                    };
                    acadDatabase.ModelSpace.Add(taggingtext);
                }
                for (int i = 0; i < PipeindexEngine.Ppipeindex.Count - 1; i++)
                {
                    Line ent_line = new Line(PipeindexEngine.Ppipeindex[i], PipeindexEngine.Ppipeindex_tag[3 * i]);
                    Line ent_line1 = new Line(PipeindexEngine.Ppipeindex_tag[3 * i], PipeindexEngine.Ppipeindex_tag[3 * i + 1]);
                    //ent_line.Layer = "W-DRAI-NOTE";
                    ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line);
                    acadDatabase.ModelSpace.Add(ent_line1);
                    DBText taggingtext = new DBText()
                    {
                        Height = 200,
                        Position = PipeindexEngine.Ppipeindex_tag[3 * i + 2],
                        TextString = $"PL{floor.Value}-{i}",

                    };
                    acadDatabase.ModelSpace.Add(taggingtext);
                }
                for (int i = 0; i < PipeindexEngine.Dpipeindex.Count - 1; i++)
                {
                    Line ent_line = new Line(PipeindexEngine.Dpipeindex[i], PipeindexEngine.Dpipeindex_tag[3 * i]);
                    Line ent_line1 = new Line(PipeindexEngine.Dpipeindex_tag[3 * i], PipeindexEngine.Dpipeindex_tag[3 * i + 1]);
                    //ent_line.Layer = "W-DRAI-NOTE";
                    ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line);
                    acadDatabase.ModelSpace.Add(ent_line1);
                    DBText taggingtext = new DBText()
                    {
                        Height = 200,
                        Position = PipeindexEngine.Dpipeindex_tag[3 * i + 2],
                        TextString = $"DL{floor.Value}-{i}",

                    };
                    acadDatabase.ModelSpace.Add(taggingtext);
                }
                for (int i = 0; i < PipeindexEngine.Npipeindex.Count - 1; i++)
                {
                    Line ent_line = new Line(PipeindexEngine.Npipeindex[i], PipeindexEngine.Npipeindex_tag[3 * i]);
                    Line ent_line1 = new Line(PipeindexEngine.Npipeindex_tag[3 * i], PipeindexEngine.Npipeindex_tag[3 * i + 1]);
                    //ent_line.Layer = "W-DRAI-NOTE";
                    ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line);
                    acadDatabase.ModelSpace.Add(ent_line1);
                    DBText taggingtext = new DBText()
                    {
                        Height = 200,
                        Position = PipeindexEngine.Npipeindex_tag[3 * i + 2],
                        TextString = $"NL{floor.Value}-{i}",

                    };
                    acadDatabase.ModelSpace.Add(taggingtext);
                }
                for (int i = 0; i < PipeindexEngine.Rainpipeindex.Count - 1; i++)
                {
                    Line ent_line = new Line(PipeindexEngine.Rainpipeindex[i], PipeindexEngine.Rainpipeindex_tag[3 * i]);
                    Line ent_line1 = new Line(PipeindexEngine.Rainpipeindex_tag[3 * i], PipeindexEngine.Rainpipeindex_tag[3 * i + 1]);
                    //ent_line.Layer = "W-DRAI-NOTE";
                    ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line);
                    acadDatabase.ModelSpace.Add(ent_line1);
                    DBText taggingtext = new DBText()
                    {
                        Height = 200,
                        Position = PipeindexEngine.Rainpipeindex_tag[3 * i + 2],
                        TextString = $"Y2L{floor.Value}-{i}",
                    };
                    acadDatabase.ModelSpace.Add(taggingtext);
                }
            }
        }
        [CommandMethod("TIANHUACAD", "THToiletRecognize", CommandFlags.Modal)]
        public void THToiletRecognize()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThToiletContainerRecognitionEngine tcre = new ThToiletContainerRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                Point3dCollection f = new Point3dCollection();
                tcre.Recognize(Active.Database, f);

                tcre.ToiletContainers.ForEach(o =>
                {
                    ObjectIdCollection objIds = new ObjectIdCollection();
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    dbObjs.Add(o.Toilet.Boundary);
                    o.Closestools.ForEach(m=> dbObjs.Add(m.Outline));
                    o.DrainageWells.ForEach(m => dbObjs.Add(m.Boundary));
                    o.FloorDrains.ForEach(m => dbObjs.Add(m.Outline));
                    dbObjs.Cast<Entity>().ForEach(m => objIds.Add(acadDatabase.ModelSpace.Add(m)));
                    if (o.Toilet != null && o.Closestools.Count == 1 &&
                    o.DrainageWells.Count ==1 && o.FloorDrains.Count > 0)
                    {
                        dbObjs.Cast<Entity>().ForEach(m => m.ColorIndex = 3);
                    }
                    else
                    {
                        dbObjs.Cast<Entity>().ForEach(m => m.ColorIndex = 1);
                    }
                    GroupTools.CreateGroup(Active.Database, Guid.NewGuid().ToString(), objIds);
                });
            }
        }
        [CommandMethod("TIANHUACAD", "THKitchenRecognize", CommandFlags.Modal)]
        public void THKitchenRecognize()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThKitchenContainerRecognitionEngine tcre = new ThKitchenContainerRecognitionEngine())
            {        
                Point3dCollection f = new Point3dCollection();
                tcre.Recognize(Active.Database, f);
                tcre.KitchenContainers.ForEach(o =>
                {
                    ObjectIdCollection objIds = new ObjectIdCollection();
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    dbObjs.Add(o.Kitchen.Boundary);
                    
                    o.DrainageWells.ForEach(m => dbObjs.Add(m.Boundary));
                   
                    dbObjs.Cast<Entity>().ForEach(m => objIds.Add(acadDatabase.ModelSpace.Add(m)));
                    if (o.Kitchen != null && o.DrainageWells.Count == 1 )
                    {
                        dbObjs.Cast<Entity>().ForEach(m => m.ColorIndex = 3);
                    }
                    else
                    {
                        dbObjs.Cast<Entity>().ForEach(m => m.ColorIndex = 1);
                    }
                    GroupTools.CreateGroup(Active.Database, Guid.NewGuid().ToString(), objIds);
                });
            }
        }
        [CommandMethod("TIANHUACAD", "ThExtractIfcBasinTool", CommandFlags.Modal)]
        public void ThExtractIfcBasinTool()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var basintoolEngine = new ThBasinRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                basintoolEngine.Recognize(acadDatabase.Database, frame.Vertices());
                basintoolEngine.Elements.ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o.Outline);
                });
            }
        }
    }
}