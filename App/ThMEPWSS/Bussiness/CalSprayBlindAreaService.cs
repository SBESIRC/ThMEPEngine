using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.Model;
using ThMEPWSS.Service;

namespace ThMEPWSS.Bussiness
{
    public class CalSprayBlindAreaService : SprayBlindService
    {
        Vector3d vDir;
        Vector3d tDir;

        public CalSprayBlindAreaService(Matrix3d matrix)
        {
            vDir = matrix.CoordinateSystem3d.Xaxis;
            tDir = matrix.CoordinateSystem3d.Yaxis;
        }

        public void CalSprayBlindArea(List<Point3d> sprays, Polyline polyline, List<Polyline> holes)
        {
            var sprayData = SprayDataOperateService.CalSprayPoint(sprays, vDir, tDir, ThWSSUIService.Instance.Parameter.protectRange);
            var blindArea = GetRealBlindArea(sprayData, polyline, holes);

            //打印盲区
            InsertBlindArea(blindArea);
        }

        public void CalSprayBlindArea(List<SprayLayoutData> sprays, Polyline polyline, List<Polyline> holes)
        {
            var sprayPts = sprays.Select(x => x.Position).ToList();
            var sprayData = SprayDataOperateService.CalSprayPoint(sprayPts, vDir, tDir, ThWSSUIService.Instance.Parameter.protectRange);
            var blindArea = GetRealBlindArea(sprayData, polyline, holes);

            //打印盲区
            InsertBlindArea(blindArea);
        }
    }
}

