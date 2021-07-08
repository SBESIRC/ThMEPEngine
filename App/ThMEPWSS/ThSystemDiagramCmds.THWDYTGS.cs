using System;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPWSS.DrainageSystemDiagram;

namespace ThMEPWSS
{
    public partial class ThSystemDiagramCmds
    {
        [CommandMethod("TIANHUACAD", "THWDYTGS", CommandFlags.Modal)]
        public void ThRouteMainPipe()
        {
#if (ACAD2016 || ACAD2018)
            //取框线
            Polyline frame = selectFrame();

            Polyline transFrame = new Polyline();
            ThMEPOriginTransformer transformer = null;
            if (frame != null && frame.NumberOfVertices > 0)
            {
                transFrame = transPoly(frame, ref transformer);
            }

            //var pts = transFrame.VerticesEx(100.0);
            var pts = frame.VerticesEx(100.0);

            //取厕所
            List<ThIfcSanitaryTerminalToilate> terminalList = null;
            using (var acadDb = AcadDatabase.Active())
            {
                var drainageExtractor = new ThDrainageSDExtractor();
                drainageExtractor.Transfer = transformer;
                drainageExtractor.Extract(acadDb.Database, pts);

                terminalList = drainageExtractor.SanTmnList;
            }

            terminalList.ForEach(toilate => toilate.SupplyCool.ForEach(pt =>
            {
                var color = toilate.SupplyCool.IndexOf(pt) == 0 ? 4 : 2;

                DrawUtils.ShowGeometry(pt, "l0supplyPt", (short)color, 25, 40, "S");
            }));


            //取房间,建筑，给水点
            List<Polyline> roomBoundary = new List<Polyline>();
            List<ThExtractorBase> archiExtractor = new List<ThExtractorBase>();
            using (var acadDb = AcadDatabase.Active())
            {
                //var roomExtractor = new ThDrainageToilateRoomExtractor() { ColorIndex = 6 };
                //roomExtractor.Extract(acadDb.Database, pts);

                archiExtractor = new List<ThExtractorBase>()
                {
                    new ThColumnExtractor(){ ColorIndex=1,IsolateSwitch=true},
                    new ThShearwallExtractor(){ ColorIndex=2,IsolateSwitch=true},
                    new ThArchitectureExtractor(){ ColorIndex=3,IsolateSwitch=true},
                    new ThDrainageSDRegionExtractor(){ColorIndex = 5,IsolateSwitch =true},
                    new ThDrainageToilateRoomExtractor() { ColorIndex = 6,GroupSwitch=true },
                    new ThDrainageSDColdWaterSupplyStartExtractor(){ColorIndex=4,GroupSwitch=true},
                };

                //archiExtractor.ForEach(o => o.SetRooms(roomExtractor.Rooms));
                archiExtractor.ForEach(o => o.Extract(acadDb.Database, pts));

                var areaId = ThDrainageSDToGJsonService.getAreaId(archiExtractor);
                archiExtractor.ForEach(o =>
                {
                    if (o is IAreaId needAreaID)
                    {
                        needAreaID.setAreaId(areaId);
                    }
                });
                //archiExtractor.Add(roomExtractor);

            }

            ThConnectToilateEngine.ThConnectEngine(archiExtractor, terminalList);
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


        [CommandMethod("TIANHUACAD", "THCleanDrainageDebugDraw", CommandFlags.Modal)]
        public void ThCleanDrainageDebugDraw()
        {

            Polyline frame = new Polyline();
            Polyline transFrame = new Polyline();
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

        [CommandMethod("TIANHUACAD", "ThCleanDrainageFinalDraw", CommandFlags.Modal)]
        public void ThCleanDrainageFinalDraw()
        {

            Polyline frame = selectFrame();
            Polyline transFrame = new Polyline();
            ThMEPOriginTransformer transformer = null;
            if (frame != null && frame.NumberOfVertices > 0)
            {
                transFrame = transPoly(frame, ref transformer);
            }

            // 调试按钮关闭且图层不是保护半径有效图层
            var debugSwitch = (Convert.ToInt16(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("USERR2")) == 1);
            if (debugSwitch)
            {
                CleanDebugDrawings.ClearFinalDrawing(transFrame, transformer);
            }

        }

        [CommandMethod("TIANHUACAD", "THTestDraw", CommandFlags.Modal)]
        public void THTestDraw()
        {
            var testL = new Line(new Point3d(107322848, -11269611, 0), new Point3d(107327851, -11274890, 0));
            DrawUtils.ShowGeometry(testL, "l0adsf", 10);

            var testL2 = new Line(new Point3d(107324958, -11269611, 0), new Point3d(107327951, -11274890, 0));
            var testL3 = new Line(new Point3d(107323958, -11269611, 0), new Point3d(107328951, -11274890, 0));
            List<Line> l = new List<Line>() { testL2, testL3 };

            DrawUtils.ShowGeometry(l, "l0adsf2", 20);


        }

    }
}