using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADCore.NTS;

using ThMEPEngineCore.Diagnostics;

using ThMEPWSS.SprinklerDim.Model;
using ThMEPWSS.SprinklerDim.Service;
using ThMEPWSS.SprinklerDim.Data;

namespace ThMEPWSS.SprinklerDim.Engine
{
    public class ThSprinklerDimEngine
    {
        public static List<ThSprinklerDimension> LayoutDimEngine(ThSprinklerDimDataProcessService dataProcess, string printTag)
        {
            var dims = new List<ThSprinklerDimension>();
            var spIdx = BuildSpatialIdx(dataProcess);

            for (int i = 0; i < dataProcess.Room.Count; i++ )
            {
                var room = dataProcess.Room[i];

                //try
                //{
                    var data = BuildRoomData(dataProcess, room, spIdx);
                    if (data.SprinklerPt.Count == 0)
                    {
                        continue;
                    }
                    var roomDim = LayoutDimForRoom(data, printTag);

                    dims.AddRange(roomDim);
                //}
                //catch (Exception ex)
                //{
                //    var a = ex.StackTrace;
                //    continue;
                //}
            }

            return dims;
        }

        private static ThCADCoreNTSSpatialIndex BuildSpatialIdx(ThSprinklerDimDataProcessService dataProcess)
        {
            var objs = new DBObjectCollection();
            dataProcess.SprinklerPt.ForEach(x => objs.Add(new DBPoint(x)));
            dataProcess.TchPipe.ForEach(x => objs.Add(x));
            dataProcess.TchPipeText.ForEach(x => objs.Add(x));
            dataProcess.Room.ForEach(x => objs.Add(x));
            dataProcess.Column.ForEach(x => objs.Add(x));
            dataProcess.Wall.ForEach(x => objs.Add(x));
            dataProcess.AxisCurves.ForEach(x => objs.Add(x));
            var spIdx = new ThCADCoreNTSSpatialIndex(objs);

            return spIdx;
        }

        private static ThSprinklerDimRoomData BuildRoomData(ThSprinklerDimDataProcessService dataProcess, MPolygon room, ThCADCoreNTSSpatialIndex spIdx)
        {

            var selectobj = spIdx.SelectCrossingPolygon(room);

            var pipe = dataProcess.TchPipe.Where(x => selectobj.Contains(x));
            var text = dataProcess.TchPipeText.Where(x => selectobj.Contains(x));
            var r = dataProcess.Room.Where(x => selectobj.Contains(x));
            var column = dataProcess.Column.Where(x => selectobj.Contains(x));
            var wall = dataProcess.Wall.Where(x => selectobj.Contains(x));
            var axis = dataProcess.AxisCurves.Where(x => selectobj.Contains(x));
            var selectPt = selectobj.OfType<DBPoint>().Select(x => x.Position);

            var roomData = new ThSprinklerDimRoomData();
            roomData.SprinklerPt.AddRange(selectPt);
            roomData.TchPipe.AddRange(pipe);
            roomData.TchPipeText.AddRange(text);
            roomData.RoomM.Add(room);
            roomData.Column.AddRange(column);
            roomData.Wall.AddRange(wall);
            roomData.AxisCurves.AddRange(axis);

            return roomData;
        }


        private static List<ThSprinklerDimension> LayoutDimForRoom(ThSprinklerDimRoomData data, string printTag)
        {
            /////////////////////////plan A
            //// 给喷淋点分区
            //var netList = ThSprinklerDimEngine.GetSprinklerPtNetwork(data.SprinklerPt, data.TchPipe, printTag, out var step);


            //// 细致断成一块块的区
            //netList = ThSprinklerNetGroupListService.ReGroupByRoom(netList, data.RoomM, out var roomOut, printTag);
            //var transNetList = ThOptimizeGroupService.GetSprinklerPtOptimizedNet(netList, printTag);

            //List<Polyline> mixRoomWall = data.Wall.Concat(ThDataTransformService.Change(data.RoomM)).ToList<Polyline>();
            //var mixRoomWallSI = ThDataTransformService.GenerateSpatialIndex(ThDataTransformService.Change(mixRoomWall));

            //ThSprinklerNetGroupListService.CutOffLinesCrossRoomOrWall(transNetList, mixRoomWall, mixRoomWallSI, printTag);


            //// 区域标注喷淋点
            //ThSprinklerNetGroupListService.GenerateCollineation(ref transNetList, step, printTag);
            //ThSprinklerDimensionService.GenerateDimension(transNetList, step, printTag, mixRoomWallSI);


            //// 生成靠参照物的标注点 + 往外拉标注
            //List<Polyline> mixColumnWall = data.Column.Concat(data.Wall).ToList<Polyline>();

            //List<List<List<Point3d>>> dimPtsList = ThSprinklerDimExtensionService.FindReferencePoint(transNetList, roomOut, mixColumnWall, data.AxisCurves, step, printTag, out var unDimedPts);
            //List<ThSprinklerDimension> dims = ThSprinklerDimExtensionService.GenerateDimensionDirectionAndDistance(dimPtsList, roomOut, data.TchPipeText, mixColumnWall, ThDataTransformService.Change(data.TchPipe), printTag);





            ////////////////////////////////plan B
            // 给喷淋点分区
            var netList = ThSprinklerDimEngine.GetSprinklerPtNetwork(data.SprinklerPt, data.TchPipe, printTag, out var step);


            // 细致断成一块块的区
            var transNetList = ThOptimizeGroupService.GetSprinklerPtOptimizedNet(netList, printTag);
            var roomOut = data.RoomM[0];

            List<Polyline> mixRoomWall = data.Wall.Concat(ThDataTransformService.Change(data.RoomM)).ToList<Polyline>();
            var mixRoomWallSI = ThDataTransformService.GenerateSpatialIndex(ThDataTransformService.Change(mixRoomWall));

            ThSprinklerNetGroupListService.CutOffLinesCrossRoomOrWall(transNetList, mixRoomWall, mixRoomWallSI, printTag);


            // 区域标注喷淋点
            ThSprinklerNetGroupListService.GenerateCollineation(ref transNetList, step, printTag);
            ThSprinklerDimensionService.GenerateDimension(transNetList, step, printTag, mixRoomWallSI);


            // 生成靠参照物的标注点 + 往外拉标注
            List<Polyline> mixColumnWall = data.Column.Concat(data.Wall).ToList<Polyline>();

            List<List<Point3d>> dimPtsList = ThSprinklerDimExtensionService.FindReferencePoint(transNetList, roomOut, mixColumnWall, data.AxisCurves, step, printTag, out var unDimedPts);
            List<ThSprinklerDimension> dims = ThSprinklerDimExtensionService.GenerateDimensionDirectionAndDistance(dimPtsList, roomOut, data.TchPipeText, mixColumnWall, ThDataTransformService.Change(data.TchPipe), printTag);

            return dims;
        }

        public static List<ThSprinklerNetGroup> GetSprinklerPtNetwork(List<Point3d> sprinkPts, List<Line> pipeLine, string printTag, out double dtSeg)
        {
            var netList = GetSprinklerPtOriginalNet(sprinkPts, pipeLine, out dtSeg, printTag);
            return netList;
        }


        private static List<ThSprinklerNetGroup> GetSprinklerPtOriginalNet(List<Point3d> sprinkPts, List<Line> pipeLine, out double DTTol, string printTag)
        {
            DTTol = 1600.0;

            var ptAngleDict = ThSprinklerNetworkService.GetAngleToPt(sprinkPts, pipeLine);

            foreach (var pt in ptAngleDict)
            {
                var dir = Vector3d.XAxis.RotateBy(pt.Value, Vector3d.ZAxis);
                DrawUtils.ShowGeometry(pt.Key, dir, String.Format("l0-{0}-prDir", printTag), 3, 30);
            }

            var dtSeg = ThSprinklerNetworkService.GetDTSeg(sprinkPts);
            DrawUtils.ShowGeometry(dtSeg, string.Format("l0-{0}-DT", printTag), 180);

            var ptDtOriDict = ThSprinklerNetworkService.GetConnectPtDict(sprinkPts, dtSeg);

            var ptDtDict = ThSprinklerNetworkService.FilterDTOrthogonalToPipeAngle(ptDtOriDict, ptAngleDict);
            var allLine = ptDtDict.SelectMany(x => x.Value).Distinct().ToList();
            DrawUtils.ShowGeometry(allLine, string.Format("l0-{0}-ptDTOrtho", printTag), 241, 30);

            ThSprinklerNetworkService.AddOrthoDTIfNoLine(ref ptDtDict, ptDtOriDict);
            var allLineAddOrthodox = ptDtDict.SelectMany(x => x.Value).Distinct().ToList();
            DrawUtils.ShowGeometry(allLineAddOrthodox, string.Format("l0-{0}-ptDTOrthoAdd", printTag), 141, 30);

            DTTol = ThSprinklerNetworkService.GetDTLength(allLineAddOrthodox);
            DrawUtils.ShowGeometry(sprinkPts[0], String.Format("dtLeng:{0}", DTTol), String.Format("l0-{0}-dtLen", printTag), hight: 200);

            ThSprinklerNetworkService.AddSinglePTToGroup(ref ptDtDict, ptAngleDict, DTTol * 1.5);
            var allLineFinal = ptDtDict.SelectMany(x => x.Value).ToList();
            ThSprinklerNetworkService.RemoveDuplicate(ref allLineFinal);
            DrawUtils.ShowGeometry(allLineFinal, string.Format("l0-{0}-ptDTOrthoAdd2", printTag), 41, 30);

            ThSprinklerNetworkService.FilterTooLongSeg(ref allLineFinal, DTTol * 3);
            DrawUtils.ShowGeometry(allLineFinal, string.Format("l0-{0}-ptDTRemoveTooLong", printTag), 171, 30);

            //return new List<ThSprinklerNetGroup>();

            var netList = ThCreateGroupService.CreateSegGroup(allLineFinal, printTag);

            ThCreateGroupService.ThSpinrklerAddSinglePtToNetGroup(ref netList, sprinkPts, ptAngleDict, DTTol);

            return netList;
        }


    }
}
