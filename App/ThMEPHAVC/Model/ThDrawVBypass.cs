using Autodesk.AutoCAD.Geometry;
using ThMEPHVAC.CAD;
using ThMEPHVAC.TCH;

namespace ThMEPHVAC.Model
{
    public class ThDrawVBypass
    {
        private Matrix3d disMat;
        private FanParam fanParam;
        public ThTCHDrawFactory tchDrawService;

        public ThDrawVBypass(ThDbModelFan fan, string curDbPath, Point3d moveSrtP, FanParam fanParam)
        {
            this.fanParam = fanParam;
            disMat = Matrix3d.Displacement(moveSrtP.GetAsVector());
            tchDrawService = new ThTCHDrawFactory(curDbPath, fan.scenario);
        }
        public void DrawVerticalBypass(ThFanAnalysis anayRes, ref ulong gId)
        {
            var param = new ThMEPHVACParam()
            {
                elevation = double.Parse(fanParam.roomElevation),
                mainHeight = ThMEPHVACService.GetHeight(fanParam.roomDuctSize)
            };
            tchDrawService.ductService.DrawVTDuct(anayRes.vt.vtDuct, disMat, false, param, ref gId);
            tchDrawService.ductService.DrawVerticalPipe(anayRes.vt.vtElbow, disMat, ref gId);
        }
    }
}
