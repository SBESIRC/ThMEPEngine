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
        [CommandMethod("TIANHUACAD", "THPLPTA", CommandFlags.Modal)]
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

            //double gridSpacing = 4500;
            //PromptDoubleOptions promptDouble = new PromptDoubleOptions("请输入轴网间距");
            //PromptDoubleResult doubleResult = Active.Editor.GetDouble(promptDouble);
            //if (doubleResult.Status == PromptStatus.OK)
            //{
            //    gridSpacing = doubleResult.Value;
            //}

            if (!CalWCSLayoutDirection(ref RotateTransformService.xDir))
            {
                return;
            }

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var plBack = acdb.Element<Polyline>(frame);
                    var plFrame = ThMEPFrameService.Normalize(plBack);

                    //清除原有构件
                    //plFrame.ClearSprayLines();
                    plFrame.ClearSpray();
                    plFrame.ClearBlindArea();

                    var columnEngine = new ThColumnRecognitionEngine();
                    columnEngine.Recognize(acdb.Database, plFrame.Vertices());
                    var columPoly = columnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();

                    //转换usc
                    RotateTransformService.RotatePolyline(plFrame);
                    RotateTransformService.RotatePolyline(columPoly);

                    //生成喷头
                    RayLayoutService layoutDemo = new RayLayoutService();
                    var sprayPts = layoutDemo.LayoutSpray(plFrame, columPoly, RotateTransformService.xDir, 4000, true, false);

                    //放置喷头
                    InsertSprayService.InsertSprayBlock(sprayPts.Select(o => o.Position).ToList(), SprayType.SPRAYDOWN);

                    RotateTransformService.RotateInversePolyline(plFrame);
                    //打印喷淋点盲区
                    CalSprayBlindAreaService calSprayBlindAreaService = new CalSprayBlindAreaService(RotateTransformService.xDir);
                    calSprayBlindAreaService.CalSprayBlindArea(sprayPts, plFrame);
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

            if (!CalWCSLayoutDirection(ref RotateTransformService.xDir))
            {
                return;
            }

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var plBack = acdb.Element<Polyline>(frame);
                    var plFrame = ThMEPFrameService.Normalize(plBack);

                    //清除原有构件
                    plFrame.ClearSprayLines();
                    plFrame.ClearSpray();
                    plFrame.ClearBlindArea();

                    var columnEngine = new ThColumnRecognitionEngine();
                    columnEngine.Recognize(acdb.Database, plFrame.Vertices());
                    var columPoly = columnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();

                    //转换usc
                    RotateTransformService.RotatePolyline(plFrame);
                    RotateTransformService.RotatePolyline(columPoly);

                    //acdb.ModelSpace.Add(plFrame);
                    //foreach (var item in columPoly)
                    //{
                    //    acdb.ModelSpace.Add(item);
                    //}

                    //生成喷淋对象
                    RayLayoutService layoutDemo = new RayLayoutService();
                    var sprayPts = layoutDemo.LayoutSpray(plFrame, columPoly, RotateTransformService.xDir, 4000, true);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THPLPT", CommandFlags.Modal)]
        public void ThGenerateSpary()
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
                    plFrame = plFrame.Buffer(5)[0] as Polyline;

                    //清除原有构件
                    plFrame.ClearSpray();
                    plFrame.ClearBlindArea();

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
                    }

                    ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(dBObjectCollection);
                    var sprayLines = thCADCoreNTSSpatialIndex.SelectWindowPolygon(plFrame).Cast<Line>().ToList();

                    GenerateSpraysPointService generateSpraysService = new GenerateSpraysPointService();
                    var sprayData = generateSpraysService.GenerateSprays(sprayLines);

                    //放置喷头
                    InsertSprayService.InsertSprayBlock(sprayData.Select(o => o.Position).ToList(), SprayType.SPRAYDOWN);

                    plFrame.ClearSprayLines();
                }
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
                if (!CalWCSLayoutDirection(ref RotateTransformService.xDir))
                {
                    return;
                }
                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var plBack = acdb.Element<Polyline>(frame);
                    var plFrame = ThMEPFrameService.Normalize(plBack);

                    var bufferPoly = plFrame.Buffer(1)[0] as Polyline;
                    //清除原有构件
                    plFrame.ClearBlindArea();
                    if (!CalSprayBlindArea(bufferPoly, acdb))
                    {
                        CalSprayLineBlindArea(bufferPoly, acdb);
                    }
                }
            }
        }

        /// <summary>
        /// 计算喷淋布置点盲区
        /// </summary>
        /// <param name="result"></param>
        /// <param name="acdb"></param>
        private bool CalSprayBlindArea(Polyline plFrame, AcadDatabase acdb)
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
            CalSprayBlindAreaService calSprayBlindAreaService = new CalSprayBlindAreaService(RotateTransformService.xDir);
            calSprayBlindAreaService.CalSprayBlindArea(sprayPts, plFrame);

            return true;
        }

        /// <summary>
        /// 计算喷淋布置线盲区
        /// </summary>
        /// <param name="result"></param>
        /// <param name="acdb"></param>
        private void CalSprayLineBlindArea(Polyline plFrame, AcadDatabase acdb)
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
        private bool CalWCSLayoutDirection(ref Vector3d dir)
        {
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
                dir = (endPt - transPt).GetNormal();
                return true;
            }

            return false;
        }

        [CommandMethod("TIANHUACAD", "THPLPTA2", CommandFlags.Modal)]
        public void ThAutomaticLayoutSpray2()
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

            if (!CalWCSLayoutDirection(ref RotateTransformService.xDir))
            {
                return;
            }

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var plBack = acdb.Element<Polyline>(frame);
                    var plFrame = ThMEPFrameService.Normalize(plBack);

                    //清除原有构件
                    //plFrame.ClearSprayLines();
                    plFrame.ClearSpray();
                    plFrame.ClearBlindArea();

                    var columnEngine = new ThColumnRecognitionEngine();
                    columnEngine.Recognize(acdb.Database, plFrame.Vertices());
                    var columPoly = columnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();

                    //转换usc
                    RotateTransformService.RotatePolyline(plFrame);
                    RotateTransformService.RotatePolyline(columPoly);

                    //生成喷头
                    RayLayoutService layoutDemo = new RayLayoutService();
                    var sprayPts = layoutDemo.LayoutSpray(plFrame, columPoly, RotateTransformService.xDir, 4500, false, false);

                    //放置喷头
                    InsertSprayService.InsertSprayBlock(sprayPts.Select(o => o.Position).ToList(), SprayType.SPRAYDOWN);

                    RotateTransformService.RotateInversePolyline(plFrame);
                    //打印喷淋点盲区
                    CalSprayBlindAreaService calSprayBlindAreaService = new CalSprayBlindAreaService(RotateTransformService.xDir);
                    calSprayBlindAreaService.CalSprayBlindArea(sprayPts, plFrame);
                }
            }
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
        [CommandMethod("TIANHUACAD", "THPIPE", CommandFlags.Modal)]
        public void ThPipe()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var result2 = Active.Editor.GetEntity("\n选择管井");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }
                var result3 = Active.Editor.GetEntity("\n选择台盆");
                if (result3.Status != PromptStatus.OK)
                {
                    return;
                }
                var result4 = Active.Editor.GetEntity("\n选择排气管");
                if (result4.Status != PromptStatus.OK)
                {
                    return;
                }

                var zone = new ThWPipeZone();
                var parameters = new ThWKitchenPipeParameters(1, 100);
                Polyline Pype = acadDatabase.Element<Polyline>(result4.ObjectId);
                Polyline Boundry = acadDatabase.Element<Polyline>(result.ObjectId);
                Polyline Outline = acadDatabase.Element<Polyline>(result2.ObjectId);
                BlockReference Basinline = acadDatabase.Element<BlockReference>(result3.ObjectId);
                var engine = new ThWKitchenPipeEngine()
                {
                    Zone = zone,
                    Parameters = parameters,
                };

                engine.Run(Boundry, Outline, Basinline, Pype);
                foreach (Point3d pt in engine.Pipes)
                {
                    acadDatabase.ModelSpace.Add(new DBPoint(pt));
                    acadDatabase.ModelSpace.Add(new Circle() { Radius = 50, Center = pt });
                }
            }
        }
        //卫生间立管
        [CommandMethod("TIANHUACAD", "THPIPE1", CommandFlags.Modal)]
        public void ThPipe1()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var result2 = Active.Editor.GetEntity("\n选择管井");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }
                var result3 = Active.Editor.GetEntity("\n选择马桶");
                if (result3.Status != PromptStatus.OK)
                {
                    return;
                }
              
                var zone = new ThWPipeZone();
                var parameters = new ThWToiletPipeParameters(1,1, 150);

                Polyline urinal = acadDatabase.Element<Polyline>(result3.ObjectId);
                Polyline boundry = acadDatabase.Element<Polyline>(result.ObjectId);
                Polyline outline = acadDatabase.Element<Polyline>(result2.ObjectId);
               
                var engine = new ThWToiletPipeEngine()
                {
                    Zone = zone,
                    Parameters = parameters,
                };

                engine.Run(boundry, outline, urinal);
                for (int i=0;i<parameters.Number;i++)
                {
                    acadDatabase.ModelSpace.Add(new DBPoint (engine.Pipes[i]));
                    acadDatabase.ModelSpace.Add(new Circle() { Radius = parameters.Diameter[i]/2,Center= engine.Pipes[i] });
                }
            }
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
                    Parameters = new ThWToiletPipeParameters(1, 1, 150),
                };
                var kitchenEngine = new ThWKitchenPipeEngine()
                {
                    Zone = zone,
                    Parameters = new ThWKitchenPipeParameters(1, 100),
                };
                var compositeEngine = new ThWCompositePipeEngine(kitchenEngine, toiletEngine);
                compositeEngine.Run(boundary, outline, basinline,  pype, boundary1, outline1,  urinal);
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
    }
}
