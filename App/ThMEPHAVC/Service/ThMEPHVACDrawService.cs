using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.Service
{
    public class ThMEPHVACDrawService
    {
        private string scale;
        private Point3d srtFlagPosition;
        private ThDuctPortsDrawService service;
        public ThMEPHVACDrawService(string scenario, string scale, Point3d srtFlagPosition)
        {
            this.scale = scale;
            this.srtFlagPosition = srtFlagPosition;
            service = new ThDuctPortsDrawService(scenario, scale);
        }
        public void DrawDuct(DuctModifyParam param, Matrix3d mat)
        {
            double w = ThMEPHVACService.GetWidth(param.ductSize);
            var duct = ThDuctPortsFactory.CreateDuct(param.sp, param.ep, w);
            service.DrawDuct(duct, mat, out ObjectIdList gids, out ObjectIdList fids, out ObjectIdList cids);
            var ductParam = ThMEPHVACService.CreateDuctModifyParam(
                duct.centerLines, param.ductSize, param.elevation.ToString(), param.airVolume);
            ThDuctPortsRecoder.CreateDuctGroup(gids, fids, cids, ductParam);
            service.textService.DrawDuctText(ductParam, scale);
        }
        public void DrawReducing(Line centerLine, double bigWidth, double smallWidth, bool isAxis, Matrix3d mat)
        {
            var reducing = ThDuctPortsFactory.CreateReducing(centerLine, bigWidth, smallWidth, isAxis);
            service.DrawShape(reducing, mat, out ObjectIdList geoIds, out ObjectIdList flgIds, out ObjectIdList centerIds);
            ThDuctPortsRecoder.CreateGroup(geoIds, flgIds, centerIds, "Reducing");
        }
        public void DrawSpecialShape(List<EntityModifyParam> specialShapesInfo)
        {
            var orgDisMat = Matrix3d.Displacement(srtFlagPosition.GetAsVector());
            service.DrawSpecialShape(specialShapesInfo, orgDisMat);
        }
    }
}