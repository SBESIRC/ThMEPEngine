using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;

namespace ThMEPHVAC.Model
{
    public class ThDrawVBypass
    {
        private double airVolume;
        private string elevation;
        private string bypassSize;
        private Matrix3d disMat;
        private ThDuctPortsDrawService service;

        public ThDrawVBypass(double airVolume, string scale, string scenario, Point3d moveSrtP, string bypassSize, string elevation)
        {
            this.airVolume = airVolume;
            this.elevation = elevation;
            this.bypassSize = bypassSize;
            disMat = Matrix3d.Displacement(moveSrtP.GetAsVector());
            service = new ThDuctPortsDrawService(scenario, scale);
        }
        public void Draw4VerticalBypass(List<LineGeoInfo> vtElbow, Point3d inVtPos, Point3d outVtPos)
        {
            foreach (var vt in vtElbow)
            {
                service.DrawShape(vt, disMat, out ObjectIdList geoIds, out ObjectIdList flgIds, out ObjectIdList centerIds);
                ThDuctPortsRecoder.CreateVtElbowGroup(geoIds, flgIds, centerIds);
            }
            DrawVtDuct(inVtPos, outVtPos, false);
        }
        public void Draw5VerticalBypass(List<LineGeoInfo> vtElbow, Point3d inVtPos, Point3d outVtPos)
        {
            foreach (var vt in vtElbow)
            {
                service.DrawDashShape(vt, disMat, out ObjectIdList geoIds, out ObjectIdList flgIds, out ObjectIdList centerIds);
                ThDuctPortsRecoder.CreateVtElbowGroup(geoIds, flgIds, centerIds);
            }
            DrawVtDuct(inVtPos, outVtPos, true);
        }
        private void DrawVtDuct(Point3d inVtPos, Point3d outVtPos, bool isDash)
        {
            var dirVec = (outVtPos - inVtPos).GetNormal();
            inVtPos = inVtPos.TransformBy(disMat);
            outVtPos = outVtPos.TransformBy(disMat);
            ThMEPHVACService.GetWidthAndHeight(bypassSize, out double width, out double height);
            var sp = inVtPos + (dirVec * height * 0.5);
            var ep = outVtPos - (dirVec * height * 0.5);
            var duct = ThDuctPortsFactory.CreateDuct(sp, ep, width);
            if (!isDash)
            {
                service.DrawDuct(duct, Matrix3d.Identity, out ObjectIdList gids, out ObjectIdList fids, out ObjectIdList cids);
                var ductParam = ThMEPHVACService.CreateDuctModifyParam(duct.centerLines, bypassSize, elevation, airVolume);
                ductParam.type = "Vertical_bypass";
                ThDuctPortsRecoder.CreateDuctGroup(gids, fids, cids, ductParam);
            }
            else
            {
                service.DrawDashDuct(duct, Matrix3d.Identity, out ObjectIdList gids, out ObjectIdList fids, out ObjectIdList cids);
                var duct_param = ThMEPHVACService.CreateDuctModifyParam(duct.centerLines, bypassSize, elevation, airVolume);
                duct_param.type = "Vertical_bypass";
                ThDuctPortsRecoder.CreateDuctGroup(gids, fids, cids, duct_param);
            }
        }
    }
}
