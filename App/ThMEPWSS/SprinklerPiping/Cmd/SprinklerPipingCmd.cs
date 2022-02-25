using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using NetTopologySuite.Operation.Relate;
using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using GeometryExtensions;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Command;

using ThMEPWSS.DrainageSystemDiagram;

using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Data;
using ThMEPWSS.SprinklerConnect.Engine;
using ThMEPWSS.SprinklerConnect.Model;

using ThMEPWSS.SprinklerPiping.Engine;
using ThMEPWSS.SprinklerPiping.Model;
using ThMEPWSS.SprinklerConnect;
using NetTopologySuite.Geometries;
using ThMEPWSS.SprinklerPiping.Service;
using ThMEPEngineCore.Diagnostics;

namespace ThMEPWSS.SprinklerPiping.Cmd
{
    public partial class SprinklerPipingCmd
    {
        [CommandMethod("TIANHUACAD", "testSprinklerPiping", CommandFlags.Modal)]
        public void SprinklerPiping()
        {
            var cmd = new PipingCmd();
            cmd.Execute();
        }
    }

    class PipingCmd : ThMEPBaseCommand
    {
        public PipingCmd() { }
        public override void SubExecute()
        {
            SprinklerPipingExecute();
        }

        public void SprinklerPipingExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var frame = ThSprinklerDataService.GetFrame();
                if (frame == null || frame.Area < 10)
                {
                    return;
                }

                //提取点位
                var SprinklerPts = ThSprinklerConnectDataFactory.GetSprinklerConnectData(frame);

                //提取墙线等
                var dataset = new ThSprinklerConnectDataFactory();
                var geos = dataset.Create(acadDatabase.Database, frame.Vertices()).Container;
                var dataQuery = new ThSprinklerDataQueryService(geos);
                dataQuery.ClassifyData();

                //提取车位
                //var singleCarParking = ThSprinklerConnectDataFactory.GetCarData(frame, ThSprinklerConnectCommon.Layer_SingleCar);
                var doubleCarParking = ThSprinklerConnectDataFactory.GetCarData(frame, "AI-车位排-双排");

                //ucs
                var sprinklerParameter = new ThSprinklerParameter();
                sprinklerParameter.SprinklerPt = SprinklerPts;
                var geometry = new List<Polyline>();
                geometry.AddRange(dataQuery.ArchitectureWallList);
                geometry.AddRange(dataQuery.ShearWallList);
                geometry.AddRange(dataQuery.ColumnList);
                geometry.AddRange(dataQuery.RoomList);
                var netList = ThSprinklerPtNetworkEngine.GetSprinklerPtNetwork(sprinklerParameter, geometry, out double dttol);

                //取起点和方向
                var startPts = SelectLinePoints("\n请选择给水起点", "\n请选给水方向线段终点");
                if (startPts.Item1 == startPts.Item2)
                {
                    return;
                }

                DateTime startTime = DateTime.Now;
                var sprinklerPipingParameter = new SprinklerPipingParameter();
                sprinklerPipingParameter.frame = frame;
                sprinklerPipingParameter.pts = ThSprinklerConnectDataFactory.GetSprinklerConnectData(frame);
                sprinklerPipingParameter.dataQuery = dataQuery;
                sprinklerPipingParameter.parkingRows = doubleCarParking;
                sprinklerPipingParameter.netList = netList;
                sprinklerPipingParameter.dttol = dttol;
                sprinklerPipingParameter.startPoint = new Point3d(startPts.Item1.X, startPts.Item1.Y, 0);
                Vector3d startDir = (startPts.Item2 - startPts.Item1).GetNormal();
                sprinklerPipingParameter.startDirection = new Vector3d(startDir.X, startDir.Y, 0);
                sprinklerPipingParameter.sprinklerPoints = SprinklerGraphAnalyzingEngine.GetLinkedSprinklerPoints(sprinklerPipingParameter);

                //List<Polyline> roomList = new List<Polyline>();
                //foreach(var wall in sprinklerPipingParameter.dataQuery.RoomList)
                //{
                //    if(wall.Area < 100000000)
                //    {
                //        roomList.Add(wall);
                //    }
                //}

                //DrawUtils.ShowGeometry(roomList, "l00rooms", 1, lineWeightNum: 100);

                //SprinklerGraphAnalyzingEngine.GetLinkedSprinklerPoints(sprinklerPipingParameter);

                SprinklerSceneDivisionEngine.SceneDivision(sprinklerPipingParameter);

                SprinklerTreeEngine engine = new SprinklerTreeEngine();
                List<Line> pipes = engine.SprinklerTreeSearch(sprinklerPipingParameter);

                DrawUtils.ShowGeometry(pipes, string.Format("l00new-pipes-{0}", (DateTime.Now - startTime).TotalSeconds), lineWeightNum: 50);

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
    }
}
