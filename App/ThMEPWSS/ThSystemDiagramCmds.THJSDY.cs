using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using GeometryExtensions;

using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Diagnostics;
using ThCADExtension;

using ThMEPWSS.DrainageSystemDiagram;
using ThMEPWSS.DrainageSystemDiagram.Service;

namespace ThMEPWSS
{
    public partial class ThSystemDiagramCmds
    {
        [CommandMethod("TIANHUACAD", "THJSDY", CommandFlags.Modal)]
        public void ThRouteMainPipe()
        {
#if (ACAD2016 || ACAD2018)
            try
            {
                //取起点
                var startPt = SelectPoint("\n请选择给水起点");
                if (startPt == Point3d.Origin)
                {
                    return;
                }

                //取框线
                var regionPts = SelectRecPoints("\n请框选洁具,选择左上角点", "\n请框选洁具,再选择右下角点");
                if (regionPts.Item1 == regionPts.Item2)
                {
                    return;
                }

                //取x轴
                var xPts = SelectLinePoints("\n请选x轴起点", "\n请选x轴终点");
                if (xPts.Item1 == xPts.Item2)
                {
                    return;
                }

                var frame = toFrame(regionPts);
                if (frame == null || frame.NumberOfVertices == 0)
                {
                    return;
                }

                if (frame.Contains(startPt) == false)
                {
                    //Active.Editor .WriteMessage(ThDrainageSDMessageCommon.startPtNoInFrame);
                    ThDrainageSDMessageServie.WriteMessage(ThDrainageSDMessageCommon.startPtNoInFrame);
                    return;
                }

                var xVector = (xPts.Item2 - xPts.Item1).GetNormal();

                var dataSet = new ThDrainageSDDataExchange();
                var areaId = Guid.NewGuid().ToString();
                var supplyPt = new ThDrainageSDCoolSupplyStart(startPt, areaId);
                var region = new ThDrainageSDRegion(frame, areaId);

                ////转换坐标系
                //Polyline transFrame = new Polyline();
                ThMEPOriginTransformer transformer = null;
                //if (frame != null && frame.NumberOfVertices > 0)
                //{
                //    transFrame = transPoly(frame, ref transformer);
                //}
                //var pts = transFrame.VerticesEx(100.0);


                dataSet.AreaID = areaId;
                dataSet.SupplyStart = supplyPt;
                dataSet.Region = region;

                //取厕所
                var pts = frame.VerticesEx(100.0);
                List<ThTerminalToilet> allToiletList = null;
                using (var acadDb = AcadDatabase.Active())
                {
                    var drainageExtractor = new ThDrainageSDExtractor();
                    drainageExtractor.Transfer = transformer;
                    drainageExtractor.Extract(acadDb.Database, pts);
                    allToiletList = drainageExtractor.SanTmnList;
                }

                //取房间,建筑
                List<ThExtractorBase> archiExtractor = new List<ThExtractorBase>();
                using (var acadDb = AcadDatabase.Active())
                {
                    archiExtractor = new List<ThExtractorBase>()
                {
                    new ThColumnExtractor(){ ColorIndex=1,IsolateSwitch=true},
                    new ThShearwallExtractor(){ ColorIndex=2,IsolateSwitch=true},
                    new ThArchitectureExtractor(){ ColorIndex=3,IsolateSwitch=true},
                    new ThDrainageToiletRoomExtractor() { ColorIndex = 6,GroupSwitch=true },
                };

                    archiExtractor.ForEach(o => o.Extract(acadDb.Database, pts));
                    archiExtractor.ForEach(o =>
                    {
                        if (o is IAreaId needAreaID)
                        {
                            needAreaID.setAreaId(areaId);
                        }
                    });
                }

                var matrix = ThDrainageSDSpaceDirectionService.getMatrix(xVector, startPt);

                allToiletList.ForEach(x => x.transformBy(matrix.Inverse()));
                var columnExtractor = ThDrainageSDCommonService.getExtruactor(archiExtractor, typeof(ThColumnExtractor)) as ThColumnExtractor;
                columnExtractor.Columns.ForEach(x => x.TransformBy(matrix.Inverse()));
                var shearwallExtractor = ThDrainageSDCommonService.getExtruactor(archiExtractor, typeof(ThShearwallExtractor)) as ThShearwallExtractor;
                shearwallExtractor.Walls.ForEach(x => x.TransformBy(matrix.Inverse()));
                var ArchitectureExtractor = ThDrainageSDCommonService.getExtruactor(archiExtractor, typeof(ThArchitectureExtractor)) as ThArchitectureExtractor;
                ArchitectureExtractor.Walls.ForEach(x => x.TransformBy(matrix.Inverse()));
                var roomExtractor = ThDrainageSDCommonService.getExtruactor(archiExtractor, typeof(ThDrainageToiletRoomExtractor)) as ThDrainageToiletRoomExtractor;
                roomExtractor.Rooms.ForEach(x => x.Boundary.TransformBy(matrix.Inverse()));


                var allLink = ThDrainageSDConnectCoolSupplyEngine.ThConnectCoolSupplyEngine(archiExtractor, allToiletList, dataSet);
                DrawUtils.ShowGeometry(allLink, "l07finalLink", 142, 30);

                var allStack = ThDrainageSDStackEngine.getStackPoint(dataSet.TerminalList);
                allStack.ForEach(x => DrawUtils.ShowGeometry(x, "l10stack", 30, 25, 25));

                var allAngleValves = ThDrainageSDAngleValvesEngine.getAngleValves(dataSet.TerminalList);
                allAngleValves.ForEach(x => DrawUtils.ShowGeometry(x.Position, x.Dir, "l20Valves", 11, 25, 100));

                ThDrainageSDTreeService.buildPipeTree(dataSet);
                ThDrainageSDTreeService.printTree(dataSet.PipeTreeRoot, "l063tree");

                var allShutValve = ThDrainageSDShutValveEngine.getShutValvePoint(dataSet);
                allShutValve.ForEach(x => DrawUtils.ShowGeometry(x.Position, x.Dir, "l31ShutValves", 50, 35, 200));

                var allDims = ThDrainageSDDimEngine.getDim(dataSet);
                allDims.ForEach(x => DrawUtils.ShowGeometry(x, "l41Dim", 223));

                var finalLink = ThDrainageSDShutValveEngine.cutPipe(allShutValve, allLink);

                //turn result back
                finalLink.ForEach(x => x.TransformBy(matrix));
                allStack = allStack.Select(x => x.TransformBy(matrix)).ToList();
                allAngleValves.ForEach(x => x.TransformBy(matrix));
                allShutValve.ForEach(x => x.TransformBy(matrix));
                allDims.ForEach(x => x.TransformBy(matrix));

                //final print
                ThDrainageSDInsertService.InsertConnectPipe(finalLink);
                ThDrainageSDInsertService.InsertStackPoint(allStack);
                ThDrainageSDInsertService.InsertValve(allAngleValves);
                ThDrainageSDInsertService.InsertValve(allShutValve);
                ThDrainageSDInsertService.InsertDim(allDims);
            }
            catch (System.Exception ex)
            {
                Active.Editor.WriteLine(ex.Message);
            }
#endif
        }

        private static Polyline selectFrame()
        {
            var polyline = new Polyline();

            // 获取框线
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "请选择布置区域框线",
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
                return polyline;
            }
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {   //获取外包框
                    var frame = acdb.Element<Polyline>(obj);
                    polyline = frame;
                }
            }

            return polyline;
        }

        private Tuple<Point3d, Point3d> SelectRecPoints(string commandSuggestStrLeft, string commandSuggestStrRight)
        {
            var ptLeftRes = Active.Editor.GetPoint(commandSuggestStrLeft);
            Point3d leftDownPt = Point3d.Origin;
            if (ptLeftRes.Status == PromptStatus.OK)
            {
                leftDownPt = ptLeftRes.Value;
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }

            var ptRightRes = Active.Editor.GetCorner(commandSuggestStrRight, leftDownPt);
            if (ptRightRes.Status == PromptStatus.OK)
            {
                var rightTopPt = ptRightRes.Value;
                leftDownPt = leftDownPt.TransformBy(Active.Editor.UCS2WCS());
                rightTopPt = rightTopPt.TransformBy(Active.Editor.UCS2WCS());
                return Tuple.Create(leftDownPt, rightTopPt);
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }
        }

        private Tuple<Point3d, Point3d> SelectLinePoints(string commandSuggestStrLeft, string commandSuggestStrRight)
        {
            var ptLeftRes = Active.Editor.GetPoint(commandSuggestStrLeft);
            Point3d leftDownPt = Point3d.Origin;
            if (ptLeftRes.Status == PromptStatus.OK)
            {
                leftDownPt = ptLeftRes.Value;
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }

            var ptRightRes = Interaction.GetLineEndPoint(commandSuggestStrRight, leftDownPt);
            if (ptRightRes != Point3d.Origin)
            {
                var rightTopPt = ptRightRes;
                leftDownPt = leftDownPt.TransformBy(Active.Editor.UCS2WCS());
                rightTopPt = rightTopPt.TransformBy(Active.Editor.UCS2WCS());
                return Tuple.Create(leftDownPt, rightTopPt);
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }
        }


        private Point3d SelectPoint(string commandSuggestStr)
        {
            var ptLeftRes = Active.Editor.GetPoint(commandSuggestStr);
            Point3d pt = Point3d.Origin;
            if (ptLeftRes.Status == PromptStatus.OK)
            {
                pt = ptLeftRes.Value;
                pt = pt.TransformBy(Active.Editor.UCS2WCS());
            }
            return pt;
        }

        private static Polyline transPoly(Polyline poly, ref ThMEPOriginTransformer transformer)
        {
            Polyline transPoly = new Polyline();
            var polyClone = poly.WashClone() as Polyline;
            var centerPt = polyClone.StartPoint;

            if (Math.Abs(centerPt.X) < 10E7)
            {
                centerPt = new Point3d();
            }

            transformer = new ThMEPOriginTransformer(centerPt);
            transformer.Transform(polyClone);
            var nFrame = ThMEPFrameService.NormalizeEx(polyClone);
            if (nFrame.Area >= 1)
            {
                transPoly = nFrame;
            }

            return transPoly;
        }

        private static Polyline toFrame(Tuple<Point3d, Point3d> leftRight)
        {
            var pl = new Polyline();
            var ptRT = new Point2d(leftRight.Item2.X, leftRight.Item1.Y);
            var ptLB = new Point2d(leftRight.Item1.X, leftRight.Item2.Y);

            pl.AddVertexAt(pl.NumberOfVertices, leftRight.Item1.ToPoint2D(), 0, 0, 0);
            pl.AddVertexAt(pl.NumberOfVertices, ptRT, 0, 0, 0);
            pl.AddVertexAt(pl.NumberOfVertices, leftRight.Item2.ToPoint2D(), 0, 0, 0);
            pl.AddVertexAt(pl.NumberOfVertices, ptLB, 0, 0, 0);

            pl.Closed = true;

            return pl;
        }


        [CommandMethod("TIANHUACAD", "ThCleanDebugDraw", CommandFlags.Modal)]
        public void ThCleanDebugDraw()
        {

            var frame = new Polyline();
            var transFrame = new Polyline();
            ThMEPOriginTransformer transformer = null;

            //frame = selectFrame();

            //if (frame != null && frame.NumberOfVertices > 0)
            //{
            //    transFrame = transPoly(frame, ref transformer);
            //}


            // 调试按钮关闭且图层不是保护半径有效图层
            var debugSwitch = (Convert.ToInt16(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("USERR2")) == 1);
            if (debugSwitch)
            {
                CleanDebugDrawings.ClearDebugDrawing(transFrame, transformer);
            }

        }

        [CommandMethod("TIANHUACAD", "ThCleanDYFinalDraw", CommandFlags.Modal)]
        public void ThCleanDYFinalDraw()
        {

            //Polyline frame = selectFrame();
            var transFrame = new Polyline();
            ThMEPOriginTransformer transformer = null;
            //if (frame != null && frame.NumberOfVertices > 0)
            //{
            //    transFrame = transPoly(frame, ref transformer);
            //}

            // 调试按钮关闭且图层不是保护半径有效图层
            var debugSwitch = (Convert.ToInt16(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("USERR2")) == 1);
            if (debugSwitch)
            {
                var sLayerName = new List<string>();
                sLayerName.Add(ThDrainageSDCommon.Layer_CoolPipe);
                sLayerName.Add(ThDrainageSDCommon.Layer_Stack);
                sLayerName.Add(ThDrainageSDCommon.Layer_Valves);
                sLayerName.Add(ThDrainageSDCommon.Layer_Dim);

                CleanDebugDrawings.ClearFinalDrawing(sLayerName, transFrame, transformer);
            }

        }
    }
}