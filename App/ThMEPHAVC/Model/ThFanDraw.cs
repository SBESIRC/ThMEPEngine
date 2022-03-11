using System;
using System.Collections.Generic;
using System.Linq;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model.Hvac;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Duct;
using ThMEPHVAC.TCH;

namespace ThMEPHVAC.Model
{
    public class ThFanDraw
    {
        public ThDbModelFan fan;
        public FanParam fanParam;
        public DBObjectCollection bypass;
        public ObjectIdList brokenLineIds;
        private Matrix3d disMat;
        private ThDuctPortsDrawService service;
        private ThTCHDrawFactory tchDrawService;
        public ThFanDraw(ref ulong gId, ThFanAnalysis anayRes, bool roomEnable, bool notRoomEnable, string curDbPath, ThDuctPortsDrawService service)
        {
            Init(anayRes, curDbPath, service);
            DrawCenterLine(anayRes);
            DrawFanTCHEntity(anayRes.specialShapesInfo, disMat, ref gId);
            DrawFanTCHDuct(anayRes, ref gId);
            DrawFanTCHReducing(anayRes.reducings, disMat, ref gId);
            DrawHose(roomEnable, notRoomEnable);
        }

        private void DrawFanTCHEntity(List<EntityWithType> entitys, Matrix3d disMat, ref ulong gId)
        {
            var roomEntInfos = new List<EntityModifyParam>();
            var notRoomEntInfos = new List<EntityModifyParam>();
            foreach (var e in entitys)
                if (e.isRoom)
                    roomEntInfos.Add(e.entity);
                else
                    notRoomEntInfos.Add(e.entity);
            var mainHeight = ThMEPHVACService.GetHeight(fanParam.roomDuctSize);
            var elevation = Double.Parse(fanParam.roomElevation);
            tchDrawService.DrawSpecialShape(roomEntInfos, disMat, mainHeight, elevation, ref gId);
            mainHeight = ThMEPHVACService.GetHeight(fanParam.notRoomDuctSize);
            elevation = Double.Parse(fanParam.notRoomElevation);
            tchDrawService.DrawSpecialShape(notRoomEntInfos, disMat, mainHeight, elevation, ref gId);
        }

        private void DrawFanTCHReducing(List<ReducingWithType> reducings, Matrix3d disMat, ref ulong gId)
        {
            var roomRedInfos = new List<LineGeoInfo>();
            var notRoomRedInfos = new List<LineGeoInfo>();
            foreach (var red in reducings)
                if (red.isRoom)
                    roomRedInfos.Add(red.bounds);
                else
                    notRoomRedInfos.Add(red.bounds);
            var mainHeight = ThMEPHVACService.GetHeight(fanParam.roomDuctSize);
            var elevation = Double.Parse(fanParam.roomElevation);
            tchDrawService.reducingService.Draw(roomRedInfos, disMat, mainHeight, elevation, ref gId);
            mainHeight = ThMEPHVACService.GetHeight(fanParam.notRoomDuctSize);
            elevation = Double.Parse(fanParam.notRoomElevation);
            tchDrawService.reducingService.Draw(notRoomRedInfos, disMat, mainHeight, elevation, ref gId);
        }

        private void DrawFanTCHDuct(ThFanAnalysis anayRes, ref ulong gId)
        {
            var roomParam = new ThMEPHVACParam() { scale = fanParam.scale, elevation = Double.Parse(fanParam.roomElevation), inDuctSize = fanParam.roomDuctSize };
            var notRoomParam = new ThMEPHVACParam() { scale = fanParam.scale, elevation = Double.Parse(fanParam.notRoomElevation), inDuctSize = fanParam.notRoomDuctSize };
            int roomLineCount = anayRes.roomLines.Count();
            var segInfos = new List<SegInfo>();
            var totalLines = anayRes.centerLines.Values.ToList();
            for (int i = 0; i < roomLineCount; ++i)
                segInfos.Add(totalLines[i]);
            tchDrawService.ductService.Draw(segInfos, disMat, false, roomParam, ref gId);
            segInfos.Clear();
            for (int i = roomLineCount; i < anayRes.centerLines.Count(); ++i)
                segInfos.Add(totalLines[i]);
            tchDrawService.ductService.Draw(segInfos, disMat, false, notRoomParam, ref gId);
            tchDrawService.ductService.Draw(anayRes.UpDownVertivalPipe, disMat, false, roomParam, ref gId);
        }
        private void Init(ThFanAnalysis anayRes, string curDbPath, ThDuctPortsDrawService service)
        {
            fan = anayRes.fan;
            this.service = service;
            bypass = anayRes.bypass;
            fanParam = anayRes.fanParam;
            disMat = Matrix3d.Displacement(anayRes.moveSrtP.GetAsVector());
            brokenLineIds = new ObjectIdList();
            tchDrawService = new ThTCHDrawFactory(curDbPath, fanParam.scenario);
        }
        private void DrawCenterLine(ThFanAnalysis anayRes)
        {
            using (var db = AcadDatabase.Active())// 立即显示重绘效果
            {
                var lines = new DBObjectCollection();
                var disMat = Matrix3d.Displacement(anayRes.moveSrtP.GetAsVector());
                foreach (var l in anayRes.notRoomLines)
                    lines.Add(l);
                foreach (var l in anayRes.roomLines)
                    lines.Add(l);
                foreach (var l in anayRes.auxLines)
                    lines.Add(l);
                ThDuctPortsDrawService.DrawLines(lines, disMat, "0", out ObjectIdList ids);
                brokenLineIds.AddRange(ids);
                lines.Clear();
                disMat = Matrix3d.Displacement(anayRes.fanBreakP.GetAsVector());
                foreach (Line l in anayRes.outCenterLine)
                    lines.Add(l);
                ThDuctPortsDrawService.DrawLines(lines, disMat, "0", out ObjectIdList outIds);
                brokenLineIds.AddRange(outIds);
            }   
        }
        public ObjectId InsertElectricValve(Vector3d fan_cp_vec, double valvewidth, double angle)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var e = new ThValve()
                {
                    Length = 200,
                    Width = valvewidth,
                    ValveBlockName = ThHvacCommon.AIRVALVE_BLOCK_NAME,
                    ValveBlockLayer = service.electrycityValveLayer,
                    ValveVisibility = ThDuctUtils.ElectricValveModelName(),
                    WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                    LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT,
                    VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
                };
                var blockName = e.ValveBlockName;
                var layerName = e.ValveBlockLayer;
                Active.Database.ImportLayer(layerName, true);
                Active.Database.ImportValve(blockName, true);
                var objId = Active.Database.InsertValve(blockName, layerName);
                objId.SetValveWidth(e.Width, e.WidthPropertyName);
                objId.SetValveModel(e.ValveVisibility);

                var blockRef = acadDatabase.Element<BlockReference>(objId, true);
                Matrix3d mat = Matrix3d.Displacement(fan_cp_vec) *
                               Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin);
                mat *= Matrix3d.Displacement(new Vector3d(-valvewidth / 2, 125, 0));

                blockRef.TransformBy(mat);
                return objId;
            }
        }
        private void DrawHose(bool roomEnable, bool notRoomEnable)
        {
            _ = new ThInletOutletDuctDrawEngine(fan, roomEnable, notRoomEnable, service);
        }
    }
}
