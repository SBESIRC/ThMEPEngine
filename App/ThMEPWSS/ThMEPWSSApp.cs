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

            var xDir = CalLayoutDirection();
            RotateTransformService.xDir = xDir;
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

                    //生成喷头
                    RayLayoutService layoutDemo = new RayLayoutService();
                    var sprayPts = layoutDemo.LayoutSpray(plFrame, columPoly, xDir, 4500, false);

                    //放置喷头
                    InsertSprayService.InsertSprayBlock(sprayPts.Select(o => o.Position).ToList(), SprayType.SPRAYDOWN);

                    RotateTransformService.RotateInversePolyline(plFrame);
                    //打印喷淋点盲区
                    CalSprayBlindAreaService calSprayBlindAreaService = new CalSprayBlindAreaService(xDir);
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

            var xDir = CalLayoutDirection();
            RotateTransformService.xDir = xDir;
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
                    plFrame.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                    columPoly.ForEach(x => x.TransformBy(Active.Editor.CurrentUserCoordinateSystem));
                    RotateTransformService.RotatePolyline(plFrame);
                    RotateTransformService.RotatePolyline(columPoly);

                    //生成喷淋对象
                    RayLayoutService layoutDemo = new RayLayoutService();
                    var sprayPts = layoutDemo.LayoutSpray(plFrame, columPoly, xDir, 4500);
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

                    //清除原有构件
                    plFrame.ClearSpray();
                    plFrame.ClearBlindArea();

                    var filterlist = OpFilter.Bulid(o =>
                    o.Dxf((int)DxfCode.LayerName) == ThWSSCommon.Layout_Line_LayerName &
                    o.Dxf((int)DxfCode.Start) == RXClass.GetClass(typeof(Line)).DxfName);

                    var dBObjectCollection= new DBObjectCollection();
                    var allLines = Active.Editor.SelectAll(filterlist);
                    if (allLines.Status == PromptStatus.OK)
                    {
                        foreach (ObjectId obj in allLines.Value.GetObjectIds())
                        {
                            dBObjectCollection.Add(acdb.Element<Line>(obj));
                        }
                    }

                    ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(dBObjectCollection);
                    var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(plFrame).Cast<Line>().ToList();

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
                if (!CalSprayBlindArea(result, acdb))
                {
                    CalSprayLineBlindArea(result, acdb);
                }
            }
        }

        /// <summary>
        /// 计算喷淋布置点盲区
        /// </summary>
        /// <param name="result"></param>
        /// <param name="acdb"></param>
        private bool CalSprayBlindArea(PromptSelectionResult result, AcadDatabase acdb)
        {
            foreach (ObjectId frame in result.Value.GetObjectIds())
            {
                var xDir = CalLayoutDirection();
                RotateTransformService.xDir = xDir;

                var plBack = acdb.Element<Polyline>(frame);
                var plFrame = ThMEPFrameService.Normalize(plBack);

                //清除原有构件
                plFrame.ClearBlindArea();

                var filterlist = OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.LayerName) == ThWSSCommon.SprayLayerName &
                o.Dxf((int)DxfCode.Start) == RXClass.GetClass(typeof(BlockReference)).DxfName);

                var sprayLines = new List<BlockReference>();
                var resLines = Active.Editor.SelectByPolyline(
                frame,
                PolygonSelectionMode.Crossing,
                filterlist);
                if (resLines.Status == PromptStatus.OK)
                {
                    foreach (ObjectId obj in resLines.Value.GetObjectIds())
                    {
                        sprayLines.Add(acdb.Element<BlockReference>(obj));
                    }

                    CalSprayBlindAreaService calSprayBlindAreaService = new CalSprayBlindAreaService(xDir);
                    calSprayBlindAreaService.CalSprayBlindArea(sprayLines, plFrame);
                }
                else
                {
                    Active.Editor.WriteMessage("\n 喷淋暂未生成");
                    return false;
                }
            }

            return true;
        }
        
        /// <summary>
        /// 计算喷淋布置线盲区
        /// </summary>
        /// <param name="result"></param>
        /// <param name="acdb"></param>
        private void CalSprayLineBlindArea(PromptSelectionResult result, AcadDatabase acdb)
        {
            foreach (ObjectId frame in result.Value.GetObjectIds())
            {
                var plBack = acdb.Element<Polyline>(frame);
                var plFrame = ThMEPFrameService.Normalize(plBack);

                //清除原有构件
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

                    ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(dBObjectCollection);
                    var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(plFrame).Cast<Line>().ToList();

                    CalSprayBlindLineAreaService calSprayBlindAreaService = new CalSprayBlindLineAreaService();
                    calSprayBlindAreaService.CalSprayBlindArea(sprayLines, plFrame);
                }
                else
                {
                    Active.Editor.WriteMessage("\n 喷淋布置线暂未生成");
                }
            }
        }

        /// <summary>
        /// 计算排布方向
        /// </summary>
        /// <returns></returns>
        private Vector3d CalLayoutDirection() 
        {
            PromptPointOptions options = new PromptPointOptions("请选择排布方向起始点");
            var sResult = Active.Editor.GetPoint(options);

            if (sResult.Status == PromptStatus.OK)
            {
                var startPt = sResult.Value;
                var transPt = startPt.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                var endPt = Interaction.GetLineEndPoint("请选择终止点", transPt);

                transPt = transPt.TransformBy(Active.Editor.CurrentUserCoordinateSystem.Inverse());
                endPt = endPt.TransformBy(Active.Editor.CurrentUserCoordinateSystem.Inverse());
                return (endPt - transPt).GetNormal();
            }

            return Vector3d.XAxis;
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
    }
}
