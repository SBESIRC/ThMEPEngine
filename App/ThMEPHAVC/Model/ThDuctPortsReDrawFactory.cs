using System.Linq;
using DotNetARX;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsReDrawFactory : ThDuctPortsDrawService
    {
        private Matrix3d toOrgMat;
        public ThDuctPortsReDrawFactory(string scenario, string scale, Point3d srtP) : base(scenario, scale)
        {
            toOrgMat = Matrix3d.Displacement(srtP.GetAsVector());
        }
        public void DrawCrossByCross(EntityModifyParam cross)
        {
            DrawCross(cross, toOrgMat);
        }
        public void DrawTeeByTee(EntityModifyParam tee)
        {
            DrawTee(tee, toOrgMat);
        }
        public void DrawElbowByElbow(EntityModifyParam elbow)
        {
            DrawElbow(elbow, toOrgMat);
        }
        public void DrawReducingByReducing(EntityModifyParam reducing)
        {
            var reducingGeo = CreateReducing(reducing);
            DrawShape(reducingGeo, toOrgMat, out ObjectIdList geoIds, out ObjectIdList flgIds, out ObjectIdList centerIds);
            ThDuctPortsRecoder.CreateGroup(geoIds, flgIds, centerIds, reducing.type);
        }
        public void DrawTextByText(TextModifyParam text)
        {
            textService.DrawTextInfo(text.textString, text.height, text.pos, text.rotateAngle);
        }
        public void DrawDuctByDuct(DuctModifyParam curDuctParam)
        {
            var w = ThMEPHVACService.GetWidth(curDuctParam.ductSize);
            var DuctGeo = ThDuctPortsFactory.CreateDuct(curDuctParam.sp, curDuctParam.ep, w);
            DrawDuct(DuctGeo, toOrgMat, out ObjectIdList geoIds, out ObjectIdList flgIds, out ObjectIdList centerIds);
            ThDuctPortsRecoder.CreateDuctGroup(geoIds, flgIds, centerIds, curDuctParam);
        }
        public static LineGeoInfo CreateReducing(EntityModifyParam reducing)
        {
            var isAxis = !(reducing.type == "Reducing");
            var ports = reducing.portWidths;
            var points = ports.Keys.ToList();
            var l = new Line(points[0], points[1]);
            return ThDuctPortsFactory.CreateReducing(l, ThMEPHVACService.GetWidth(ports[points[0]]), ThMEPHVACService.GetWidth(ports[points[1]]), isAxis);
        }
    }
}