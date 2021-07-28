using System;
using System.Collections.Generic;
using System.Linq;

using Linq2Acad;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore.Algorithm;
using ThMEPWSS.DrainageSystemDiagram;
using ThCADExtension;

namespace ThMEPWSS
{
    public partial class ThSystemDiagramCmds
    {
        [CommandMethod("TIANHUACAD", "THJSZC", CommandFlags.Modal)]
        public void ThDStoAxonometricDrawing()
        {
            //取框线
            var regionPts = SelectRecPoints("\n请框选给水管,选择左上角点", "\n请框选给水管,再选择右下角点");
            if (regionPts.Item1 == regionPts.Item2)
            {
                return;
            }

            //取起点
            var startPt = SelectPoint("\n请选择给水起点");
            if (startPt == Point3d.Origin)
            {
                return;
            }

            //取绘制轴测图位置
            var drawPlace = SelectPoint("\n请选择绘制轴测图位置");
            if (drawPlace == Point3d.Origin)
            {
                return;
            }

            var frame = toFrame(regionPts);
            if (frame == null || frame.NumberOfVertices == 0)
            {
                return;
            }

            //var frameCenter = new Point3d((regionPts.Item1.X + regionPts.Item2.X) / 2, (regionPts.Item1.Y + regionPts.Item2.Y) / 2, 0);
            //var frameCenter = regionPts.Item1;
           
            ////转换坐标系
            //Polyline transFrame = new Polyline();
            ThMEPOriginTransformer transformer = null;
            //if (frame != null && frame.NumberOfVertices > 0)
            //{
            //    transFrame = transPoly(frame, ref transformer);
            //}
            //var pts = transFrame.VerticesEx(100.0);

            //取厕所
            var pts = frame.VerticesEx(100.0);
            List<ThTerminalToilet> toiletList = null;
            List<Line> pipes = null;
            List<ThDrainageSDADValve> valveList = null;
            List<Circle> stackList = null;

            using (var acadDb = AcadDatabase.Active())
            {
                var drainageExtractor = new ThDrainageSDAxonoExtractor();
                drainageExtractor.Transfer = transformer;
                drainageExtractor.Extract(acadDb.Database, pts);
                toiletList = drainageExtractor.SanTmnList;
                pipes = drainageExtractor.pipes;
                valveList = drainageExtractor.ValveList;
                stackList = drainageExtractor.stack;
            }

            var dataset = new ThDrainageSDADDataExchange();
            dataset.pipes = pipes;
            dataset.valveList = valveList;
            dataset.toiletList = toiletList;
            dataset.stackList = stackList;
            dataset.startPt = startPt;

            ThDrainageADConvertEngine.convertDiagram(dataset);

            //transform result to user appoint point
            var drawPlaceMatrix = Matrix3d.Displacement(drawPlace - startPt);
            dataset.convertedPipes.ForEach(x => x.TransformBy(drawPlaceMatrix));
            dataset.convertedValve.ForEach(x => x.TransformBy(drawPlaceMatrix));

            //final print
            ThDrainageSDInsertService.InsertConnectPipe(dataset.convertedPipes);
            ThDrainageSDInsertService.InsertValve(dataset.convertedValve);

            
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


        [CommandMethod("TIANHUACAD", "ThCleanZCFinalDraw", CommandFlags.Modal)]
        public void ThCleanZCFinalDraw()
        {

            //Polyline frame = selectFrame();
            Polyline transFrame = new Polyline();
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
                sLayerName.Add(ThDrainageSDCommon.Layer_Valves );

                CleanDebugDrawings.ClearFinalDrawing(sLayerName, transFrame, transformer);
            }

        }

    }
}
