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

        public CalSprayBlindAreaService(Vector3d xDir)
        {
            vDir = xDir;
            tDir = xDir.CrossProduct(Vector3d.ZAxis);
        }

        public void CalSprayBlindArea(List<BlockReference> sprays, Polyline polyline)
        {
            var sprayPts = sprays.Select(x => x.Position).ToList();
            var sprayData = SprayDataOperateService.CalSprayPoint(sprayPts, vDir, tDir, length);
            var blindArea = GetBlindArea(sprayData, polyline);

            //打印盲区
            InsertBlindArea(blindArea);
        }

        public void CalSprayBlindArea(List<SprayLayoutData> sprays, Polyline polyline)
        {
            var sprayPts = sprays.Select(x => x.Position).ToList();
            var sprayData = SprayDataOperateService.CalSprayPoint(sprayPts, vDir, tDir, length);
            var blindArea = GetBlindArea(sprayData, polyline);

            //打印盲区
            InsertBlindArea(blindArea);
        }
    }
}

