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
        Matrix3d Matrix;

        public CalSprayBlindAreaService(Matrix3d matrix)
        {
            vDir = matrix.CoordinateSystem3d.Xaxis;
            tDir = matrix.CoordinateSystem3d.Yaxis;
            Matrix = matrix;
        }

        public void CalSprayBlindArea(List<Point3d> sprays, Polyline polyline, List<Polyline> holes, double protectRange)
        {
            var transSpray = sprays.Select(x => x.TransformBy(Matrix.Inverse())).ToList();
            var sprayData = SprayDataOperateService.CalSprayPoint(transSpray, vDir, tDir, protectRange);
            var blindArea = GetRealBlindArea(sprayData, polyline, holes);
            blindArea.ForEach(x => x.TransformBy(Matrix));

            //打印盲区
            InsertBlindArea(blindArea);
        }

        public void CalSprayBlindArea(List<SprayLayoutData> sprays, Polyline polyline, List<Polyline> holes)
        {
            var transSpray = sprays.Select(x =>
            {
                var transPt = x.Position.TransformBy(Matrix.Inverse());
                return transPt;
            }).ToList();
            var sprayData = SprayDataOperateService.CalSprayPoint(transSpray, vDir, tDir, ThWSSUIService.Instance.Parameter.protectRange);
            var blindArea = GetRealBlindArea(sprayData, polyline, holes);
            blindArea.ForEach(x => x.TransformBy(Matrix));

            //打印盲区
            InsertBlindArea(blindArea);
        }
    }
}

