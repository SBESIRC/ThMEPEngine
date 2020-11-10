using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.Bussiness
{
    public class CalSprayBlindLineAreaService : SprayBlindService
    {
        readonly double length = 3400;
        public void CalSprayBlindArea(List<Line> sprayLines, Polyline polyline)
        {
            GenerateSpraysPointService generateSpraysService = new GenerateSpraysPointService();
            var sprayData = generateSpraysService.GenerateSprays(sprayLines, length);

            //获取盲区
            var blindArea = GetBlindArea(sprayData, polyline, new List<Polyline>());

            //打印盲区
            InsertBlindArea(blindArea);
        }
    }
}
