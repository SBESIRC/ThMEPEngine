using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.Model;
using ThMEPWSS.Service;
using ThMEPWSS.Utils;
using ThWSS;

namespace ThMEPWSS.Bussiness
{
    public class CalSprayBlindAreaService : SprayBlindService
    {
        Vector3d vDir;
        Vector3d tDir;
        readonly double length = 3400;

        public CalSprayBlindAreaService(Matrix3d matrix)
        {
            vDir = matrix.CoordinateSystem3d.Xaxis;
            tDir = matrix.CoordinateSystem3d.Yaxis;
        }

        public void CalSprayBlindArea(List<Point3d> sprays, Polyline polyline, List<Polyline> holes)
        {
            var sprayData = SprayDataOperateService.CalSprayPoint(sprays, vDir, tDir, length);
            var blindArea = GetBlindArea(sprayData, polyline, holes);

            //打印盲区
            InsertBlindArea(blindArea);
        }

        public void CalSprayBlindArea(List<SprayLayoutData> sprays, Polyline polyline, List<Polyline> holes)
        {
            var sprayPts = sprays.Select(x => x.Position).ToList();
            var sprayData = SprayDataOperateService.CalSprayPoint(sprayPts, vDir, tDir, length);
            var blindArea = GetBlindArea(sprayData, polyline, holes);

            //打印盲区
            InsertBlindArea(blindArea);
        }
    }
}

