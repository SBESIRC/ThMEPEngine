using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.DrainageSystemAG;
using ThMEPWSS.DrainageSystemAG.Bussiness;
using ThMEPWSS.DrainageSystemAG.DataEngine;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.DrainageSystemAG.Services;
using ThMEPWSS.Engine;

namespace ThMEPWSS.Command
{
    /// <summary>
    /// 地上排水系统
    /// </summary>
    class ThDrainSystemAboveGroundCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {

        }

        public void Execute()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 获取框线
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
                //获取外包框
                List<Curve> frameLst = new List<Curve>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }
                var pt = frameLst.First().StartPoint;

                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pt);
                EquipmentDataEngine equipmentData = new EquipmentDataEngine();
                ThRoomDataEngine roomEngine = new ThRoomDataEngine();

                var plines = HandleFrame(frameLst);
                var holeInfo = CalHoles(plines);
                foreach (var pline in holeInfo)
                {
                    var plEquipment = equipmentData.GetPolylineEquipmentBlocks(pline.Key);
                    
                    var allRooms = roomEngine.GetAllRooms(DrainSysAGCommon.GetPolyLinePointColllection(pline.Key));
                    var tubeBlocks = new List<BlockReference>();
                    var flueBlocks = new List<BlockReference>();
                    foreach (var item in plEquipment)
                    {
                        if (item.enumEquipmentType == EnumEquipmentType.waterTubeWell)
                            tubeBlocks.AddRange(item.blockReferences);
                        else if (item.enumEquipmentType == EnumEquipmentType.flueWell)
                            flueBlocks.AddRange(item.blockReferences);
                    }
                    var tubeFlueRooms = roomEngine.TubeFlueWellToRoom(tubeBlocks, flueBlocks);
                    var rooms = roomEngine.GetRoomModelRooms(allRooms, tubeFlueRooms);
                    //对设备数据进行分离
                    var classifyEqumBlock = new ClassifyEqumBlockByRoomSpace(rooms, plEquipment);
                    var classifyResult = classifyEqumBlock.GetClassifyEquipments();

                    //对房间进行分类处理
                    var tRooms = roomEngine.GetTubeWellRooms(rooms);
                    var fRooms = roomEngine.GetFlueRooms(rooms);
                    var tubeRooms = DrainSysAGCommon.GetTubeWellRoomRelation(rooms.Where(c => c.roomTypeName == Model.EnumRoomType.Toilet || c.roomTypeName == Model.EnumRoomType.Kitchen).ToList(), tRooms);
                    var kitchenRooms = rooms.Where(c => c.roomTypeName == Model.EnumRoomType.Kitchen).ToList();
                    var toiletRooms = rooms.Where(c => c.roomTypeName == Model.EnumRoomType.Toilet).ToList();
                    var balconyRooms = rooms.Where(c => c.roomTypeName == Model.EnumRoomType.Balcony).ToList();
                    var corridorRooms = rooms.Where(c => c.roomTypeName == Model.EnumRoomType.Corridor).ToList();


                    List<CreateBlockInfo> createBlockInfos = new List<CreateBlockInfo>();
                    //地漏转换
                    var createBlocks = FloorDrainConvert.FloorDrainConvertToBlock(
                        classifyResult.Where(c=>c.enumEquipmentType == EnumEquipmentType.floorDrain).ToList(),
                        classifyResult.Where(c=>c.enumEquipmentType == EnumEquipmentType.washingMachine).ToList());
                    if (null != createBlocks && createBlocks.Count > 0)
                        createBlockInfos.AddRange(createBlocks);

                    //厨房卫生间逻辑
                    RoomKitchenToiletLayout roomKitchenToiletLayout = new RoomKitchenToiletLayout(acdb, toiletRooms, kitchenRooms, tubeRooms);
                    roomKitchenToiletLayout.ToiletLayout();
                    roomKitchenToiletLayout.KitchenLayout(null,null);
                    if (null != roomKitchenToiletLayout.createBlocks && roomKitchenToiletLayout.createBlocks.Count > 0)
                        createBlockInfos.AddRange(roomKitchenToiletLayout.createBlocks);

                    CreateBlockService.CreateBlocks(acdb.Database, createBlockInfos);
                    //foreach (var room in fRooms)
                    //{
                    //    var roomPline = foundationData.RoomOutPolyline(room.thIFCRoom);
                    //    var centerPt = PipeCalcCommon.PolyLineCenterPoint(roomPline);
                    //    PointToView(acdb, null, centerPt);
                    //}
                    //foreach (var room in rooms)
                    //{

                    //    var env = room.thIFCRoom.Boundary.ToNTSGeometry().EnvelopeInternal;
                    //    var pt1 = new Point3d(env.MinX, env.MinY, 0);
                    //    var pt2 = new Point3d(env.MaxX, env.MinY, 0);
                    //    var pt3 = new Point3d(env.MaxX, env.MaxY, 0);
                    //    var pt4 = new Point3d(env.MinX, env.MaxY, 0);
                    //    var line1 = new Line(pt1, pt2);
                    //    acdb.ModelSpace.Add(line1);
                    //    var line2 = new Line(pt2, pt3);
                    //    acdb.ModelSpace.Add(line2);
                    //    var line3 = new Line(pt3, pt4);
                    //    acdb.ModelSpace.Add(line3);
                    //    var line4 = new Line(pt4, pt1);
                    //    acdb.ModelSpace.Add(line4);
                    //    //var centerPt = PipeCalcCommon.PolyLineCenterPoint(roomPline);
                    //    //PointToView(acdb, null, centerPt);
                    //}
                }
            }
        }
        /// <summary>
        /// 计算外包框和其中的洞
        /// </summary>
        /// <param name="frames"></param>
        /// <returns></returns>
        private Dictionary<Polyline, List<Polyline>> CalHoles(List<Polyline> frames)
        {
            frames = frames.OrderByDescending(x => x.Area).ToList();

            Dictionary<Polyline, List<Polyline>> holeDic = new Dictionary<Polyline, List<Polyline>>(); //外包框和洞口
            while (frames.Count > 0)
            {
                var firFrame = frames[0];
                frames.Remove(firFrame);

                var bufferFrames = firFrame.Buffer(1)[0] as Polyline;
                var holes = frames.Where(x => bufferFrames.Contains(x)).ToList();
                frames.RemoveAll(x => holes.Contains(x));

                holeDic.Add(firFrame, holes);
            }

            return holeDic;
        }

        /// <summary>
        /// 处理外包框线
        /// </summary>
        /// <param name="frameLst"></param>
        /// <returns></returns>
        private List<Polyline> HandleFrame(List<Curve> frameLst)
        {
            return frameLst.Cast<Polyline>().ToList();
        }
        void SPointArrow(AcadDatabase acdb, ThMEPOriginTransformer originTransformer, Point3d sp, Point3d ep)
        {
            Line line = new Line(sp, ep);
            Vector3d dir = line.LineDirection();
            SPointArrow(acdb, originTransformer, sp, dir);
        }
        void SPointArrow(AcadDatabase acdb, ThMEPOriginTransformer originTransformer, Point3d sp, Vector3d dir)
        {
            Vector3d normal = new Vector3d(0, 0, 1);
            Point3d tempPt = sp + dir.MultiplyBy(100);
            Vector3d x = -dir.RotateBy(Math.PI / 6, normal);
            Point3d tempEp = tempPt + x.MultiplyBy(100);
            Line line1 = new Line(tempPt, tempEp);
            x = -dir.RotateBy(-Math.PI / 6, normal);
            tempEp = tempPt + x.MultiplyBy(100);
            Line line2 = new Line(tempPt, tempEp);
            if (null != originTransformer)
                originTransformer.Reset(line1);
            acdb.ModelSpace.Add(line1);
            if (null != originTransformer)
                originTransformer.Reset(line2);
            acdb.ModelSpace.Add(line2);
        }
        void PointToView(AcadDatabase acdb, ThMEPOriginTransformer originTransformer, Point3d sp)
        {
            Vector3d x = new Vector3d(1, 0, 0);
            Vector3d y = new Vector3d(0, 1, 0);
            Point3d tempPt = sp - x.MultiplyBy(100);
            Point3d tempEp = sp + x.MultiplyBy(100);
            Line line1 = new Line(tempPt, tempEp);
            if (null != originTransformer)
                originTransformer.Reset(line1);
            acdb.ModelSpace.Add(line1);
            tempPt = sp - y.MultiplyBy(100);
            tempEp = sp + y.MultiplyBy(100);
            Line line2 = new Line(tempPt, tempEp);
            if (null != originTransformer)
                originTransformer.Reset(line2);
            acdb.ModelSpace.Add(line2);

        }
    }
}