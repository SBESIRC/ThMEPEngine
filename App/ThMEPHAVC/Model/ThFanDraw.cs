using System.Linq;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
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
        private Matrix3d disMat;
        private ThDuctPortsDrawService service;
        private ThTCHDrawFactory tchDrawService;
        public ThFanDraw(ref ulong gId, ThFanAnalysis anayRes, bool roomEnable, bool notRoomEnable)
        {
            Init(anayRes);
            DrawCenterLine(anayRes);
            tchDrawService.DrawSpecialShape(anayRes.specialShapesInfo, disMat, ref gId);
            tchDrawService.ductService.Draw(anayRes.centerLines.Values.ToList(), disMat, ref gId);
            tchDrawService.ductService.Draw(anayRes.UpDownVertivalPipe, disMat, ref gId);
            tchDrawService.reducingService.Draw(anayRes.reducings, disMat, ref gId);


            //service.DrawSpecialShape(anayRes.specialShapesInfo, disMat);
            //service.DrawDuct(anayRes.centerLines.Values.ToList(), disMat);
            //service.DrawDuct(anayRes.UpDownVertivalPipe, disMat);
            //service.DrawReducing(anayRes.reducings, disMat);
            service.DrawSideDuctText(anayRes, anayRes.moveSrtP, fanParam);
            DrawHose(roomEnable, notRoomEnable);
        }
        private void Init(ThFanAnalysis anayRes)
        {
            disMat = Matrix3d.Displacement(anayRes.moveSrtP.GetAsVector());
            fan = anayRes.fan;
            fanParam = anayRes.fanParam;
            bypass = anayRes.bypass;
            service = new ThDuctPortsDrawService(fan.scenario, fanParam.scale);
            tchDrawService = new ThTCHDrawFactory("D://TG20.db");
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
                ThDuctPortsDrawService.DrawLines(lines, disMat, "0", out _);
                lines.Clear();
                disMat = Matrix3d.Displacement(anayRes.fanBreakP.GetAsVector());
                foreach (Line l in anayRes.outCenterLine)
                    lines.Add(l);
                ThDuctPortsDrawService.DrawLines(lines, disMat, "0", out _);
            }   
        }
        public ObjectId InsertElectricValve(Vector3d fan_cp_vec, double valvewidth, double angle)
        {
            var e = new ThValve()
            {
                Length = 200,
                Width = valvewidth,
                ValveBlockName = ThHvacCommon.AIRVALVE_BLOCK_NAME,
                ValveBlockLayer = "H-DAPP-EDAMP",
                ValveVisibility = ThDuctUtils.ElectricValveModelName(),
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
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
            _ = new ThInletOutletDuctDrawEngine(fan, roomEnable, notRoomEnable);
        }
    }
}
