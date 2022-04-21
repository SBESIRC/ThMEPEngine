using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;

using ThMEPEngineCore.Algorithm;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.GeojsonExtractor;

using ThMEPWSS.DrainageSystemDiagram;
using ThMEPWSS.DrainageSystemDiagram.Service;

namespace ThMEPWSS
{
    public partial class ThSystemDiagramCmds
    {
        [CommandMethod("TIANHUACAD", "-THJSZC", CommandFlags.Modal)]
        public void ThDStoAxonometricDrawing()
        {
            try
            {
                //取起点
                var startPt = SelectPoint("\n请选择给水起点");
                if (startPt == Point3d.Origin)
                {
                    return;
                }

                //取框线
                var regionPts = SelectRecPoints("\n请框选给水管,选择左上角点", "\n请框选给水管,再选择右下角点");
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

                if (frame.Contains(startPt) == false)
                {
                    ThDrainageSDMessageServie.WriteMessage(ThDrainageSDMessageCommon.startPtNoInFrame);
                    return;
                }

                var xVector = (xPts.Item2 - xPts.Item1).GetNormal();

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

                //取厕所，管线
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


                //取房间,建筑
                List<ThExtractorBase> archiExtractor = new List<ThExtractorBase>();
                using (var acadDb = AcadDatabase.Active())
                {
                    archiExtractor = new List<ThExtractorBase>()
                    {
                        new ThDrainageToiletRoomExtractor() { ColorIndex = 6,GroupSwitch=true },
                    };
                    archiExtractor.ForEach(o => o.Extract(acadDb.Database, pts));
                }

                //旋转坐标系到横平竖直
                var matrix = ThDrainageSDSpaceDirectionService.getMatrix(xVector, startPt);
                pipes.ForEach(x => x.TransformBy(matrix.Inverse()));
                valveList.ForEach(x => x.TransformBy(matrix.Inverse()));
                toiletList.ForEach(x => x.transformBy(matrix.Inverse()));
                stackList.ForEach(x => x.TransformBy(matrix.Inverse()));
                startPt = startPt.TransformBy(matrix.Inverse());
                var roomExtractor = ThDrainageSDCommonService.getExtruactor(archiExtractor, typeof(ThDrainageToiletRoomExtractor)) as ThDrainageToiletRoomExtractor;
                roomExtractor.Rooms.ForEach(x => x.Boundary.TransformBy(matrix.Inverse()));


                //传参数
                var alpha = THDrainageADUISetting.Instance.alpha;
                var dataset = new ThDrainageSDADDataExchange();
                dataset.pipes = pipes;
                dataset.valveList = valveList;
                dataset.toiletList = toiletList;
                dataset.stackList = stackList;
                dataset.startPt = startPt;
                dataset.archiExtractor = archiExtractor;
                dataset.alpha = alpha;

                ThDrainageADConvertEngine.convertDiagram(dataset, out var convertedPipes, out var convertedValve);

                //transform result to user appoint point
                var drawPlaceMatrix = Matrix3d.Displacement(drawPlace - startPt);
                convertedPipes.ForEach(x => x.TransformBy(drawPlaceMatrix));
                convertedValve.ForEach(x => x.TransformBy(drawPlaceMatrix));

                //final print
                ThDrainageSDInsertService.InsertConnectPipe(convertedPipes);
                ThDrainageSDInsertService.InsertValve(convertedValve);

            }
            catch (System.Exception ex)
            {
                Active.Editor.WriteLine(ex.Message);
            }
        }
    }
}
